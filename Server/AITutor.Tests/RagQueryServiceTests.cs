using AITutor.Core.Data;
using AITutor.Core.Rag;
using AITutor.Core.Subjects;
using AITutor.Core.Subjects.OperatingSystems;

namespace AITutor.Tests;

public class RagQueryServiceTests
{
    private readonly FakeEmbeddings _emb = new();
    private readonly FakeChat _chat = new();
    private readonly FakeIntent _intent = new();
    private readonly FakeRepo _repo = new();
    private readonly FakeSolver _solver = new();
    private readonly RagQueryService _svc;

    public RagQueryServiceTests()
    {
        var registry = new SubjectRegistry(new ISubjectProfile[]
        {
            new TestProfile("Programski prevodioci", _solver),
            new OperatingSystemsProfile(_chat, _solver),
        });
        _svc = new RagQueryService(_emb, _repo, _chat, _intent, registry);
        _repo.Hybrid = new List<ChunkSearchResult>
        {
            new() { Id = 1, SourceType = "vezbe", SourceFile = "Zadatak - While petlja.pdf", Content = "Postavka zadatka o while petlji", Distance = 0.25 },
            new() { Id = 3, SourceType = "predavanja", SourceFile = "PP 10 Medjukodovi.pdf", Content = "Teorija medjukoda", Distance = 0.30 },
        };
        _repo.Next[1] = new ChunkSearchResult { Id = 2, SourceType = "vezbe", SourceFile = "Zadatak - While petlja.pdf", Content = "Resenje: startLab endLab", IsNeighbor = true };
        _repo.Dense = new List<ChunkSearchResult>
        {
            new() { Id = 10, SourceType = "predavanja", SourceFile = "PP 06.pdf", Content = "FOLLOW definicija", Distance = 0.30 },
        };
    }

    [Fact]
    public async Task New_task_starts_at_hint_level_one_without_solving()
    {
        _intent.Result = ChatIntent.Task;
        var answer = await _svc.AnswerAsync("Napisati translate za while petlju");

        Assert.Equal(1, answer.Task!.Level);
        Assert.Contains("Sličan zadatak u materijalu: vezbe/Zadatak - While petlja.pdf", answer.Reply);
        Assert.Contains("NAGOVEŠTAJ", _chat.Prompts.Single());
        Assert.Equal(400, _chat.Options.Single()!.MaxTokens);
    }

    [Fact]
    public async Task Show_solution_jumps_to_level_three_and_appends_verbatim_example()
    {
        var answer = await _svc.AnswerAsync("Prikaži rešenje", null, new TaskState("zadatak o while petlji", 1));

        Assert.Equal(3, answer.Task!.Level);
        Assert.Equal(0, _intent.Calls);
        Assert.Contains("Rešen primer iz materijala (vezbe/Zadatak - While petlja.pdf)", answer.Reply);
        Assert.Contains("startLab endLab", answer.Reply);
        Assert.Contains("automatski generisao", answer.Reply);
    }

    [Fact]
    public async Task Reference_solution_comes_from_the_semantically_closest_example()
    {
        _repo.Hybrid = new List<ChunkSearchResult>
        {
            new() { Id = 5, SourceType = "vezbe", SourceFile = "Daleki.pdf", Content = "daleki zadatak", Distance = 0.50 },
            new() { Id = 1, SourceType = "vezbe", SourceFile = "Blizak.pdf", Content = "blizak zadatak", Distance = 0.20 },
        };
        _repo.Next[1] = new ChunkSearchResult { Id = 2, SourceType = "vezbe", SourceFile = "Blizak.pdf", Content = "pravo resenje", IsNeighbor = true };
        _repo.Next[5] = new ChunkSearchResult { Id = 6, SourceType = "vezbe", SourceFile = "Daleki.pdf", Content = "tudje resenje", IsNeighbor = true };

        var answer = await _svc.AnswerAsync("Prikaži rešenje", null, new TaskState("zadatak", 2));

        Assert.Contains("Rešen primer iz materijala (vezbe/Blizak.pdf)", answer.Reply);
        Assert.Contains("pravo resenje", answer.Reply);
        Assert.DoesNotContain("tudje resenje", answer.Reply.Split("Rešen primer iz materijala")[1]);
    }

    [Fact]
    public async Task Solver_result_is_used_for_the_full_solution_without_calling_the_model()
    {
        _solver.Result = new SolverResult("inode", "Fajl 6MB: 1536 blokova");

        var answer = await _svc.AnswerAsync("Prikaži rešenje", null, new TaskState("zadatak o i-node", 1));

        Assert.Equal(3, answer.Task!.Level);
        Assert.Contains("izračunato programski", answer.Reply);
        Assert.Contains("Fajl 6MB: 1536 blokova", answer.Reply);
        Assert.Empty(_chat.Prompts);
    }

    [Fact]
    public async Task Hints_for_solver_tasks_are_prewritten_method_hints_and_never_call_the_model()
    {
        _solver.Result = new SolverResult("fork", "Broj procesa: 4", new[] { "NAGOVESTAJ-1", "NAGOVESTAJ-2" });

        var first = await _svc.AnswerAsync("zadatak sa fork", null, null);
        _intent.Result = ChatIntent.Task;
        var one = await _svc.AnswerAsync("Napisati zadatak sa fork", null, null);
        var two = await _svc.AnswerAsync("Sledeći nagoveštaj", null, new TaskState("zadatak", 1));

        Assert.Contains("NAGOVESTAJ-1", one.Reply);
        Assert.Contains("NAGOVESTAJ-2", two.Reply);
        Assert.DoesNotContain("Broj procesa: 4", one.Reply + two.Reply);
        Assert.Equal(2, two.Task!.Level);
        Assert.DoesNotContain(_chat.Prompts, p => p.Contains("NAGOVESTAJ"));
    }

    [Fact]
    public async Task Hints_fall_back_to_the_model_when_the_solver_has_none()
    {
        _solver.Result = new SolverResult("x", "y");

        await _svc.AnswerAsync("Sledeći nagoveštaj", null, new TaskState("zadatak", 1));

        Assert.Single(_chat.Prompts);
    }

    [Fact]
    public async Task Full_solution_falls_back_to_the_model_when_no_solver_applies()
    {
        _solver.Result = null;

        var answer = await _svc.AnswerAsync("Prikaži rešenje", null, new TaskState("zadatak", 2));

        Assert.Equal(1, _solver.Calls);
        Assert.Single(_chat.Prompts);
        Assert.Contains("automatski generisao", answer.Reply);
    }

    [Fact]
    public async Task Distance_threshold_depends_on_the_subject()
    {
        _repo.Dense[0].Distance = 0.60;

        var pp = await _svc.AnswerAsync("pitanje", null, null, "Programski prevodioci");
        var os = await _svc.AnswerAsync("pitanje", null, null, "Operativni sistemi");

        Assert.Contains("Nemam dovoljno informacija", pp.Reply);
        Assert.DoesNotContain("Nemam dovoljno informacija", os.Reply);
    }

    [Fact]
    public async Task Sync_task_always_gets_verified_patterns_and_the_course_notation()
    {
        _repo.Hybrid = new List<ChunkSearchResult>
        {
            new() { Id = 20, SourceType = "predavanja", SourceFile = "4. Konkurentnost.pdf", Content = "slajd o monitorima", Distance = 0.20 },
            new() { Id = 21, SourceType = "obrasci", SourceFile = "Obrasci.txt", Content = "OBRAZAC-RANDEZVOUS monitor Randezvous", Distance = 0.55 },
        };
        _solver.Result = null;

        await _svc.AnswerAsync("Prikaži rešenje", null, new TaskState("Napisati monitor za sinhronizaciju dva procesa", 2), "Operativni sistemi");

        var prompt = _chat.Prompts.Single();
        Assert.Contains("OBRAZAC-RANDEZVOUS", prompt);
        Assert.Contains("semWait(s)", prompt);
        Assert.Contains("cwait(c)", prompt);
    }

    [Theory]
    [InlineData("Odgovoriti sa Da, ili Ne: a) x b) y", true)]
    [InlineData("Na pitanja odgovoriti sa Da ili Ne + objasnjenje", true)]
    [InlineData("U kom modu je sistem: a) strlen() b) read() c) sin() d) fork()", true)]
    [InlineData("Sta je semafor?", false)]
    [InlineData("Objasni razliku izmedju a) procesa i b) niti", false)]
    public void Multi_part_questions_are_detected(string question, bool expected) =>
        Assert.Equal(expected, RagQueryService.IsMultiPartQuestion(question));

    [Fact]
    public async Task Multi_part_question_asks_for_an_answer_per_item()
    {
        await _svc.AnswerAsync("Odgovoriti sa Da ili Ne: a) x b) y c) z");

        Assert.Contains("SVAKU stavku", _chat.Prompts.Single());
        Assert.Equal(1000, _chat.Options.Single()!.MaxTokens);
    }

    [Fact]
    public void Question_is_split_into_its_items()
    {
        var items = RagQueryService.SplitItems("Odgovoriti sa Da ili Ne: a) niti dele stek b) malloc je u adresnom prostoru c) greska stranice azurira tabelu");
        Assert.Equal(3, items.Count);
        Assert.StartsWith("a) niti dele stek", items[0]);
        Assert.StartsWith("c) greska stranice", items[2]);
        Assert.Empty(RagQueryService.SplitItems("Sta je semafor?"));
    }

    [Fact]
    public async Task Multi_part_question_searches_material_for_each_item()
    {
        await _svc.AnswerAsync("Odgovoriti sa Da ili Ne: a) prva tvrdnja b) druga tvrdnja c) treca tvrdnja");

        // jedno ugradjivanje za celo pitanje + po jedno za svaku od 3 stavke
        Assert.Equal(4, _emb.Texts.Count);
    }

    [Fact]
    public async Task Plain_mode_answers_without_retrieval()
    {
        var answer = await _svc.AnswerAsync("Sta je semafor?", mode: AnswerMode.Plain);

        Assert.Empty(_emb.Texts);
        Assert.Single(_chat.Prompts);
        Assert.Empty(answer.RetrievedChunks);
        Assert.Equal(0, _intent.Calls);
    }

    [Fact]
    public async Task Next_hint_advances_one_level()
    {
        var answer = await _svc.AnswerAsync("Sledeći nagoveštaj", null, new TaskState("zadatak", 1));

        Assert.Equal(2, answer.Task!.Level);
        Assert.Contains("PRVA DVA KORAKA", _chat.Prompts.Single());
        Assert.Equal(350, _chat.Options.Single()!.MaxTokens);
    }

    [Fact]
    public async Task Hint_level_two_builds_on_the_plan_from_the_previous_reply()
    {
        var history = new List<ChatTurn>
        {
            new("user", "zadatak o while petlji"),
            new("assistant", "1. Prvi korak plana.\n2. Drugi korak plana.\n\nSličan zadatak u materijalu: vezbe/X.pdf\n\nPokušaj prvo sam."),
        };

        await _svc.AnswerAsync("Sledeći nagoveštaj", history, new TaskState("zadatak o while petlji", 1));

        var prompt = _chat.Prompts.Single();
        Assert.Contains("2. Drugi korak plana.", prompt);
        Assert.DoesNotContain("Pokušaj prvo sam.", prompt);
        Assert.Contains("Razradi rečima SAMO korake 1 i 2", prompt);
    }

    [Fact]
    public async Task Unrelated_question_during_active_task_goes_through_normal_routing()
    {
        _intent.Result = ChatIntent.Subject;
        var answer = await _svc.AnswerAsync("Sta je FOLLOW skup?", null, new TaskState("zadatak", 2));

        Assert.Equal(1, _intent.Calls);
        Assert.Null(answer.Task);
    }

    [Fact]
    public async Task Task_far_from_material_is_refused_without_calling_the_model()
    {
        _intent.Result = ChatIntent.Task;
        foreach (var c in _repo.Hybrid) c.Distance = 0.9;

        var answer = await _svc.AnswerAsync("Napisi pesmu o moru");

        Assert.Contains("Nemam dovoljno informacija", answer.Reply);
        Assert.Empty(_chat.Prompts);
    }

    [Fact]
    public async Task Subject_question_beyond_distance_threshold_is_refused_without_calling_the_model()
    {
        _repo.Dense[0].Distance = 0.7;

        var answer = await _svc.AnswerAsync("Sta je nuklearna fizika?");

        Assert.Contains("Nemam dovoljno informacija", answer.Reply);
        Assert.Empty(_chat.Prompts);
    }

    [Fact]
    public async Task Subject_is_passed_to_retrieval_and_exams_are_not_a_knowledge_source()
    {
        await _svc.AnswerAsync("Sta je LL(1) gramatika?", null, null, "Operativni sistemi");

        Assert.All(_repo.Subjects, s => Assert.Equal("Operativni sistemi", s));
        Assert.DoesNotContain("pismeni", _repo.LastDenseTypes!);
        Assert.Contains("predavanja", _repo.LastDenseTypes!);
    }

    [Fact]
    public async Task Default_subject_is_used_when_none_is_given()
    {
        _intent.Result = ChatIntent.Task;
        await _svc.AnswerAsync("zadatak");

        Assert.All(_repo.Subjects, s => Assert.Equal("Programski prevodioci", s));
    }

    [Fact]
    public async Task Short_follow_up_is_rewritten_before_retrieval_and_history_is_in_the_prompt()
    {
        _chat.Responder = p => p.Contains("SAMOSTALNO PITANJE:") ? "Sta je FOLLOW skup?" : "odgovor";
        var history = new List<ChatTurn> { new("user", "Sta je FIRST skup?"), new("assistant", "FIRST je skup terminala.") };

        await _svc.AnswerAsync("a FOLLOW?", history);

        Assert.Equal("Sta je FOLLOW skup?", _emb.Texts.Single());
        Assert.Contains("RANIJI RAZGOVOR", _chat.Prompts.Last());
        Assert.Contains("FIRST je skup terminala.", _chat.Prompts.Last());
    }

    [Fact]
    public async Task Long_question_is_not_rewritten()
    {
        var history = new List<ChatTurn> { new("user", "Sta je FIRST skup?"), new("assistant", "odgovor") };

        await _svc.AnswerAsync("Objasni mi detaljno kako se racuna FOLLOW skup neterminala u gramatici", history);

        Assert.Single(_chat.Prompts);
        Assert.StartsWith("Objasni mi detaljno", _emb.Texts.Single());
    }

    [Fact]
    public async Task Only_recent_history_turns_are_sent()
    {
        _intent.Result = ChatIntent.Casual;
        var history = Enumerable.Range(1, 10)
            .Select(i => new ChatTurn(i % 2 == 1 ? "user" : "assistant", $"poruka-{i}")).ToList();

        await _svc.AnswerAsync("hvala", history);

        var prompt = _chat.Prompts.Single();
        Assert.Contains("poruka-10", prompt);
        Assert.DoesNotContain("poruka-1\n", prompt);
        Assert.DoesNotContain("poruka-6", prompt);
    }
}
