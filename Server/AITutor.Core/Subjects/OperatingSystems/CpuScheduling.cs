namespace AITutor.Core.Subjects.OperatingSystems;

public record Proc(string Name, int Burst, int Arrival, int? Priority);

public record Segment(int Start, int End, string Name);

public record ScheduleResult(string Algorithm, List<Segment> Timeline, Dictionary<string, int> Completion);

public static class CpuScheduling
{
    public static readonly string[] Supported = { "FCFS", "SJF", "SRTF", "PRIORITY", "PRIORITY_PREEMPTIVE", "RR" };

    // Simulacija po milisekundama. Prioritet: manji broj = veći prioritet.
    // RR: novopristigli procesi ulaze u red pre procesa kome je istekao kvant.
    public static ScheduleResult Simulate(string algorithm, IReadOnlyList<Proc> procs, int quantum = 0)
    {
        algorithm = algorithm.ToUpperInvariant();
        if (!Supported.Contains(algorithm)) throw new ArgumentException($"Nepoznat algoritam {algorithm}");
        if (algorithm == "RR" && quantum <= 0) throw new ArgumentException("RR traži vremenski kvant");
        if (algorithm.StartsWith("PRIORITY") && procs.Any(p => p.Priority == null)) throw new ArgumentException("Nedostaju prioriteti");

        var index = procs.Select((p, i) => (p.Name, i)).ToDictionary(x => x.Name, x => x.i);
        var remaining = procs.ToDictionary(p => p.Name, p => p.Burst);
        var queue = new List<Proc>();
        var completion = new Dictionary<string, int>();
        var ticks = new List<string>();
        Proc? current = null;
        var quantumLeft = 0;
        var time = 0;

        while (completion.Count < procs.Count)
        {
            if (time > 100_000) throw new InvalidOperationException("Simulacija predugo traje");

            if (algorithm == "RR")
            {
                foreach (var p in procs.Where(p => p.Arrival == time)) queue.Add(p);
                if (current != null && quantumLeft == 0 && remaining[current.Name] > 0)
                {
                    queue.Add(current);
                    current = null;
                }
                if (current == null && queue.Count > 0)
                {
                    current = queue[0];
                    queue.RemoveAt(0);
                    quantumLeft = quantum;
                }
            }
            else
            {
                var ready = procs.Where(p => p.Arrival <= time && remaining[p.Name] > 0).ToList();
                var preemptive = algorithm is "SRTF" or "PRIORITY_PREEMPTIVE";
                if (ready.Count > 0 && (preemptive || current == null))
                {
                    var running = current?.Name;
                    current = algorithm switch
                    {
                        "FCFS" => ready.OrderBy(p => p.Arrival).ThenBy(p => index[p.Name]).First(),
                        "SJF" => ready.OrderBy(p => p.Burst).ThenBy(p => p.Arrival).ThenBy(p => index[p.Name]).First(),
                        "SRTF" => ready.OrderBy(p => remaining[p.Name]).ThenBy(p => p.Name == running ? 0 : 1).ThenBy(p => p.Arrival).ThenBy(p => index[p.Name]).First(),
                        _ => ready.OrderBy(p => p.Priority!.Value).ThenBy(p => p.Name == running ? 0 : 1).ThenBy(p => p.Arrival).ThenBy(p => index[p.Name]).First(),
                    };
                }
            }

            if (current == null)
            {
                ticks.Add("idle");
                time++;
                continue;
            }

            ticks.Add(current.Name);
            remaining[current.Name]--;
            if (algorithm == "RR") quantumLeft--;
            time++;
            if (remaining[current.Name] == 0)
            {
                completion[current.Name] = time;
                current = null;
                quantumLeft = 0;
            }
        }

        return new ScheduleResult(algorithm, ToSegments(ticks), completion);
    }

    private static List<Segment> ToSegments(List<string> ticks)
    {
        var segments = new List<Segment>();
        for (var t = 0; t < ticks.Count; t++)
        {
            if (segments.Count > 0 && segments[^1].Name == ticks[t] && segments[^1].End == t)
                segments[^1] = segments[^1] with { End = t + 1 };
            else
                segments.Add(new Segment(t, t + 1, ticks[t]));
        }
        return segments;
    }

    // Procesi koji se izvršavaju bar delom u intervalu [from, to).
    public static List<string> WhoRuns(ScheduleResult result, int from, int to) =>
        result.Timeline.Where(s => s.Name != "idle" && s.Start < to && s.End > from).Select(s => s.Name).Distinct().ToList();
}
