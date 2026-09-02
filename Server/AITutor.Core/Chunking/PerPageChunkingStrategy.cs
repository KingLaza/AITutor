namespace AITutor.Core.Chunking;

public class PerPageChunkingStrategy : IChunkingStrategy
{
    private const int MinChunkLength = 40;

    public List<string> Chunk(List<string> pages)
    {
        var chunks = new List<string>();
        var buffer = "";

        foreach (var page in pages)
        {
            var text = page.Trim();
            buffer = string.IsNullOrEmpty(buffer) ? text : buffer + "\n\n" + text;

            if (buffer.Length >= MinChunkLength)
            {
                chunks.Add(buffer);
                buffer = "";
            }
        }

        if (!string.IsNullOrWhiteSpace(buffer))
            chunks.Add(buffer);

        return chunks;
    }
}
