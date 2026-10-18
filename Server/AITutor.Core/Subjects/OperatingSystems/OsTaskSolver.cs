using System.Globalization;
using System.Text;
using System.Text.Json;
using AITutor.Core.Subjects;

namespace AITutor.Core.Subjects.OperatingSystems;

// LLM samo prepoznaje tip zadatka i izvlači parametre u JSON; sve računanje radi deterministički kod.
public class OsTaskSolver : ITaskSolver
{
    private readonly IChatService _chat;

    public OsTaskSolver(IChatService chat) => _chat = chat;

    public async Task<SolverResult?> TrySolveAsync(string task)
    {
        // Zadaci sa fork() koda: kod se čita direktno iz teksta zadatka, bez LLM-a.
        if (ForkSimulator.TryCount(task) is { } fork)
            return Hinted("fork",
                $"Broj procesa koje program stvara (zajedno sa početnim): {fork.Processes}\n" +
                $"Novih procesa (uspešnih poziva fork()): {fork.Processes - 1}\n" +
                "Svaki poziv fork() udvostručuje proces koji ga izvršava; roditelj dobija pid deteta (različito od 0), dete dobija 0, " +
                "pa uslovi tipa if(fork()) izvršava roditelj, a if(!fork()) dete.");

        var raw = await _chat.GenerateAsync(BuildPrompt(task), new ChatOptions(Temperature: 0, MaxTokens: 500));
        return SolveFromJson(raw, task);
    }

    private static string BuildPrompt(string task) => $$"""
        Izvuci parametre zadatka iz predmeta Operativni sistemi u JSON. Prvo odredi tip zadatka:
        - "scheduling": raspoređivanje procesa (SJF, SRTF, FCFS, prioritet, Round Robin); procesi imaju CPU fazu (burst), vreme aktiviranja (arrival) i opciono prioritet.
        - "io_scheduling": procesi sa CPU i U/I fazama zapisanim kao 4-(5)-3 (CPU 4, U/I 5, CPU 3); traži se vreme izvršenja i iskorišćenost CPU za multiprogramiranje (FCFS, SRTF) i bez njega.
        - "disk": raspoređivanje zahteva za disk (SSTF, SCAN, C-SCAN, LOOK, C-LOOK, FCFS) i/ili broj staza diska.
        - "page_replacement": zamena stranica (FIFO, LRU, OPT) za niz referenci i dati broj okvira.
        - "inode": Unix/Linux i-node: veličina bloka, broj direktnih adresa, veličina adrese, veličine fajlova.
        - "address": format virtuelne adrese (straničenje, segmentacija), veličina stranice i stavke tabele stranica.
        - "banker": uzajamno blokiranje, Bankerov algoritam, bezbedno/nebezbedno stanje: matrice dodeljenih (allocation) i maksimalnih (max) zahteva, vektor raspoloživih resursa, opciono zahtev procesa.
        - "none": sve ostalo (teorija, kod, pitanja Da/Ne, monitori, semafori).

        Vrati SAMO JSON, bez objašnjenja, u obliku za izabrani tip (vrednosti uzmi iz zadatka, polja kojih nema u zadatku stavi na null):
        {"type":"scheduling","processes":[{"name":"<ime>","burst":<ms>,"arrival":<ms>,"priority":<broj ili null>}],"algorithms":["SJF","SRTF","PRIORITY","PRIORITY_PREEMPTIVE","RR","FCFS","MLFQ" (ML Feedback) - samo traženi],"quantum":<ms ili null>,"interval":[<od>,<do>] ili null}
        {"type":"io_scheduling","processes":[{"name":"<ime>","arrival":<ms>}]}
        {"type":"disk","requests":[<svi zahtevi iz reda, brojevi>],"served":[<već usluženi zahtevi redom>],"head":<broj ili null>,"direction":"up"|"down"|null,"algorithms":["SSTF","SCAN","C-SCAN","LOOK","C-LOOK","FCFS" - samo traženi],"cylinders":<broj ili null>,"surfaces":<broj strana ili null>}
        {"type":"page_replacement","frames":<broj okvira>,"reference":"<niz referenci tačno kako piše>","algorithms":["FIFO","LRU","OPT" - samo traženi]}
        {"type":"inode","blockSize":"<veličina bloka sa jedinicom>","direct":<broj direktnih adresa>,"addressSize":"<veličina adrese>","indirectLevels":<0-3>,"fileSizes":["<veličina fajla sa jedinicom>"]}
        {"type":"address","virtualBits":<broj bita virtuelne adrese ili null>,"segments":<maks. broj segmenata ili null>,"pageSize":"<veličina stranice sa jedinicom>","entrySize":"<veličina stavke tabele stranica>","levels":<broj nivoa tabele stranica>}
        {"type":"banker","processNames":["<imena procesa po redu ili null>"],"allocation":[[<red po procesu>]],"max":[[<red po procesu>]],"available":[<raspoloživi resursi>],"request":{"process":"<ime procesa>","amounts":[<traženi resursi>]} ili null}
        {"type":"none"}

        ZADATAK:
        {{task}}

        JSON:
        """;

    // ---- parsiranje i rešavanje (bez LLM-a, testira se direktno) ----

    private static SolverResult Hinted(string type, string text) => new(type, text, HintsFor(type));

    internal static IReadOnlyList<string>? HintsFor(string type) => Hints.TryGetValue(type, out var h) ? h : null;

    private static readonly Dictionary<string, string[]> Hints = new()
    {
        ["fork"] = new[]
        {
            "Prati izvršavanje proces po proces. fork() vraća roditelju pid deteta (različito od 0), a detetu 0. Zato u if(fork()) ulazi roditelj, a dete preskače telo; u if(!fork()) ulazi dete. Svaki izvršeni fork() stvara tačno jedan novi proces.",
            "Nacrtaj stablo procesa: za svaki fork() koji se izvršava dodaj po jedno dete, a za svaki proces posebno proveri koje naredbe posle toga izvršava (zavisi od povratne vrednosti i uslova). Ukupno procesa = 1 + broj izvršenih fork() poziva u svim procesima; exec i return završavaju proces.",
        },
        ["scheduling"] = new[]
        {
            "Simuliraj izvršavanje po milisekundama. U svakom trenutku odredi koji su procesi stigli (vreme aktiviranja) i nisu završeni, pa izaberi jedan po algoritmu: SJF najkraća CPU faza, SRTF najmanje preostalo vreme (uz prekidanje), prioritet manji broj je veći prioritet, Round Robin po redu uz vremenski kvant.",
            "Kod algoritama sa prekidanjem (SRTF, prioritet sa prekidanjem) pri svakom pristizanju novog procesa proveri da li ima prednost nad onim koji se izvršava. Kod Round Robin novopristigli proces ide u red pre procesa kome je istekao kvant. Nacrtaj Gantov dijagram i očitaj ko se izvršava u traženom intervalu.",
        },
        ["io_scheduling"] = new[]
        {
            "Faze procesa idu redom: CPU, U/I, CPU... (npr. 4-(5)-3). U/I uređaji rade paralelno, a procesor izvršava samo jedan proces. Kod multiprogramiranja procesor dok jedan proces čeka na U/I izvršava drugi; bez multiprogramiranja procesor miruje tokom U/I faze, a sledeći proces počinje tek kad prethodni završi.",
            "Prati vreme po vreme: ko je u CPU fazi, ko u U/I fazi i kada postaje spreman. FCFS ne prekida CPU fazu; SRTF pri svakom novom spremnom procesu poredi preostalo vreme tekuće CPU faze. Iskorišćenost CPU = ukupno CPU vreme svih procesa / ukupno vreme do završetka poslednjeg procesa.",
        },
        ["disk"] = new[]
        {
            "Odredi gde je glava diska i u kom smeru se kreće (iz poslednja dva usluzena zahteva), pa preostale zahteve podeli na one ispred i iza glave. SSTF uvek bira najbliži cilindar. SCAN ide u smeru kretanja do kraja pa se vraća, a C-SCAN posle kraja skače na suprotni kraj i nastavlja u istom smeru.",
            "Broj staza = broj strana x broj cilindara. Za SCAN i LOOK poređaj zahteve u smeru kretanja, pa zahteve u suprotnom smeru obrnutim redom; za C-SCAN i C-LOOK posle poslednjeg zahteva u smeru kretanja preskoči na suprotni kraj i nastavi u istom smeru.",
        },
        ["page_replacement"] = new[]
        {
            "Prolazi kroz niz referenci redom i vodi stanje okvira. Ako je stranica već u okviru, nema greške; ako nije, nastaje greška stranice: popuni slobodan okvir ili izbaci stranicu po algoritmu (FIFO najranije učitana, LRU najdavnije korišćena, OPT ona koja će se najkasnije koristiti).",
            "Za LRU posle svake reference zabeleži vreme poslednjeg korišćenja stranica u okvirima (pogodak takođe osvežava vreme). Prebroj greške stranica; popunjavanje praznih okvira na početku se računa kao greška.",
        },
        ["inode"] = new[]
        {
            "Broj adresa po bloku = veličina bloka / veličina adrese. Broj blokova sa podacima = veličina fajla / veličina bloka, zaokruženo naviše. Prvo se koriste direktne adrese, pa jednostruko, dvostruko i trostruko indirektni blok.",
            "Indeksni blokovi: jednostruko indirektna adresa troši 1 blok; dvostruko troši 1 blok plus blokove sledećeg nivoa; trostruko isto kroz tri nivoa. Broj indeksnih blokova na nivou je zaokruženo (preostali blokovi / adresa po bloku na tom nivou). Pristupa disku do poslednjeg bloka (fajl otvoren) = broj nivoa indirekcije + 1.",
        },
        ["address"] = new[]
        {
            "Pomeraj u stranici = log2(veličina stranice). Stavki po tabeli stranica = veličina stranice / veličina stavke; broj bita indeksa po nivou = log2(broj stavki). Ako postoji segmentacija, za segment treba log2(broj segmenata) bita.",
            "Korisni bitovi = (segment) + bitovi indeksa po nivoima + pomeraj. Neiskorišćeni bitovi = veličina virtuelne adrese minus korisni bitovi. Veličina virtuelnog adresnog prostora je 2 na ukupan broj korisnih bita, u bajtovima.",
        },
        ["banker"] = new[]
        {
            "Matrica potreba (Need) = Max - Allocation. Zatim traži proces čije potrebe staju u raspoložive resurse; kada završi, vraća svoje dodeljene resurse pa raspoloživi rastu. Stanje je bezbedno ako se tako svi procesi mogu završiti nekim redosledom.",
            "Za zahtev procesa proveri redom: zahtev <= potrebe procesa i zahtev <= raspoloživo. Zatim probno dodeli resurse (smanji raspoloživo, povećaj dodeljeno, smanji potrebe) i ponovo proveri bezbednost; ako je stanje nebezbedno, proces mora da čeka.",
        },
    };

    // task (tekst zadatka) je opcioni: koristi se za proveru onoga što je LLM izvukao (traženi algoritmi, interval).
    internal static SolverResult? SolveFromJson(string raw, string? task = null)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start < 0 || end < start) return null;
            using var doc = JsonDocument.Parse(raw[start..(end + 1)], new JsonDocumentOptions { AllowTrailingCommas = true });
            var root = doc.RootElement;
            return Str(root, "type")?.ToLowerInvariant() switch
            {
                "scheduling" => Scheduling(root, task),
                "io_scheduling" => IoSchedulingSolve(root, task),
                "disk" => Disk(root),
                "page_replacement" => Pages(root),
                "inode" => Inode(root),
                "address" => Address(root),
                "banker" => BankerSolve(root),
                _ => null,
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static readonly System.Text.RegularExpressions.Regex IntervalInText =
        new(@"period\w*\s*(\d+)\s*(?:ms)?\s*[-–]\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    // Algoritam se prihvata samo ako ga zadatak zaista pominje (LLM ponekad doda nezatražene).
    private static bool MentionedIn(string task, string algorithm)
    {
        var t = task.ToLowerInvariant();
        return algorithm switch
        {
            "SJF" => t.Contains("sjf"),
            "SRTF" => t.Contains("srtf"),
            "FCFS" => t.Contains("fcfs") || t.Contains("fifo") || t.Contains("first come"),
            "RR" => t.Contains("rr") || t.Contains("round"),
            "MLFQ" => t.Contains("feedback") || t.Contains("mlfq"),
            _ => t.Contains("prioritet") || t.Contains("priority"),
        };
    }

    private static SolverResult? Scheduling(JsonElement root, string? task)
    {
        if (!root.TryGetProperty("processes", out var arr) || arr.ValueKind != JsonValueKind.Array) return null;
        var procs = new List<Proc>();
        foreach (var p in arr.EnumerateArray())
        {
            var name = Str(p, "name");
            var burst = Int(p, "burst");
            if (name == null || burst == null) return null;
            procs.Add(new Proc(name, burst.Value, Int(p, "arrival") ?? 0, Int(p, "priority")));
        }
        if (procs.Count == 0) return null;

        var quantum = Int(root, "quantum") ?? 0;
        int? from = null, to = null;
        if (root.TryGetProperty("interval", out var iv) && iv.ValueKind == JsonValueKind.Array && iv.GetArrayLength() == 2)
        {
            from = ToInt(iv[0]);
            to = ToInt(iv[1]);
        }

        if (from == null && task != null && IntervalInText.Match(task) is { Success: true } m)
        {
            from = int.Parse(m.Groups[1].Value);
            to = int.Parse(m.Groups[2].Value);
        }

        var algorithms = StrList(root, "algorithms").Select(NormalizeSchedulingName).Where(a => a != null).Distinct().ToList();
        if (task != null)
        {
            var mentioned = algorithms.Where(a => MentionedIn(task, a!)).ToList();
            if (mentioned.Count > 0) algorithms = mentioned;
        }
        // LLM ponekad izostavi ML Feedback iz liste, a zadatak ga traži.
        if (task != null && MentionedIn(task, "MLFQ") && !algorithms.Contains("MLFQ")) algorithms.Add("MLFQ");
        if (algorithms.Count == 0) return null;

        var sb = new StringBuilder("Raspoređivanje procesa (simulacija po milisekundama):\n");
        var solved = 0;
        foreach (var alg in algorithms)
        {
            ScheduleResult result;
            try { result = alg == "MLFQ" ? CpuScheduling.SimulateFeedback(procs, FeedbackBase(task), FeedbackExponential(task)) : CpuScheduling.Simulate(alg!, procs, quantum); }
            catch (ArgumentException) { continue; }
            solved++;
            var label = alg switch
            {
                "RR" => $"RR (q={quantum}ms)",
                "MLFQ" => FeedbackExponential(task) ? $"ML Feedback (kvant reda i = {FeedbackBase(task)}*2^i ms, RQ0 prvi)" : $"ML Feedback (stalan kvant {FeedbackBase(task)}ms)",
                _ => alg,
            };
            sb.AppendLine($"- {label}: {string.Join(" | ", result.Timeline.Select(s => $"{s.Name} {s.Start}-{s.End}"))}");
            if (from != null && to != null)
                sb.AppendLine($"    U periodu {from}-{to}ms izvršava se: {string.Join(", ", CpuScheduling.WhoRuns(result, from.Value, to.Value))}");
            sb.AppendLine($"    Završetak procesa: {string.Join(", ", procs.Select(p => $"{p.Name}={result.Completion[p.Name]}"))}");
        }
        return solved == 0 ? null : Hinted("scheduling", sb.ToString().TrimEnd());
    }

    // Faze se čitaju direktno iz teksta zadatka (zapis 4-(5)-3), LLM daje samo imena procesa i vremena aktiviranja.
    private static SolverResult? IoSchedulingSolve(JsonElement root, string? task)
    {
        if (task == null || !root.TryGetProperty("processes", out var arr) || arr.ValueKind != JsonValueKind.Array) return null;
        var notations = IoScheduling.PhasesInText(task);
        var jsonProcs = arr.EnumerateArray().ToList();
        if (notations.Count == 0 || notations.Count != jsonProcs.Count) return null;

        var procs = new List<IoProc>();
        for (var i = 0; i < notations.Count; i++)
        {
            var name = Str(jsonProcs[i], "name");
            if (name == null) return null;
            procs.Add(new IoProc(name, Int(jsonProcs[i], "arrival") ?? 0, IoScheduling.ParsePhases(notations[i])));
        }

        var t = task.ToLowerInvariant();
        var policies = new List<string>();
        if (t.Contains("fcfs") || t.Contains("fifo")) policies.Add("FCFS");
        if (t.Contains("srtf")) policies.Add("SRTF");
        if (System.Text.RegularExpressions.Regex.IsMatch(t, @"bez\s+multi|monoprogram")) policies.Add("NO_MULTIPROGRAMMING");
        if (policies.Count == 0) policies.AddRange(IoScheduling.Supported);

        var sb = new StringBuilder("Raspoređivanje procesa sa U/I fazama (simulacija po milisekundama):\n");
        sb.AppendLine($"Procesi: {string.Join("; ", procs.Select(p => $"{p.Name} (aktiviranje {p.Arrival}ms, faze {string.Join("-", p.Phases.Select((x, i) => i % 2 == 1 ? $"({x})" : x.ToString()))})"))}");
        var any = false;
        foreach (var policy in policies)
        {
            IoScheduleResult r;
            try { r = IoScheduling.Simulate(policy, procs); }
            catch (ArgumentException) { continue; }
            any = true;
            var label = policy switch { "FCFS" => "Multiprogramiranje, FCFS", "SRTF" => "Multiprogramiranje, SRTF", _ => "Bez multiprogramiranja" };
            sb.AppendLine($"- {label}: CPU: {string.Join(" | ", r.CpuTimeline.Select(s => $"{s.Name} {s.Start}-{s.End}"))}");
            sb.AppendLine($"    Završetak procesa: {string.Join(", ", procs.Select(p => $"{p.Name}={r.Completion[p.Name]}"))}; ukupno vreme {r.Makespan}ms");
            sb.AppendLine($"    Iskorišćenost CPU: {r.CpuBusy}/{r.Makespan} = {r.Utilization.ToString("0.#", CultureInfo.InvariantCulture)}%");
        }
        return any ? Hinted("io_scheduling", sb.ToString().TrimEnd()) : null;
    }

    // "ML Feedback q=2^i" (ili "q=2i") = kvant raste sa nivoom (1, 2, 4...); "ML Feedback q=3" = stalan kvant.
    private static readonly System.Text.RegularExpressions.Regex FixedFeedbackQuantum =
        new(@"feedback[^\d]{0,12}q\s*=\s*(\d+)(?!\s*\^?\s*i)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static bool FeedbackExponential(string? task) => task == null || !FixedFeedbackQuantum.IsMatch(task);

    private static int FeedbackBase(string? task) =>
        task != null && FixedFeedbackQuantum.Match(task) is { Success: true } m ? int.Parse(m.Groups[1].Value) : 1;

    private static string? NormalizeSchedulingName(string name) => name.ToUpperInvariant().Replace(" ", "_").Replace("-", "_") switch
    {
        "SJF" => "SJF",
        "SRTF" => "SRTF",
        "FCFS" or "FIFO" => "FCFS",
        "PRIORITY" or "PRIORITET" or "PRIORITY_NON_PREEMPTIVE" => "PRIORITY",
        "PRIORITY_PREEMPTIVE" or "PREEMPTIVE_PRIORITY" => "PRIORITY_PREEMPTIVE",
        "RR" or "ROUND_ROBIN" or "ROUNDROBIN" => "RR",
        "MLFQ" or "ML_FEEDBACK" or "FEEDBACK" or "MULTILEVEL_FEEDBACK" => "MLFQ",
        _ => null,
    };

    private static SolverResult? Disk(JsonElement root)
    {
        var sb = new StringBuilder("Disk:\n");
        var any = false;

        var cylinders = Int(root, "cylinders");
        var surfaces = Int(root, "surfaces");
        if (cylinders != null && surfaces != null)
        {
            sb.AppendLine($"- Broj staza: {surfaces} strana x {cylinders} cilindara = {(long)surfaces.Value * cylinders.Value}");
            any = true;
        }

        var requests = IntList(root, "requests");
        if (requests.Count > 0)
        {
            var plan = DiskScheduling.Prepare(requests, IntList(root, "served"), Int(root, "head"), Str(root, "direction"));
            if (plan != null)
            {
                var algorithms = StrList(root, "algorithms");
                if (algorithms.Count == 0) algorithms = new List<string> { "SSTF", "SCAN", "C-SCAN" };
                sb.AppendLine($"- Glava je na cilindru {plan.Head}, kreće se ka {(plan.Direction == "down" ? "nižim" : "višim")} cilindrima; preostali zahtevi: [{string.Join(", ", plan.Remaining)}]");
                foreach (var alg in algorithms)
                {
                    try
                    {
                        var order = DiskScheduling.Order(alg, plan);
                        sb.AppendLine($"- {DiskScheduling.NormalizeName(alg)}: {string.Join(" -> ", order)}");
                        any = true;
                    }
                    catch (ArgumentException) { }
                }
            }
        }
        return any ? Hinted("disk", sb.ToString().TrimEnd()) : null;
    }

    private static SolverResult? Pages(JsonElement root)
    {
        var frames = Int(root, "frames");
        var reference = Str(root, "reference");
        if (frames == null || string.IsNullOrWhiteSpace(reference)) return null;

        var refs = PageReplacement.Tokenize(reference);
        var algorithms = StrList(root, "algorithms");
        if (algorithms.Count == 0) algorithms = new List<string> { "FIFO", "LRU", "OPT" };

        var sb = new StringBuilder($"Zamena stranica ({frames} okvira, niz referenci: {string.Join(" ", refs)}):\n");
        var any = false;
        foreach (var alg in algorithms)
        {
            PageResult result;
            try { result = PageReplacement.Run(alg, refs, frames.Value); }
            catch (ArgumentException) { continue; }
            any = true;
            sb.AppendLine($"- {result.Algorithm}: {result.Faults} grešaka stranica");
            foreach (var step in result.Steps)
            {
                var state = string.Join(" ", step.Frames.Select(f => f ?? "-"));
                var note = step.Fault ? (step.Evicted != null ? $"greška, izbačena {step.Evicted}" : "greška") : "pogodak";
                sb.AppendLine($"    {step.Page}: [{state}] {note}");
            }
        }
        return any ? Hinted("page_replacement", sb.ToString().TrimEnd()) : null;
    }

    private static SolverResult? Inode(JsonElement root)
    {
        var blockSize = SizeParser.Parse(Str(root, "blockSize"));
        var direct = Int(root, "direct");
        var addressSize = SizeParser.Parse(Str(root, "addressSize"));
        var levels = Int(root, "indirectLevels") ?? 3;
        var sizes = StrList(root, "fileSizes");
        if (sizes.Count == 0 && Str(root, "fileSize") is string single) sizes.Add(single);
        if (blockSize == null || direct == null || addressSize == null || sizes.Count == 0) return null;

        var sb = new StringBuilder($"I-node (blok {SizeParser.Format(blockSize.Value)}, {direct} direktnih adresa, adresa {addressSize} B, {blockSize / addressSize} adresa po bloku, nivoa indirekcije: {levels}):\n");
        foreach (var text in sizes)
        {
            var bytes = SizeParser.Parse(text);
            if (bytes == null) return null;
            var r = FileSystemCalc.Inode(bytes.Value, blockSize.Value, direct.Value, addressSize.Value, levels);
            sb.AppendLine($"- Fajl {text}: {r.DataBlocks} blokova sa podacima + {r.IndexBlocks} indeksnih = {r.DataBlocks + r.IndexBlocks} blokova; " +
                          $"pristupa disku do poslednjeg bloka (fajl otvoren): {r.AccessesToLastBlock}");
        }
        return Hinted("inode", sb.ToString().TrimEnd());
    }

    private static SolverResult? Address(JsonElement root)
    {
        var pageSize = SizeParser.Parse(Str(root, "pageSize"));
        var entrySize = SizeParser.Parse(Str(root, "entrySize"));
        if (pageSize == null || entrySize == null) return null;
        var levels = Int(root, "levels") ?? 1;
        var segments = Int(root, "segments");
        var virtualBits = Int(root, "virtualBits");

        var a = AddressCalc.Format(virtualBits, segments, pageSize.Value, entrySize.Value, levels);
        var sb = new StringBuilder("Format virtuelne adrese:\n");
        sb.AppendLine($"- Stranica {SizeParser.Format(pageSize.Value)} -> pomeraj {a.OffsetBits} bita");
        sb.AppendLine($"- Tabela stranica ima {SizeParser.Format(pageSize.Value)} / {entrySize} B = {a.EntriesPerTable} stavki -> {a.PageIndexBits[0]} bita indeksa po nivou ({levels} {(levels == 1 ? "nivo" : "nivoa")}, ukupno {a.PageIndexBits.Sum()} bita)");
        if (a.SegmentBits is int sb2) sb.AppendLine($"- {segments} segmenata -> {sb2} bita za segment");
        sb.AppendLine($"- Format: {a.Layout}");
        sb.AppendLine($"- Korisnih bita: {a.UsedBits}" + (a.UnusedBits is int u ? $"; neiskorišćenih od {virtualBits}: {u}" : ""));
        sb.AppendLine($"- Veličina virtuelnog adresnog prostora: 2^{a.UsedBits} B = {SizeParser.Format(1L << a.UsedBits)}");
        return Hinted("address", sb.ToString().TrimEnd());
    }

    private static SolverResult? BankerSolve(JsonElement root)
    {
        var allocation = Matrix(root, "allocation");
        var max = Matrix(root, "max");
        var needGiven = Matrix(root, "need");
        var available = IntList(root, "available").ToArray();
        var n = allocation.Length;
        if (n == 0 || available.Length == 0 || (max.Length == 0 && needGiven.Length == 0)) return null;
        var resources = available.Length;
        if (allocation.Any(r => r.Length != resources)) return null;
        if (max.Length > 0 && (max.Length != n || max.Any(r => r.Length != resources))) return null;
        if (needGiven.Length > 0 && (needGiven.Length != n || needGiven.Any(r => r.Length != resources))) return null;

        var need = needGiven.Length > 0 ? needGiven : Banker.Need(max, allocation);
        if (need.Any(r => r.Any(x => x < 0))) return null;

        string? requestName = null;
        int[]? amounts = null;
        if (root.TryGetProperty("request", out var req) && req.ValueKind == JsonValueKind.Object)
        {
            requestName = Str(req, "process");
            amounts = IntList(req, "amounts").ToArray();
            if (requestName == null || amounts.Length != resources) { requestName = null; amounts = null; }
        }

        var names = StrList(root, "processNames");
        if (names.Count != n)
        {
            var oneBased = requestName != null && requestName.Equals($"P{n}", StringComparison.OrdinalIgnoreCase);
            names = Enumerable.Range(0, n).Select(i => $"P{i + (oneBased ? 1 : 0)}").ToList();
        }

        var sb = new StringBuilder($"Bankerov algoritam ({n} procesa, {resources} vrsta resursa):\n");
        sb.AppendLine("- Matrica potreba (Max - Allocation):");
        for (var i = 0; i < n; i++) sb.AppendLine($"    {names[i]}: {string.Join(" ", need[i])}");
        sb.AppendLine($"- Raspoloživo: {string.Join(" ", available)}");

        var safety = Banker.IsSafe(allocation, need, available);
        sb.AppendLine(safety.Safe
            ? $"- Stanje je BEZBEDNO. Bezbedan redosled: {string.Join(" -> ", safety.Sequence.Select(i => names[i]))}"
            : $"- Stanje je NEBEZBEDNO (proces koji može da završi do kraja: {(safety.Sequence.Count == 0 ? "nijedan" : string.Join(", ", safety.Sequence.Select(i => names[i])))}).");

        if (requestName != null && amounts != null)
        {
            var idx = names.FindIndex(x => x.Equals(requestName, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                var r = Banker.Request(allocation, need, available, idx, amounts);
                sb.AppendLine($"- Zahtev {names[idx]} ({string.Join(", ", amounts)}): {r.Verdict}");
                if (r.SafetyAfter is { Safe: true } after)
                    sb.AppendLine($"    Bezbedan redosled posle odobravanja: {string.Join(" -> ", after.Sequence.Select(i => names[i]))}");
            }
        }
        return Hinted("banker", sb.ToString().TrimEnd());
    }

    // ---- JSON pomoćne ----

    private static int[][] Matrix(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array) return Array.Empty<int[]>();
        return arr.EnumerateArray()
            .Where(row => row.ValueKind == JsonValueKind.Array)
            .Select(row => row.EnumerateArray().Select(v => ToInt(v) ?? 0).ToArray())
            .ToArray();
    }

    private static string? Str(JsonElement e, string name)
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            _ => null,
        };
    }

    private static int? Int(JsonElement e, string name) =>
        Str(e, name) is string s && double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? (int)d : null;

    private static int? ToInt(JsonElement v) =>
        v.ValueKind == JsonValueKind.Number ? (int)v.GetDouble()
        : v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var i) ? i : null;

    private static List<int> IntList(JsonElement e, string name)
    {
        var list = new List<int>();
        if (e.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var v in arr.EnumerateArray())
                if (ToInt(v) is int i) list.Add(i);
        return list;
    }

    private static List<string> StrList(JsonElement e, string name)
    {
        var list = new List<string>();
        if (e.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var v in arr.EnumerateArray())
                if (v.ValueKind == JsonValueKind.String && v.GetString() is string s) list.Add(s);
                else if (v.ValueKind == JsonValueKind.Number) list.Add(v.GetRawText());
        return list;
    }
}
