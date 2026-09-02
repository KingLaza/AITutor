namespace AITutor.Core.Data;

public interface IChunkRepository
{
    Task InsertAsync(ChunkRecord chunk);
}
