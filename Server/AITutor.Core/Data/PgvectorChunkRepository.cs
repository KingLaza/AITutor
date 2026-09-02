using Npgsql;
using Pgvector;

namespace AITutor.Core.Data;

public class PgvectorChunkRepository : IChunkRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PgvectorChunkRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task InsertAsync(ChunkRecord chunk)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO chunks (subject, source_type, source_file, content, embedding) VALUES ($1, $2, $3, $4, $5)";
        cmd.Parameters.AddWithValue(chunk.Subject);
        cmd.Parameters.AddWithValue(chunk.SourceType);
        cmd.Parameters.AddWithValue((object?)chunk.SourceFile ?? DBNull.Value);
        cmd.Parameters.AddWithValue(chunk.Content);
        cmd.Parameters.AddWithValue(new Vector(chunk.Embedding));
        await cmd.ExecuteNonQueryAsync();
    }
}
