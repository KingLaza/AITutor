using AITutor.Core.Subjects.OperatingSystems;

namespace AITutor.Tests;

// Klasičan primer iz literature: 5 procesa, resursi A, B, C.
public class BankerTests
{
    private static readonly int[][] Allocation = { new[] { 0, 1, 0 }, new[] { 2, 0, 0 }, new[] { 3, 0, 2 }, new[] { 2, 1, 1 }, new[] { 0, 0, 2 } };
    private static readonly int[][] Max = { new[] { 7, 5, 3 }, new[] { 3, 2, 2 }, new[] { 9, 0, 2 }, new[] { 2, 2, 2 }, new[] { 4, 3, 3 } };
    private static readonly int[] Available = { 3, 3, 2 };

    [Fact]
    public void Need_is_max_minus_allocation()
    {
        var need = Banker.Need(Max, Allocation);
        Assert.Equal(new[] { 7, 4, 3 }, need[0]);
        Assert.Equal(new[] { 1, 2, 2 }, need[1]);
        Assert.Equal(new[] { 6, 0, 0 }, need[2]);
    }

    [Fact]
    public void Initial_state_is_safe_with_known_sequence()
    {
        var safety = Banker.IsSafe(Allocation, Banker.Need(Max, Allocation), Available);
        Assert.True(safety.Safe);
        Assert.Equal(new[] { 1, 3, 4, 0, 2 }, safety.Sequence);
    }

    [Fact]
    public void Request_that_keeps_the_system_safe_is_granted()
    {
        var r = Banker.Request(Allocation, Banker.Need(Max, Allocation), Available, 1, new[] { 1, 0, 2 });
        Assert.True(r.Granted);
        Assert.True(r.SafetyAfter!.Safe);
    }

    [Fact]
    public void Request_above_available_must_wait()
    {
        var r = Banker.Request(Allocation, Banker.Need(Max, Allocation), Available, 0, new[] { 0, 0, 3 });
        Assert.False(r.Granted);
        Assert.Contains("mora da čeka", r.Verdict);
    }

    [Fact]
    public void Request_above_remaining_need_is_rejected()
    {
        var r = Banker.Request(Allocation, Banker.Need(Max, Allocation), Available, 1, new[] { 5, 0, 0 });
        Assert.False(r.Granted);
        Assert.Contains("maksimuma", r.Verdict);
    }

    [Fact]
    public void Request_that_leads_to_an_unsafe_state_is_refused()
    {
        // Posle odobravanja (1,0,2) procesu P1, zahtev P0 za (0,2,0) vodi u nebezbedno stanje.
        var alloc = Allocation.Select(r => (int[])r.Clone()).ToArray();
        alloc[1] = new[] { 3, 0, 2 };
        var need = Banker.Need(Max, alloc);
        var r = Banker.Request(alloc, need, new[] { 2, 3, 0 }, 0, new[] { 0, 2, 0 });
        Assert.False(r.Granted);
        Assert.Contains("nebezbedno", r.Verdict);
    }
}
