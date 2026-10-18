using System.Globalization;
using System.Text.RegularExpressions;

namespace AITutor.Core.Subjects.OperatingSystems;

public static class SizeParser
{
    // "4KB" -> 4096, "1048B+516MB" -> 1048 + 516*2^20, "2 KB" -> 2048. Jedinice su stepeni dvojke.
    public static long? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        long total = 0;
        var any = false;
        foreach (Match m in Regex.Matches(text.ToUpperInvariant().Replace(" ", ""), @"(\d+(?:[.,]\d+)?)(KB|MB|GB|TB|B|K|M|G)?"))
        {
            var number = double.Parse(m.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
            long multiplier = m.Groups[2].Value switch
            {
                "KB" or "K" => 1L << 10,
                "MB" or "M" => 1L << 20,
                "GB" or "G" => 1L << 30,
                "TB" => 1L << 40,
                _ => 1L,
            };
            total += (long)Math.Round(number * multiplier);
            any = true;
        }
        return any ? total : null;
    }

    public static string Format(long bytes) => bytes switch
    {
        >= 1L << 40 when bytes % (1L << 40) == 0 => $"{bytes >> 40} TB",
        >= 1L << 30 when bytes % (1L << 30) == 0 => $"{bytes >> 30} GB",
        >= 1L << 20 when bytes % (1L << 20) == 0 => $"{bytes >> 20} MB",
        >= 1L << 10 when bytes % (1L << 10) == 0 => $"{bytes >> 10} KB",
        _ => $"{bytes} B",
    };

    public static int Log2(long value)
    {
        if (value <= 0 || (value & (value - 1)) != 0) throw new ArgumentException($"{value} nije stepen dvojke");
        return System.Numerics.BitOperations.Log2((ulong)value);
    }
}
