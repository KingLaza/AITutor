using System.Text.RegularExpressions;

namespace AITutor.Core.Chunking;

public class PerTaskChunkingStrategy : IChunkingStrategy
{
    private static readonly Regex TaskBoundary = new(@"^\s*\d+\.\s", RegexOptions.Multiline);

    public List<string> Chunk(List<string> pages)
    {
        var fullText = string.Join("\n", pages);
        var matches = TaskBoundary.Matches(fullText);

        if (matches.Count == 0)
            return new List<string> { fullText };

        var chunks = new List<string>();
        for (int i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : fullText.Length;
            chunks.Add(fullText[start..end].Trim());
        }

        return chunks;
    }
}
