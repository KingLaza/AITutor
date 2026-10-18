using AITutor.Core.Subjects.OperatingSystems;

namespace AITutor.Tests;

// SolveFromJson prima ono što LLM vrati (ponekad sa ograđenim blokom koda ili dopunskim tekstom).
public class TaskSolverJsonTests
{
    [Fact]
    public void Scheduling_json_is_solved_and_formatted()
    {
        const string json = """
            ```json
            {"type":"scheduling","processes":[
              {"name":"A","burst":50,"arrival":0,"priority":4},{"name":"B","burst":20,"arrival":20,"priority":1},
              {"name":"C","burst":100,"arrival":40,"priority":3},{"name":"D","burst":40,"arrival":60,"priority":2}],
             "algorithms":["SJF","SRTF","Round Robin","prioritet sa prekidanjem"],"quantum":30,"interval":[60,70]}
            ```
            """;
        var result = OsTaskSolver.SolveFromJson(json)!;

        Assert.Equal("scheduling", result.Type);
        Assert.Contains("SJF:", result.Text);
        Assert.Contains("RR (q=30ms)", result.Text);
        Assert.Contains("U periodu 60-70ms izvršava se: B", result.Text);
        Assert.Contains("U periodu 60-70ms izvršava se: A", result.Text);
    }

    [Fact]
    public void Disk_json_with_served_requests_derives_head_and_direction()
    {
        const string json = """{"type":"disk","requests":[25,100,360,77,125,222],"served":[100,77],"head":null,"direction":null,"algorithms":["SSTF","SCAN","C-SCAN"],"cylinders":16000,"surfaces":4}""";
        var result = OsTaskSolver.SolveFromJson(json)!;

        Assert.Contains("Broj staza: 4 strana x 16000 cilindara = 64000", result.Text);
        Assert.Contains("SSTF: 125 -> 222 -> 360 -> 25", result.Text);
        Assert.Contains("SCAN: 25 -> 125 -> 222 -> 360", result.Text);
        Assert.Contains("C-SCAN: 25 -> 360 -> 222 -> 125", result.Text);
    }

    [Fact]
    public void Page_replacement_json()
    {
        var result = OsTaskSolver.SolveFromJson("""{"type":"page_replacement","frames":3,"reference":"EDHBDEDAEBEDEBG","algorithms":["LRU"]}""")!;
        Assert.Contains("LRU: 9 grešaka stranica", result.Text);
    }

    [Fact]
    public void Inode_json_with_unit_strings()
    {
        const string json = """{"type":"inode","blockSize":"4KB","direct":10,"addressSize":8,"indirectLevels":3,"fileSizes":["40KB","6MB","1GB"]}""";
        var result = OsTaskSolver.SolveFromJson(json)!;

        Assert.Contains("Fajl 6MB: 1536 blokova sa podacima + 4 indeksnih", result.Text);
        Assert.Contains("Fajl 1GB: 262144 blokova sa podacima + 513 indeksnih", result.Text);
    }

    [Fact]
    public void Address_json()
    {
        var result = OsTaskSolver.SolveFromJson("""{"type":"address","virtualBits":64,"segments":null,"pageSize":"256KB","entrySize":8,"levels":2}""")!;
        Assert.Contains("neiskorišćenih od 64: 16", result.Text);
        Assert.Contains("P1 15 | P2 15 | pomeraj 18", result.Text);
    }

    [Fact]
    public void Interval_is_read_from_the_task_text_and_unrequested_algorithms_are_dropped()
    {
        const string json = """{"type":"scheduling","processes":[{"name":"A","burst":5,"arrival":0},{"name":"B","burst":3,"arrival":1}],"algorithms":["SJF","FCFS"],"quantum":null,"interval":null}""";
        var result = OsTaskSolver.SolveFromJson(json, "Koji proces se izvrsava u periodu 2-4ms po SJF?")!;

        Assert.Contains("U periodu 2-4ms izvršava se: A", result.Text);
        Assert.DoesNotContain("FCFS", result.Text);
    }

    [Fact]
    public void Banker_json_reports_need_safety_and_request_verdict()
    {
        const string json = """
            {"type":"banker","processNames":null,
             "allocation":[[0,1,0],[2,0,0],[3,0,2],[2,1,1],[0,0,2]],
             "max":[[7,5,3],[3,2,2],[9,0,2],[2,2,2],[4,3,3]],
             "available":[3,3,2],"request":{"process":"P1","amounts":[1,0,2]}}
            """;
        var result = OsTaskSolver.SolveFromJson(json)!;

        Assert.Equal("banker", result.Type);
        Assert.Contains("P0: 7 4 3", result.Text);
        Assert.Contains("Stanje je BEZBEDNO. Bezbedan redosled: P1 -> P3 -> P4 -> P0 -> P2", result.Text);
        Assert.Contains("Zahtev P1 (1, 0, 2): Zahtev se može odobriti", result.Text);
    }

    [Fact]
    public void Banker_json_with_inconsistent_dimensions_returns_null() =>
        Assert.Null(OsTaskSolver.SolveFromJson("""{"type":"banker","allocation":[[1,2],[3]],"max":[[3,3],[3,3]],"available":[1,1]}"""));

    [Theory]
    [InlineData("fork")]
    [InlineData("scheduling")]
    [InlineData("disk")]
    [InlineData("page_replacement")]
    [InlineData("inode")]
    [InlineData("address")]
    [InlineData("banker")]
    public void Every_supported_task_type_has_two_prewritten_hints(string type)
    {
        var hints = OsTaskSolver.HintsFor(type);
        Assert.NotNull(hints);
        Assert.Equal(2, hints!.Count);
        Assert.All(hints, h => Assert.True(h.Length > 40));
    }

    [Fact]
    public void Solver_results_carry_the_hints_of_their_type()
    {
        var result = OsTaskSolver.SolveFromJson("""{"type":"page_replacement","frames":3,"reference":"EDHBDEDAEBEDEBG","algorithms":["LRU"]}""")!;
        Assert.Equal(OsTaskSolver.HintsFor("page_replacement"), result.Hints);
    }

    [Fact]
    public void Ml_feedback_is_reported_as_unsupported()
    {
        const string json = """{"type":"scheduling","processes":[{"name":"A","burst":5,"arrival":0}],"algorithms":["RR"],"quantum":2}""";
        var result = OsTaskSolver.SolveFromJson(json, "Prikazati izvrsenje za RR (q=2) i ML Feedback")!;

        Assert.Contains("ML Feedback", result.Text);
        Assert.Contains("ne podržava", result.Text);
    }

    [Theory]
    [InlineData("""{"type":"none"}""")]
    [InlineData("nije json uopšte")]
    [InlineData("""{"type":"scheduling","processes":[]}""")]
    [InlineData("""{"type":"scheduling","processes":[{"name":"A","burst":5}],"algorithms":["PRIORITY"]}""")]
    [InlineData("""{"type":"disk","requests":[1,2,3],"served":[]}""")]
    [InlineData("""{"type":"page_replacement","frames":null,"reference":"ABC"}""")]
    [InlineData("""{"type":"inode","blockSize":"4KB","direct":10,"addressSize":8,"fileSizes":[]}""")]
    [InlineData("""{"type":"inode","blockSize":"4KB","direct":10,"addressSize":3,"indirectLevels":3,"fileSizes":["1GB"]}""")]
    [InlineData("""{"type":"address","pageSize":"3KB","entrySize":8,"levels":2}""")]
    public void Unsupported_or_invalid_input_returns_null(string raw) => Assert.Null(OsTaskSolver.SolveFromJson(raw));
}
