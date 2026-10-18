using AITutor.Core.Subjects.OperatingSystems;

namespace AITutor.Tests;

// Tačni odgovori su izračunati ručno za zadatke sa ispita iz Operativnih sistema.
public class SolverTests
{
    private static readonly Proc[] Jan24 =
    {
        new("A", 50, 0, 4), new("B", 20, 20, 1), new("C", 100, 40, 3), new("D", 40, 60, 2),
    };

    [Theory]
    [InlineData("SJF", "B")]
    [InlineData("SRTF", "A")]
    [InlineData("PRIORITY", "B")]
    [InlineData("PRIORITY_PREEMPTIVE", "D")]
    public void Scheduling_who_runs_between_60_and_70ms(string algorithm, string expected)
    {
        var result = CpuScheduling.Simulate(algorithm, Jan24);
        Assert.Equal(new List<string> { expected }, CpuScheduling.WhoRuns(result, 60, 70));
    }

    [Fact]
    public void Round_robin_with_quantum_30()
    {
        var result = CpuScheduling.Simulate("RR", Jan24, 30);
        Assert.Equal(new List<string> { "A" }, CpuScheduling.WhoRuns(result, 60, 70));
        Assert.Equal("A:0-30 B:30-50 A:50-70 C:70-100 D:100-130 C:130-160 D:160-170 C:170-210",
            string.Join(" ", result.Timeline.Select(s => $"{s.Name}:{s.Start}-{s.End}")));
        Assert.Equal(50, result.Completion["B"]);
        Assert.Equal(70, result.Completion["A"]);
    }

    [Fact]
    public void Srtf_timeline_matches_hand_calculation()
    {
        var result = CpuScheduling.Simulate("SRTF", Jan24);
        Assert.Equal("A:0-20 B:20-40 A:40-70 D:70-110 C:110-210",
            string.Join(" ", result.Timeline.Select(s => $"{s.Name}:{s.Start}-{s.End}")));
    }

    [Fact]
    public void Idle_time_is_recorded_when_nothing_is_ready()
    {
        var result = CpuScheduling.Simulate("FCFS", new[] { new Proc("A", 2, 0, null), new Proc("B", 2, 5, null) });
        Assert.Equal("A:0-2 idle:2-5 B:5-7", string.Join(" ", result.Timeline.Select(s => $"{s.Name}:{s.Start}-{s.End}")));
    }

    [Fact]
    public void Priority_requires_priorities() =>
        Assert.Throws<ArgumentException>(() => CpuScheduling.Simulate("PRIORITY", new[] { new Proc("A", 2, 0, null) }));

    [Fact]
    public void Disk_oct23_order_for_sstf_scan_cscan()
    {
        var plan = DiskScheduling.Prepare(new[] { 25, 100, 360, 77, 125, 222 }, new[] { 100, 77 }, null, null)!;
        Assert.Equal(77, plan.Head);
        Assert.Equal("down", plan.Direction);
        Assert.Equal(new[] { 125, 222, 360, 25 }, DiskScheduling.Order("SSTF", plan));
        Assert.Equal(new[] { 25, 125, 222, 360 }, DiskScheduling.Order("SCAN", plan));
        Assert.Equal(new[] { 25, 360, 222, 125 }, DiskScheduling.Order("C-SCAN", plan));
    }

    [Fact]
    public void Disk_jan24_order()
    {
        var plan = DiskScheduling.Prepare(new[] { 185, 190, 33, 400, 250, 200 }, new[] { 200, 190 }, null, null)!;
        Assert.Equal(new[] { 185, 250, 400, 33 }, DiskScheduling.Order("SSTF", plan));
        Assert.Equal(new[] { 185, 33, 250, 400 }, DiskScheduling.Order("SCAN", plan));
        Assert.Equal(new[] { 185, 33, 400, 250 }, DiskScheduling.Order("C-LOOK", plan));
    }

    [Fact]
    public void Disk_without_head_information_is_not_solvable() =>
        Assert.Null(DiskScheduling.Prepare(new[] { 1, 2, 3 }, Array.Empty<int>(), null, null));

    [Fact]
    public void Lru_with_three_frames_has_nine_faults()
    {
        var result = PageReplacement.Run("LRU", PageReplacement.Tokenize("EDHBDEDAEBEDEBG"), 3);
        Assert.Equal(9, result.Faults);
        Assert.Equal(new[] { "B", "E", "G" }, result.Steps[^1].Frames.OrderBy(x => x).ToArray());
    }

    [Fact]
    public void Classic_belady_reference_string()
    {
        var refs = PageReplacement.Tokenize("1 2 3 4 1 2 5 1 2 3 4 5");
        Assert.Equal(9, PageReplacement.Run("FIFO", refs, 3).Faults);
        Assert.Equal(10, PageReplacement.Run("FIFO", refs, 4).Faults);
        Assert.Equal(7, PageReplacement.Run("OPT", refs, 3).Faults);
    }

    [Theory]
    [InlineData("40KB", 10, 0)]
    [InlineData("6MB", 1536, 4)]
    [InlineData("1GB", 262144, 513)]
    public void Inode_oct23_blocks(string size, long data, long index)
    {
        var r = FileSystemCalc.Inode(SizeParser.Parse(size)!.Value, 4096, 10, 8, 3);
        Assert.Equal(data, r.DataBlocks);
        Assert.Equal(index, r.IndexBlocks);
    }

    [Fact]
    public void Inode_jan24_triple_indirect_and_four_disk_accesses()
    {
        var r = FileSystemCalc.Inode(SizeParser.Parse("1048B+516MB")!.Value, 2048, 10, 4, 3);
        Assert.Equal(264193, r.DataBlocks);
        Assert.Equal(519, r.IndexBlocks);
        Assert.Equal(4, r.AccessesToLastBlock);
    }

    [Fact]
    public void Inode_rejects_files_that_do_not_fit() =>
        Assert.Throws<ArgumentException>(() => FileSystemCalc.Inode(1L << 50, 1024, 10, 4, 1));

    [Fact]
    public void Address_oct23_unused_bits_and_layout()
    {
        var a = AddressCalc.Format(64, null, SizeParser.Parse("256KB")!.Value, 8, 2);
        Assert.Equal(18, a.OffsetBits);
        Assert.Equal(new[] { 15, 15 }, a.PageIndexBits);
        Assert.Equal(16, a.UnusedBits);
        Assert.Equal("P1 15 | P2 15 | pomeraj 18", a.Layout);
    }

    [Fact]
    public void Address_jan24_segmentation_with_two_level_paging()
    {
        var a = AddressCalc.Format(null, 512, SizeParser.Parse("4KB")!.Value, 8, 2);
        Assert.Equal(39, a.UsedBits);
        Assert.Equal("segment 9 | P1 9 | P2 9 | pomeraj 12", a.Layout);
    }

    [Theory]
    [InlineData("4KB", 4096L)]
    [InlineData("1048B+516MB", 1048L + 516L * 1024 * 1024)]
    [InlineData("2 KB", 2048L)]
    [InlineData("1GB", 1L << 30)]
    public void Size_parser_uses_powers_of_two(string text, long expected) =>
        Assert.Equal(expected, SizeParser.Parse(text));

    [Fact]
    public void Size_parser_returns_null_for_garbage() => Assert.Null(SizeParser.Parse("nema broja"));
}
