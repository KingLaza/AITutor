namespace AITutor.Core.Subjects.OperatingSystems;

public record InodeResult(long FileBytes, long DataBlocks, long IndexBlocks, int AccessesToLastBlock, int LastBlockLevel);

public static class FileSystemCalc
{
    // Unix i-node: 'direct' direktnih adresa + do 3 nivoa indirekcije (jednostruka, dvostruka, trostruka).
    // Broj indeksnih blokova na dubini k za nivo L: ceil(t / perBlock^(L-k+1)).
    public static InodeResult Inode(long fileBytes, long blockSize, int direct, long addressSize, int indirectLevels)
    {
        if (blockSize <= 0 || addressSize <= 0 || blockSize % addressSize != 0) throw new ArgumentException("Neispravne veličine bloka/adrese");
        if (indirectLevels is < 0 or > 3) throw new ArgumentException("Nivoi indirekcije moraju biti 0-3");

        var perBlock = blockSize / addressSize;
        var dataBlocks = (fileBytes + blockSize - 1) / blockSize;
        var remaining = dataBlocks;

        remaining -= Math.Min(remaining, direct);
        long indexBlocks = 0;
        var lastLevel = 0;

        for (var level = 1; level <= indirectLevels && remaining > 0; level++)
        {
            var capacity = Pow(perBlock, level);
            var t = Math.Min(remaining, capacity);
            for (var depth = 1; depth <= level; depth++)
                indexBlocks += CeilDiv(t, Pow(perBlock, level - depth + 1));
            remaining -= t;
            lastLevel = level;
        }

        if (remaining > 0) throw new ArgumentException("Fajl je veći od maksimalne veličine koju i-node može da adresira");

        // Pristupi disku do poslednjeg bloka kada je fajl otvoren (i-node u memoriji): indeksni blokovi + blok sa podacima.
        return new InodeResult(fileBytes, dataBlocks, indexBlocks, lastLevel + 1, lastLevel);
    }

    private static long Pow(long b, int e)
    {
        long r = 1;
        for (var i = 0; i < e; i++) r *= b;
        return r;
    }

    private static long CeilDiv(long a, long b) => (a + b - 1) / b;
}
