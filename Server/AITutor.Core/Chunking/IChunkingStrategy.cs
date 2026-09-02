namespace AITutor.Core.Chunking;

public interface IChunkingStrategy
{
    List<string> Chunk(List<string> pages);
}
