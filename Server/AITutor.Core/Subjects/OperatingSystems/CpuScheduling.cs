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

    // Planiranje u više redova sa povratnom spregom (predavanje 6): novi proces ulazi u RQ0, posle isteka kvanta spušta se u
    // sledeći red; kvant reda i je baseQuantum * 2^i (exponential) ili stalan baseQuantum. Redovi su FCFS, a poslednji red
    // je ciklični (proces koji istekne ostaje u njemu). Proces dobija ceo kvant (nema prekidanja usred kvanta);
    // procesi koji stignu u istom trenutku kad se bira sledeći, ulaze u RQ0 pre izbora. Broj redova nije ograničen.
    public static ScheduleResult SimulateFeedback(IReadOnlyList<Proc> procs, int baseQuantum = 1, bool exponential = true)
    {
        if (baseQuantum <= 0) throw new ArgumentException("Kvant mora biti pozitivan");
        var remaining = procs.ToDictionary(p => p.Name, p => p.Burst);
        var queues = new List<List<Proc>> { new() };
        var arrived = new HashSet<string>();
        var completion = new Dictionary<string, int>();
        var ticks = new List<string>();
        var time = 0;

        void Admit()
        {
            foreach (var p in procs.Where(p => p.Arrival <= time && arrived.Add(p.Name))) queues[0].Add(p);
        }

        while (completion.Count < procs.Count)
        {
            if (time > 100_000) throw new InvalidOperationException("Simulacija predugo traje");
            Admit();
            var level = queues.FindIndex(q => q.Count > 0);
            if (level < 0)
            {
                ticks.Add("idle");
                time++;
                continue;
            }

            var proc = queues[level][0];
            queues[level].RemoveAt(0);
            var quantum = exponential ? baseQuantum << level : baseQuantum;
            var run = Math.Min(quantum, remaining[proc.Name]);
            for (var i = 0; i < run; i++)
            {
                ticks.Add(proc.Name);
                time++;
                remaining[proc.Name]--;
                // Pristigli u toku kvanta čekaju u RQ0; redosled u redu sledi vreme pristizanja.
                Admit();
            }

            if (remaining[proc.Name] == 0)
            {
                completion[proc.Name] = time;
                continue;
            }
            var next = level + 1;
            while (queues.Count <= next) queues.Add(new List<Proc>());
            queues[next].Add(proc);
        }

        return new ScheduleResult("MLFQ", ToSegments(ticks), completion);
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
