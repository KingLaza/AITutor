using System.Text.RegularExpressions;

namespace AITutor.Core.Subjects.OperatingSystems;

public record PageStep(string Page, bool Fault, string?[] Frames, string? Evicted);

public record PageResult(string Algorithm, int Faults, List<PageStep> Steps);

public static class PageReplacement
{
    // "EDHB" -> E,D,H,B; "7 0 1 2" ili "7,0,1,2" -> 7,0,1,2 (višecifreni brojevi samo uz razdvajanje).
    public static List<string> Tokenize(string reference)
    {
        var trimmed = reference.Trim();
        if (Regex.IsMatch(trimmed, @"[\s,;]")) return Regex.Split(trimmed, @"[\s,;]+").Where(s => s.Length > 0).ToList();
        return trimmed.Select(c => c.ToString()).ToList();
    }

    public static PageResult Run(string algorithm, IReadOnlyList<string> references, int frameCount)
    {
        algorithm = algorithm.ToUpperInvariant();
        if (algorithm is not ("FIFO" or "LRU" or "OPT")) throw new ArgumentException($"Nepoznat algoritam {algorithm}");
        if (frameCount <= 0) throw new ArgumentException("Broj okvira mora biti pozitivan");

        var frames = new string?[frameCount];
        var loadedAt = new int[frameCount];
        var usedAt = new int[frameCount];
        var steps = new List<PageStep>();
        var faults = 0;

        for (var t = 0; t < references.Count; t++)
        {
            var page = references[t];
            var hit = Array.IndexOf(frames, page);
            if (hit >= 0)
            {
                usedAt[hit] = t;
                steps.Add(new PageStep(page, false, (string?[])frames.Clone(), null));
                continue;
            }

            faults++;
            var slot = Array.IndexOf(frames, null);
            string? evicted = null;
            if (slot < 0)
            {
                slot = algorithm switch
                {
                    "FIFO" => Enumerable.Range(0, frameCount).OrderBy(i => loadedAt[i]).First(),
                    "LRU" => Enumerable.Range(0, frameCount).OrderBy(i => usedAt[i]).First(),
                    _ => Enumerable.Range(0, frameCount)
                            .OrderByDescending(i => NextUse(references, t + 1, frames[i]!))
                            .ThenBy(i => loadedAt[i]).First(),
                };
                evicted = frames[slot];
            }
            frames[slot] = page;
            loadedAt[slot] = t;
            usedAt[slot] = t;
            steps.Add(new PageStep(page, true, (string?[])frames.Clone(), evicted));
        }

        return new PageResult(algorithm, faults, steps);
    }

    private static int NextUse(IReadOnlyList<string> references, int from, string page)
    {
        for (var i = from; i < references.Count; i++)
            if (references[i] == page) return i;
        return int.MaxValue;
    }
}
