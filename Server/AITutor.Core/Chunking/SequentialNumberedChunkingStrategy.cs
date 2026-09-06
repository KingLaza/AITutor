using System.Text.RegularExpressions;

namespace AITutor.Core.Chunking;

public class SequentialNumberedChunkingStrategy : IChunkingStrategy
{
    private static readonly Regex CandidateBoundary = new(@"^(\d{1,3})\.\s", RegexOptions.Multiline);

    public List<string> Chunk(List<string> pages)
    {
        var fullText = string.Join("\n", pages);
        var matches = CandidateBoundary.Matches(fullText);

        var boundaries = new List<int>();
        int nextExpected = 1;

        foreach (Match m in matches)
        {
            if (int.TryParse(m.Groups[1].Value, out var num) && num == nextExpected)
            {
                boundaries.Add(m.Index);
                nextExpected++;
            }
        }

        if (boundaries.Count == 0)
            return new List<string> { fullText };

        var chunks = new List<string>();
        for (int i = 0; i < boundaries.Count; i++)
        {
            var start = boundaries[i];
            var end = i + 1 < boundaries.Count ? boundaries[i + 1] : fullText.Length;
            chunks.Add(fullText[start..end].Trim());
        }

        return chunks;
    }
}
