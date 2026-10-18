namespace AITutor.Core.Subjects.OperatingSystems;

public record AddressFormat(int? SegmentBits, int[] PageIndexBits, int OffsetBits, int UsedBits, int? UnusedBits, int EntriesPerTable)
{
    public string Layout => string.Join(" | ",
        (SegmentBits is int s ? new[] { $"segment {s}" } : Array.Empty<string>())
        .Concat(PageIndexBits.Select((b, i) => PageIndexBits.Length == 1 ? $"stranica {b}" : $"P{i + 1} {b}"))
        .Append($"pomeraj {OffsetBits}"));
}

public static class AddressCalc
{
    // Pretpostavka kao na ispitima: svaka tabela stranica staje u jednu stranicu, pa ima pageSize/entrySize stavki
    // i koristi log2(pageSize/entrySize) bita indeksa po nivou.
    public static AddressFormat Format(int? virtualBits, long? segments, long pageSize, long entrySize, int levels)
    {
        if (levels < 1) throw new ArgumentException("Mora postojati bar jedan nivo tabele stranica");
        var offset = SizeParser.Log2(pageSize);
        var entries = pageSize / entrySize;
        var perLevel = SizeParser.Log2(entries);
        var indexBits = Enumerable.Repeat(perLevel, levels).ToArray();

        int? segBits = null;
        if (segments is long seg and > 0) segBits = (int)Math.Ceiling(Math.Log2(seg));

        var used = (segBits ?? 0) + indexBits.Sum() + offset;
        int? unused = virtualBits is int vb && segBits == null ? vb - used : null;
        return new AddressFormat(segBits, indexBits, offset, used, unused, (int)entries);
    }
}
