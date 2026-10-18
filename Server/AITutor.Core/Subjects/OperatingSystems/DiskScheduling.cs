namespace AITutor.Core.Subjects.OperatingSystems;

public record DiskPlan(int Head, string Direction, List<int> Remaining);

public static class DiskScheduling
{
    // Preostali zahtevi = svi zahtevi bez već usluženih; glava je na poslednje usluženom cilindru;
    // smer se izvodi iz poslednja dva usluzena zahteva (opadajuće = ka nižim cilindrima).
    public static DiskPlan? Prepare(IReadOnlyList<int> requests, IReadOnlyList<int> served, int? head, string? direction)
    {
        var remaining = requests.ToList();
        foreach (var s in served) remaining.Remove(s);

        int? h = head ?? (served.Count > 0 ? served[^1] : null);
        if (h == null) return null;

        var dir = direction?.ToLowerInvariant() switch
        {
            "up" or "gore" => "up",
            "down" or "dole" => "down",
            _ => served.Count >= 2 && served[^1] < served[^2] ? "down" : "up",
        };
        return new DiskPlan(h.Value, dir, remaining);
    }

    // Redosled usluživanja. LOOK ima isti redosled kao SCAN, a C-LOOK kao C-SCAN (razlikuju se samo po pomeranju glave).
    public static List<int> Order(string algorithm, DiskPlan plan)
    {
        var rem = plan.Remaining;
        var head = plan.Head;
        var down = plan.Direction == "down";

        switch (algorithm.ToUpperInvariant().Replace("-", "").Replace("_", ""))
        {
            case "FCFS":
                return rem.ToList();

            case "SSTF":
            {
                var left = rem.ToList();
                var order = new List<int>();
                var pos = head;
                while (left.Count > 0)
                {
                    var next = left.OrderBy(x => Math.Abs(x - pos)).ThenBy(x => x).First();
                    order.Add(next);
                    left.Remove(next);
                    pos = next;
                }
                return order;
            }

            case "SCAN" or "LOOK":
                return down
                    ? rem.Where(x => x <= head).OrderByDescending(x => x).Concat(rem.Where(x => x > head).OrderBy(x => x)).ToList()
                    : rem.Where(x => x >= head).OrderBy(x => x).Concat(rem.Where(x => x < head).OrderByDescending(x => x)).ToList();

            case "CSCAN" or "CLOOK":
                return down
                    ? rem.Where(x => x <= head).OrderByDescending(x => x).Concat(rem.Where(x => x > head).OrderByDescending(x => x)).ToList()
                    : rem.Where(x => x >= head).OrderBy(x => x).Concat(rem.Where(x => x < head).OrderBy(x => x)).ToList();

            default:
                throw new ArgumentException($"Nepoznat algoritam {algorithm}");
        }
    }

    public static string NormalizeName(string algorithm) => algorithm.ToUpperInvariant().Replace("_", "-") switch
    {
        "CSCAN" => "C-SCAN",
        "CLOOK" => "C-LOOK",
        var other => other,
    };
}
