namespace AITutor.Core.Chunking;

// Deli tekst na odeljke koji počinju linijom "### Naslov"; tekst pre prvog naslova se odbacuje (uvodna napomena).
public class SectionChunkingStrategy : IChunkingStrategy
{
    public List<string> Chunk(List<string> pages)
    {
        var chunks = new List<string>();
        List<string>? current = null;

        foreach (var line in string.Join("\n", pages).Split('\n'))
        {
            if (line.StartsWith("### "))
            {
                if (current != null) chunks.Add(string.Join("\n", current).Trim());
                current = new List<string> { line[4..].Trim() };
            }
            else current?.Add(line.TrimEnd('\r'));
        }
        if (current != null) chunks.Add(string.Join("\n", current).Trim());

        return chunks.Count > 0 ? chunks : new List<string> { string.Join("\n", pages) };
    }
}
