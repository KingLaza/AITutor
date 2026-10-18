namespace AITutor.Core.Subjects.OperatingSystems;

public record SafetyResult(bool Safe, List<int> Sequence);

public record RequestResult(string Verdict, bool Granted, SafetyResult? SafetyAfter);

public static class Banker
{
    public static int[][] Need(int[][] max, int[][] allocation) =>
        max.Select((row, i) => row.Select((m, j) => m - allocation[i][j]).ToArray()).ToArray();

    // Algoritam bezbednosti: prolazi kroz procese redom i uzima svaki čije potrebe staju u raspoložive resurse,
    // pa nastavlja napred (bez vraćanja na početak) kao u udžbeniku; ponavlja prolaze dok ima napretka.
    public static SafetyResult IsSafe(int[][] allocation, int[][] need, int[] available)
    {
        var work = (int[])available.Clone();
        var finished = new bool[allocation.Length];
        var sequence = new List<int>();

        bool progress;
        do
        {
            progress = false;
            for (var i = 0; i < allocation.Length; i++)
            {
                if (finished[i] || !need[i].Zip(work, (n, w) => n <= w).All(ok => ok)) continue;
                for (var j = 0; j < work.Length; j++) work[j] += allocation[i][j];
                finished[i] = true;
                sequence.Add(i);
                progress = true;
            }
        } while (progress);

        return new SafetyResult(finished.All(f => f), sequence);
    }

    // Zahtev procesa: 1) ne sme da prelazi njegove preostale potrebe, 2) mora da stane u raspoložive resurse,
    // 3) probno se dodeli i dodeljuje se samo ako sistem ostaje u bezbednom stanju.
    public static RequestResult Request(int[][] allocation, int[][] need, int[] available, int process, int[] request)
    {
        if (request.Zip(need[process], (r, n) => r > n).Any(x => x))
            return new RequestResult("Zahtev se odbija: proces traži više nego što mu je preostalo do maksimuma (greška).", false, null);
        if (request.Zip(available, (r, a) => r > a).Any(x => x))
            return new RequestResult("Proces mora da čeka: zahtev prelazi raspoložive resurse.", false, null);

        var alloc2 = allocation.Select(r => (int[])r.Clone()).ToArray();
        var need2 = need.Select(r => (int[])r.Clone()).ToArray();
        var avail2 = (int[])available.Clone();
        for (var j = 0; j < request.Length; j++)
        {
            avail2[j] -= request[j];
            alloc2[process][j] += request[j];
            need2[process][j] -= request[j];
        }
        var safety = IsSafe(alloc2, need2, avail2);
        return safety.Safe
            ? new RequestResult("Zahtev se može odobriti: sistem ostaje u bezbednom stanju.", true, safety)
            : new RequestResult("Zahtev se ne odobrava: sistem bi prešao u nebezbedno stanje.", false, safety);
    }
}
