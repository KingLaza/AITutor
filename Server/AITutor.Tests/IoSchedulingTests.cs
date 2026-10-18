using AITutor.Core.Subjects.OperatingSystems;

namespace AITutor.Tests;

// Zadatak sa ispita: A = 4-(5)-3 (aktiviranje 0), B = 2-(4)-1 (aktiviranje 1); tačni odgovori izračunati ručno.
public class IoSchedulingTests
{
    private static readonly IoProc[] Procs = { new("A", 0, new[] { 4, 5, 3 }), new("B", 1, new[] { 2, 4, 1 }) };

    private const string Task =
        "U sistemu postoje 2 procesa, A i B sa trajanjem 4-(5)-3 (CPU faza 4, nakon koje sledi I/O faza od 5, a zatim CPU faza od 3) " +
        "i 2-(4)-1, respektivno. Proces A je startovan u trenutku 0, a B u trenutku 1. Odrediti vreme izvršenja procesa A i B i procentualnu " +
        "iskorišćenost CPU za a) multiprogramiranje sa FCFS b) multiprogramiranje sa SRTF c) bez multiprogramiranja";

    [Fact]
    public void Without_multiprogramming_processes_run_one_after_another_and_cpu_idles_during_io()
    {
        var r = IoScheduling.Simulate("NO_MULTIPROGRAMMING", Procs);

        Assert.Equal(12, r.Completion["A"]);
        Assert.Equal(19, r.Completion["B"]);
        Assert.Equal(19, r.Makespan);
        Assert.Equal(10, r.CpuBusy);
        Assert.Equal(52.6, r.Utilization, 1);
    }

    [Fact]
    public void Fcfs_multiprogramming_overlaps_io_with_the_other_process()
    {
        var r = IoScheduling.Simulate("FCFS", Procs);

        Assert.Equal("A:0-4 B:4-6 A:9-12 B:12-13", string.Join(" ", r.CpuTimeline.Select(s => $"{s.Name}:{s.Start}-{s.End}")));
        Assert.Equal(12, r.Completion["A"]);
        Assert.Equal(13, r.Completion["B"]);
        Assert.Equal(76.9, r.Utilization, 1);
    }

    [Fact]
    public void Srtf_preempts_when_a_process_with_a_shorter_remaining_burst_arrives()
    {
        var r = IoScheduling.Simulate("SRTF", Procs);

        Assert.Equal("A:0-1 B:1-3 A:3-6 B:7-8 A:11-14", string.Join(" ", r.CpuTimeline.Select(s => $"{s.Name}:{s.Start}-{s.End}")));
        Assert.Equal(14, r.Completion["A"]);
        Assert.Equal(8, r.Completion["B"]);
        Assert.Equal(71.4, r.Utilization, 1);
    }

    [Fact]
    public void Single_process_with_idle_gap_between_arrivals()
    {
        var r = IoScheduling.Simulate("FCFS", new[] { new IoProc("A", 5, new[] { 2 }) });

        Assert.Equal(7, r.Completion["A"]);
        Assert.Equal(7, r.Makespan);
    }

    [Fact]
    public void Phases_are_read_from_the_task_text()
    {
        Assert.Equal(new[] { "4-(5)-3", "2-(4)-1" }, IoScheduling.PhasesInText(Task));
        Assert.Equal(new[] { 4, 5, 3 }, IoScheduling.ParsePhases("4-(5)-3"));
    }

    [Fact]
    public void Solver_uses_names_and_arrivals_from_json_but_phases_and_policies_from_the_text()
    {
        var json = """{"type":"io_scheduling","processes":[{"name":"A","arrival":0},{"name":"B","arrival":1}]}""";

        var result = OsTaskSolver.SolveFromJson(json, Task)!;

        Assert.Equal("io_scheduling", result.Type);
        Assert.Contains("Multiprogramiranje, FCFS", result.Text);
        Assert.Contains("A=12, B=13", result.Text);
        Assert.Contains("A=14, B=8", result.Text);
        Assert.Contains("A=12, B=19", result.Text);
        Assert.Contains("10/19 = 52.6%", result.Text);
        Assert.Equal(2, result.Hints!.Count);
    }

    [Fact]
    public void Solver_gives_up_when_process_count_does_not_match_the_phase_notation()
    {
        var json = """{"type":"io_scheduling","processes":[{"name":"A","arrival":0}]}""";

        Assert.Null(OsTaskSolver.SolveFromJson(json, Task));
    }
}
