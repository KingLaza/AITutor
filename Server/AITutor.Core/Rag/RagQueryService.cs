namespace AITutor.Core.Rag;

using AITutor.Core.Data;
using AITutor.Core.Subjects;

public record ChatTurn(string Role, string Content);

// Zadatak na kome student trenutno radi i nivo nagoveštaja koji je već dobio (1-3).
public record TaskState(string Text, int Level);

public class RagQueryService
{
    private readonly IEmbeddingService _embeddings;
    private readonly IChunkRepository _repository;
    private readonly IChatService _chat;
    private readonly IIntentClassifier _intent;
    private readonly SubjectRegistry _subjects;

    private const int TopK = 6;
    private const string NoInfoReply = "Nemam dovoljno informacija u materijalu da odgovorim na ovo pitanje.";

    // Ispiti (pismeni) sadrže samo postavke bez odgovora, pa nisu izvor znanja za pitanja iz gradiva.
    private static readonly string[] KnowledgeSourceTypes = { "predavanja", "vezbe", "usmeni", "reseni_blanketi" };

    private const string StyleRules =
        "Piši na srpskom jeziku, ekavicom, običnim tekstom. Bez emotikona i bez LaTeX-a ($...$): gramatike i formule piši običnim znacima (->, |, eps).";

    private const int MaxChunkCharsInPrompt = 2000;
    private const int MaxHintLevel = 3;
    private const int MaxHistoryTurns = 4;
    private const int MaxHistoryTurnChars = 500;
    private const int FollowUpMaxWords = 7;

    private const string TaskDisclaimer =
        "\n\n_Napomena: rešenje je automatski generisao lokalni model. Uporedi ga sa izvorima i proveri rezultat._";

    public RagQueryService(IEmbeddingService embeddings, IChunkRepository repository,
                           IChatService chat, IIntentClassifier intent, SubjectRegistry subjects)
    {
        _embeddings = embeddings;
        _repository = repository;
        _chat = chat;
        _intent = intent;
        _subjects = subjects;
    }

    public class RagAnswer
    {
        public string Reply { get; set; } = "";
        public List<ChunkSearchResult> RetrievedChunks { get; set; } = new();
        public ChatIntent Intent { get; set; }
        public TaskState? Task { get; set; }
    }

    public async Task<RagAnswer> AnswerAsync(string question, IReadOnlyList<ChatTurn>? history = null, TaskState? activeTask = null, string? subject = null, AnswerMode mode = AnswerMode.Rag)
    {
        history ??= Array.Empty<ChatTurn>();
        subject ??= _subjects.DefaultName;
        var profile = _subjects.Get(subject);

        if (mode == AnswerMode.Plain) return await AnswerPlainAsync(question, history, subject);

        if (activeTask != null && TryGetHintLevel(question, activeTask.Level) is int level)
            return await AnswerTaskAsync(activeTask.Text, level, history, subject);

        var intent = await _intent.ClassifyAsync(question, subject);

        if (intent == ChatIntent.Casual)
        {
            var casual = await _chat.GenerateAsync(BuildCasualPrompt(question, history, subject), new ChatOptions(Temperature: 0.6));
            return new RagAnswer { Reply = casual, Intent = intent };
        }

        if (intent == ChatIntent.Task)
            return await AnswerTaskAsync(question, 1, history, subject);

        var standalone = history.Count > 0 && IsShortFollowUp(question)
            ? await CondenseQuestionAsync(question, history)
            : question;

        var queryEmbedding = await _embeddings.GetEmbeddingAsync(standalone, EmbeddingKind.Query);
        var results = await _repository.SearchAsync(queryEmbedding, TopK, KnowledgeSourceTypes, subject);

        if (results.Count == 0 || results.Min(r => r.Distance) > profile.MaxDistance)
            return new RagAnswer { Reply = NoInfoReply, RetrievedChunks = results, Intent = intent };

        var multiPart = IsMultiPartQuestion(question);
        if (multiPart)
        {
            // Svaka stavka (a, b, c...) traži sopstveni materijal; pretraga celog pitanja često promaši pojedine stavke.
            var extra = new List<ChunkSearchResult>();
            foreach (var item in SplitItems(question).Take(6))
            {
                var itemHits = await _repository.SearchAsync(await _embeddings.GetEmbeddingAsync(item, EmbeddingKind.Query),
                    2, KnowledgeSourceTypes, subject);
                extra.AddRange(itemHits.Where(h => results.All(r => r.Id != h.Id) && extra.All(x => x.Id != h.Id)));
            }
            results = extra.Concat(results).Take(10).ToList();
        }

        var context = string.Join("\n\n---\n\n", results.Select(r => Truncate(r.Content, MaxChunkCharsInPrompt)));
        var lengthRule = multiPart
            ? "Pitanje ima više stavki (a, b, c...). Odgovori na SVAKU stavku posebno, redom, u obliku \"a) Da/Ne - obrazloženje u jednoj rečenici\" (ili kratak odgovor ako pitanje nije Da/Ne). Ne preskači nijednu stavku."
            : "Daj direktan, jasan odgovor u par rečenica.";
        var prompt = $"""
            Odgovaraj SAMO na osnovu sledećeg konteksta iz nastavnog materijala. {StyleRules}
            {lengthRule} NEMOJ komentarisati kvalitet ili potpunost konteksta.
            Objasni pojam koji je student pitao. Ako kontekst sadrži i opštu definiciju i definicije posebnih varijanti (npr. proste gramatike), daj opštu definiciju, a varijante pomeni samo ako ih je student tražio.
            Ako kontekst zaista ne sadrži dovoljno informacija za pouzdan odgovor, odgovori TAČNO sa:
            "Nemam dovoljno informacija u materijalu da odgovorim na ovo pitanje." i ništa više.
            {HistoryBlock(history)}
            KONTEKST:
            {context}

            PITANJE: {question}{(standalone != question ? $"\n(Samostalno: {standalone})" : "")}

            ODGOVOR (običnim tekstom, bez LaTeX-a i bez znaka $; formule piši kao FIRST(a) ∩ FIRST(b) = ∅):
            """;

        var reply = await _chat.GenerateAsync(prompt, new ChatOptions(Temperature: 0.2, MaxTokens: multiPart ? 1000 : 600));
        return new RagAnswer { Reply = reply, RetrievedChunks = results, Intent = intent };
    }

    // Pojedinačne stavke pitanja oblika "a) ... b) ... c) ..."; tekst pre prve stavke se pridodaje svakoj ("U kom modu je sistem:").
    internal static List<string> SplitItems(string question)
    {
        var pieces = System.Text.RegularExpressions.Regex.Split(question, @"(?<![\w])(?=[a-e][\).]\s)")
            .Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        if (pieces.Count < 2) return new List<string>();
        var hasPreamble = !System.Text.RegularExpressions.Regex.IsMatch(pieces[0], @"^[a-e][\).]\s");
        return hasPreamble ? pieces.Skip(1).ToList() : pieces;
    }

    // "Odgovoriti sa Da ili Ne" ili tri i više stavki oblika a) b) c) u jednom pitanju.
    internal static bool IsMultiPartQuestion(string question) =>
        System.Text.RegularExpressions.Regex.IsMatch(question, @"da,? ili ne", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        || System.Text.RegularExpressions.Regex.Matches(question, @"(?<![\w])[a-e][\).]\s").Count >= 3;

    // Bez pretrage i bez materijala: samo opšte znanje modela (osnova za poređenje sa RAG-om).
    private async Task<RagAnswer> AnswerPlainAsync(string question, IReadOnlyList<ChatTurn> history, string subject)
    {
        var prompt = $"""
            Ti si AITutor, asistent za učenje predmeta "{subject}" na Elektronskom fakultetu u Nišu. {StyleRules}
            Odgovori na pitanje studenta na osnovu svog opšteg znanja.
            {HistoryBlock(history)}
            PITANJE: {question}

            ODGOVOR:
            """;
        var reply = await _chat.GenerateAsync(prompt, new ChatOptions(Temperature: 0.2, MaxTokens: 800));
        return new RagAnswer { Reply = reply.Trim(), Intent = ChatIntent.Subject };
    }

    // ---- zadaci i lestvica nagoveštaja ----

    private async Task<RagAnswer> AnswerTaskAsync(string task, int level, IReadOnlyList<ChatTurn> history, string subject)
    {
        level = Math.Clamp(level, 1, MaxHintLevel);
        var profile = _subjects.Get(subject);
        var plan = level == 2 ? ExtractPlan(history) : null;

        // Za zadatke podržanog tipa (raspoređivanje, disk, zamena stranica, i-node, format adrese) rešenje računa kod, ne model.
        var solved = profile.Solver == null ? null : await profile.Solver.TrySolveAsync(task);
        if (level == MaxHintLevel && solved != null)
            return new RagAnswer
            {
                Reply = "Rešenje izračunato programski (tačno, bez procene modela):\n\n" + solved.Text.Replace("\r\n", "\n"),
                Intent = ChatIntent.Task,
                Task = new TaskState(task, level),
            };

        // Za zadatke koje alat zna da reši, nagoveštaji 1 i 2 su unapred napisani i tačni (model ih ne generiše, pa ne može da zavede).
        if (level < MaxHintLevel && solved?.Hints is { } hints && hints.Count >= level)
            return new RagAnswer
            {
                Reply = hints[level - 1] + "\n\nPokušaj prvo sam. Ako zapneš, traži sledeći nagoveštaj ili prikaži rešenje.",
                Intent = ChatIntent.Task,
                Task = new TaskState(task, level),
            };
        var queryEmbedding = await _embeddings.GetEmbeddingAsync(task, EmbeddingKind.Query);

        // Predmet može da za određenu vrstu zadatka zada notaciju i izvore primera (npr. provereni obrasci).
        var guidance = profile.GetGuidance(task);
        List<ChunkSearchResult> examples;
        if (guidance != null)
        {
            // Primarni izvori uvek ulaze u kontekst; ne takmiče se sa ostalim izvorima po rangu.
            var primary = await _repository.SearchHybridAsync(queryEmbedding, task, guidance.PrimaryCount, guidance.PrimaryExampleTypes, subject);
            var secondary = await _repository.SearchHybridAsync(queryEmbedding, task, guidance.SecondaryCount, guidance.SecondaryExampleTypes, subject);
            examples = primary.Concat(secondary).OrderBy(e => e.Distance).ToList();
        }
        else
        {
            examples = DistinctByContent(await _repository.SearchHybridAsync(
                queryEmbedding, task, 6, new[] { "reseni_blanketi", "vezbe" }, subject)).Take(2).OrderBy(e => e.Distance).ToList();
        }
        var theory = await _repository.SearchHybridAsync(queryEmbedding, task, 1, new[] { "predavanja", "usmeni" }, subject);

        var state = new TaskState(task, level);
        if (examples.Concat(theory).All(c => c.Distance > profile.MaxDistance))
            return new RagAnswer { Reply = NoInfoReply, RetrievedChunks = examples.Concat(theory).ToList(), Intent = ChatIntent.Task };

        // Postavka i rešenje su često na susednim stranama: uz svaki primer dodajemo i njegov nastavak.
        var groups = new List<List<ChunkSearchResult>>();
        foreach (var ex in examples)
        {
            var group = new List<ChunkSearchResult> { ex };
            // Primarni izvori smernica su samostalni; za ostale primere postavka i rešenje su često na susednim stranama.
            var next = guidance != null && guidance.PrimaryExampleTypes.Contains(ex.SourceType) ? null : await _repository.GetNextChunkAsync(ex.Id);
            if (next != null && !examples.Any(e => e.Id == next.Id) && !groups.Any(g => g.Any(c => c.Id == next.Id)))
                group.Add(next);
            groups.Add(group);
        }

        // Nagoveštaji koriste samo najsličniji primer (kraći prompt, brži odgovor); potpuno rešenje oba.
        if (level < MaxHintLevel && groups.Count > 1) groups = groups.Take(1).ToList();
        var shown = groups.SelectMany(g => g).ToList();

        string Format(ChunkSearchResult r) => $"[{r.SourceType}/{r.SourceFile}]\n{Truncate(r.Content, MaxChunkCharsInPrompt)}";
        var theoryText = theory.Count > 0 ? string.Join("\n\n", theory.Select(Format)) : "(nema)";
        // Najsličniji primer ide poslednji, odmah pre zadatka.
        var examplesText = shown.Count > 0 ? string.Join("\n\n", Enumerable.Reverse(groups).SelectMany(g => g).Select(Format)) : "(nema)";
        var bestFile = groups.Count > 0 ? $"{groups[0][0].SourceType}/{groups[0][0].SourceFile}" : null;

        var instruction = level switch
        {
            1 => """
                Student traži NAGOVEŠTAJ, a ne rešenje. Na osnovu najsličnijeg rešenog primera (POSLEDNJI u odeljku REŠENI PRIMERI) i TEORIJE napiši kratak plan rešavanja ovakvog zadatka: 3 do 5 numerisanih koraka, svaki jednom rečenicom (šta treba uraditi, bez konačnog rezultata).
                NE rešavaj zadatak studenta, ne piši kod, tabele ni konačnu gramatiku. Počni odmah sa koracima.
                """,
            2 when plan != null => $"""
                Student je već dobio ovaj plan rešavanja:
                {plan}

                Sada traži POMOĆ, a ne ceo odgovor. Razradi rečima SAMO korake 1 i 2 tog plana: šta tačno treba uraditi na zadatku studenta i zašto, najviše 6 rečenica.
                NE piši kod, tabele ni konačno rešenje. Korake 3 i dalje NE razrađuj.
                """,
            2 => """
                Student traži POMOĆ, a ne ceo odgovor. Opiši rečima SAMO PRVA DVA KORAKA postupka rešavanja zadatka studenta (šta tačno uraditi i zašto), prema najsličnijem rešenom primeru (POSLEDNJI u odeljku REŠENI PRIMERI), najviše 6 rečenica.
                NE piši kod, tabele ni konačno rešenje.
                """,
            _ => """
                Zadaci iz ovog predmeta su šablonski. Najsličniji rešen primer je POSLEDNJI u odeljku REŠENI PRIMERI.
                Reši zadatak studenta tako što ćeš strukturu i oblik rešenja iz tog primera PREPISATI i prilagoditi zadatku (drugačija gramatika, uslov, imena, oznake).
                Ako sličnog primera nema, primeni postupak iz TEORIJE korak po korak.
                Ako ne možeš da dovršiš zadatak, reši deo koji možeš i reci šta nedostaje. Nemoj samo odbiti.
                """,
        };

        var prompt = $"""
            Ti si AITutor, tutor za predmet "{subject}" na Elektronskom fakultetu u Nišu. Student ti je dao zadatak. {StyleRules}
            Počni odmah odgovorom: bez pozdrava, bez predstavljanja i bez prepričavanja zadatka.
            Ne izmišljaj novu strukturu i ne koristi klase, metode ni instrukcije koje se ne pojavljuju u materijalu.
            {guidance?.Notation}

            {instruction}

            === TEORIJA ===
            {theoryText}

            === REŠENI PRIMERI ===
            {examplesText}

            === ZADATAK STUDENTA ===
            {task}

            ODGOVOR:
            """;

        var maxTokens = level switch { 1 => 400, 2 => 350, _ => 1500 };
        var generated = (await _chat.GenerateAsync(prompt, new ChatOptions(Temperature: 0.2, MaxTokens: maxTokens))).Trim();

        var reply = level switch
        {
            1 => generated
                + (bestFile != null ? $"\n\nSličan zadatak u materijalu: {bestFile}" : "")
                + "\n\nPokušaj prvo sam. Ako zapneš, traži sledeći nagoveštaj ili prikaži rešenje.",
            2 => generated + "\n\nPokušaj da nastaviš sam odavde. Ako zapneš, traži sledeći nagoveštaj ili prikaži rešenje.",
            _ => generated + TaskDisclaimer + ReferenceSolution(groups),
        };

        return new RagAnswer { Reply = reply, RetrievedChunks = shown.Concat(theory).ToList(), Intent = ChatIntent.Task, Task = state };
    }

    // Plan iz nivoa 1 je poslednja poruka asistenta u istoriji (bez dopisa o izvoru i poziva da student proba sam).
    internal static string? ExtractPlan(IReadOnlyList<ChatTurn> history)
    {
        var last = history.LastOrDefault(t => t.Role == "assistant")?.Content;
        if (string.IsNullOrWhiteSpace(last)) return null;
        var cut = last.IndexOf("\n\nSličan zadatak", StringComparison.Ordinal);
        var plan = (cut >= 0 ? last[..cut] : last).Trim();
        return plan.Length == 0 ? null : Truncate(plan, 800);
    }

    // Doslovan izvod iz materijala (pouzdan, za razliku od izlaza modela) uz celo rešenje.
    private static string ReferenceSolution(List<List<ChunkSearchResult>> groups)
    {
        if (groups.Count == 0) return "";
        var chunk = groups[0].Last();
        return $"\n\n---\nRešen primer iz materijala ({chunk.SourceType}/{chunk.SourceFile}):\n{Truncate(chunk.Content, 1500)}";
    }

    // "Sledeći nagoveštaj", "pokaži rešenje", "ne znam"... dok student radi na aktivnom zadatku.
    internal static int? TryGetHintLevel(string message, int currentLevel)
    {
        var text = Normalize(message);
        if (text.Length == 0 || text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 6) return null;
        if (text.Contains("resenj")) return MaxHintLevel;
        string[] hintWords = { "nagovestaj", "sledeci korak", "dalje", "pomozi", "ne znam", "jos jedan" };
        return hintWords.Any(text.Contains) ? Math.Min(currentLevel + 1, MaxHintLevel) : null;
    }

    private static string Normalize(string s) => new string(s.ToLowerInvariant()
        .Replace('č', 'c').Replace('ć', 'c').Replace('ž', 'z').Replace('š', 's').Replace('đ', 'd')
        .Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Trim();

    // ---- istorija razgovora ----

    private static bool IsShortFollowUp(string message) =>
        message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= FollowUpMaxWords;

    private static string HistoryBlock(IReadOnlyList<ChatTurn> history)
    {
        var recent = history.TakeLast(MaxHistoryTurns).ToList();
        if (recent.Count == 0) return "";
        var lines = recent.Select(t => $"{(t.Role == "user" ? "Student" : "AITutor")}: {Truncate(t.Content.Trim(), MaxHistoryTurnChars)}");
        return $"\nRANIJI RAZGOVOR (samo za razumevanje pitanja, nije izvor činjenica):\n{string.Join("\n", lines)}\n";
    }

    private async Task<string> CondenseQuestionAsync(string question, IReadOnlyList<ChatTurn> history)
    {
        var prompt = $"""
            Prepiši poslednje pitanje studenta tako da bude razumljivo samo za sebe, koristeći raniji razgovor.
            Vrati samo prepisano pitanje, bez objašnjenja. Ako je pitanje već samostalno, vrati ga neizmenjeno.
            {HistoryBlock(history)}
            POSLEDNJE PITANJE: {question}

            SAMOSTALNO PITANJE:
            """;
        var rewritten = (await _chat.GenerateAsync(prompt, new ChatOptions(Temperature: 0, MaxTokens: 80)))
            .Trim().Trim('"').Split('\n')[0].Trim();
        return rewritten.Length is > 0 and <= 300 ? rewritten : question;
    }

    // ---- pomoćne ----

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + " …";

    // Isto rešenje često postoji u više fajlova (vezbe i reseni_blanketi); poredimo po početku teksta.
    private static List<ChunkSearchResult> DistinctByContent(IEnumerable<ChunkSearchResult> chunks) =>
        chunks.GroupBy(c => new string(c.Content.Where(char.IsLetterOrDigit).Take(80).ToArray()))
              .Select(g => g.First()).ToList();

    private static string BuildCasualPrompt(string message, IReadOnlyList<ChatTurn> history, string subject) => $"""
        Ti si AITutor, prijateljski asistent za učenje predmeta "{subject}" na Elektronskom fakultetu u Nišu.
        Student ti je poslao poruku koja nije konkretno pitanje o gradivu. Odgovori kratko (1-3 rečenice), toplo i prirodno. {StyleRules}
        Ako je prikladno, ponudi pomoć: objašnjenje pojma iz gradiva, pomoć oko zadatka sa starih ispita ili ispitivanje gradiva.
        NE iznosi nikakve tvrdnje o sadržaju gradiva u ovom odgovoru.
        {HistoryBlock(history)}
        PORUKA STUDENTA: {message}
        """;
}
