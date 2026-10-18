namespace AITutor.Core.Data;

public interface IChunkRepository
{
    Task InsertAsync(ChunkRecord chunk);

    Task<List<string>> GetSubjectsAsync();

    Task<List<ChunkSearchResult>> SearchAsync(float[] queryEmbedding, int topK, string[]? sourceTypes = null, string? subject = null);

    // Sledeći chunk istog fajla (npr. rešenje zadatka koje je na sledećoj strani).
    Task<ChunkSearchResult?> GetNextChunkAsync(int id);

    // Vektorska pretraga + tekstualna po ključnim rečima, spojene preko Reciprocal Rank Fusion.
    Task<List<ChunkSearchResult>> SearchHybridAsync(float[] queryEmbedding, string queryText, int topK, string[]? sourceTypes = null, string? subject = null);
}
