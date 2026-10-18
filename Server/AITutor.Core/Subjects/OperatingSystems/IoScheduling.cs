using System.Text.RegularExpressions;

namespace AITutor.Core.Subjects.OperatingSystems;

// Proces sa naizmeničnim CPU i U/I fazama: "4-(5)-3" = CPU 4, U/I 5, CPU 3 (faze su redom CPU, U/I, CPU, ...).
public record IoProc(string Name, int Arrival, int[] Phases);

public record IoScheduleResult(string Policy, List<Segment> CpuTimeline, Dictionary<string, int> Completion, int Makespan, int CpuBusy)
{
    public double Utilization => Makespan == 0 ? 0 : 100.0 * CpuBusy / Makespan;
}

public static class IoScheduling
{
    public static readonly string[] Supported = { "FCFS", "SRTF", "NO_MULTIPROGRAMMING" };

    private static readonly Regex PhasesPattern = new(@"\d+(?:\s*-\s*\(\s*\d+\s*\)\s*-\s*\d+)+");

    // Svi nizovi faza zapisani u tekstu zadatka, normalizovani ("4-(5)-3").
    public static List<string> PhasesInText(string task) =>
        PhasesPattern.Matches(task).Select(m => Regex.Replace(m.Value, @"\s+", "")).ToList();

    public static int[] ParsePhases(string notation) =>
        Regex.Matches(notation, @"\d+").Select(m => int.Parse(m.Value)).ToArray();

    // U/I uređaji rade paralelno (nema čekanja na U/I); jedan procesor. Simulacija po milisekundama.
    // FCFS: red spremnih po redosledu postajanja spremnim, bez prekidanja CPU faze.
    // SRTF: bira se proces sa najmanje preostalog vremena tekuće CPU faze; prekida se pri svakom novom spremnom procesu.
    // NO_MULTIPROGRAMMING: proces se izvršava do kraja (uključujući U/I, procesor miruje), tek onda sledeći.
    public static IoScheduleResult Simulate(string policy, IReadOnlyList<IoProc> procs)
    {
        policy = policy.ToUpperInvariant();
        if (!Supported.Contains(policy)) throw new ArgumentException($"Nepoznata politika {policy}");
        if (procs.Any(p => p.Phases.Length % 2 == 0 || p.Phases.Any(x => x <= 0))) throw new ArgumentException("Neispravne faze");
        return policy == "NO_MULTIPROGRAMMING" ? Sequential(procs) : Multiprogrammed(policy, procs);
    }

    private static IoScheduleResult Sequential(IReadOnlyList<IoProc> procs)
    {
        var timeline = new List<Segment>();
        var completion = new Dictionary<string, int>();
        var time = 0;
        foreach (var p in procs.OrderBy(p => p.Arrival))
        {
            time = Math.Max(time, p.Arrival);
            for (var i = 0; i < p.Phases.Length; i++)
            {
                if (i % 2 == 0) AddSegment(timeline, time, time + p.Phases[i], p.Name);
                time += p.Phases[i];
            }
            completion[p.Name] = time;
        }
        return new IoScheduleResult("NO_MULTIPROGRAMMING", timeline, completion, time, timeline.Sum(s => s.End - s.Start));
    }

    private static IoScheduleResult Multiprogrammed(string policy, IReadOnlyList<IoProc> procs)
    {
        var phase = procs.ToDictionary(p => p.Name, _ => 0);
        var left = procs.ToDictionary(p => p.Name, p => p.Phases[0]);
        var readyAt = procs.ToDictionary(p => p.Name, p => p.Arrival); // kada postaje spremno (ili kada se završava U/I)
        var done = new HashSet<string>();
        var order = procs.Select((p, i) => (p.Name, i)).ToDictionary(x => x.Name, x => x.i);
        var completion = new Dictionary<string, int>();
        var timeline = new List<Segment>();
        string? running = null;
        var time = 0;

        while (done.Count < procs.Count)
        {
            if (time > 100_000) throw new InvalidOperationException("Simulacija predugo traje");

            // Spremni: u CPU fazi (parna faza), vreme spremnosti <= trenutno vreme, nisu završeni.
            var ready = procs.Where(p => !done.Contains(p.Name) && phase[p.Name] % 2 == 0 && readyAt[p.Name] <= time).Select(p => p.Name).ToList();
            if (policy == "SRTF" && ready.Count > 0)
            {
                // Prekidanje: bira se najmanje preostalo; pri izjednačenju ostaje tekući, pa raniji u redu spremnih.
                var best = ready.OrderBy(n => left[n]).ThenBy(n => n == running ? 0 : 1).ThenBy(n => readyAt[n]).ThenBy(n => order[n]).First();
                running = best;
            }
            else if (running == null || !ready.Contains(running))
            {
                running = ready.Count == 0 ? null : ready.OrderBy(n => readyAt[n]).ThenBy(n => order[n]).First();
            }

            if (running != null)
            {
                AddSegment(timeline, time, time + 1, running);
                left[running]--;
            }
            time++;

            if (running != null && left[running] == 0)
            {
                var name = running;
                running = null;
                var p = procs.First(x => x.Name == name);
                phase[name]++;
                if (phase[name] >= p.Phases.Length)
                {
                    done.Add(name);
                    completion[name] = time;
                }
                else
                {
                    left[name] = p.Phases[phase[name]];   // U/I faza
                    readyAt[name] = time + left[name];    // posle U/I postaje spreman za sledeću CPU fazu
                    phase[name]++;
                    left[name] = p.Phases[phase[name]];
                }
            }
        }

        var makespan = completion.Values.Max();
        return new IoScheduleResult(policy, timeline, completion, makespan, timeline.Sum(s => s.End - s.Start));
    }

    private static void AddSegment(List<Segment> timeline, int start, int end, string name)
    {
        if (timeline.Count > 0 && timeline[^1].Name == name && timeline[^1].End == start)
            timeline[^1] = timeline[^1] with { End = end };
        else
            timeline.Add(new Segment(start, end, name));
    }
}
