namespace AITutor.Core.Data;

public interface IChunkRepository
{
    Task InsertAsync(ChunkRecord chunk);

    Task<List<ChunkSearchResult>> SearchAsync(float[] queryEmbedding, int topK);
}
