namespace AITutor.Core.Chunking;

public class WholeDocumentChunkingStrategy : IChunkingStrategy
{
    public List<string> Chunk(List<string> pages) => new() { string.Join("\n", pages) };
}
