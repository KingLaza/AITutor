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

    public async Task<List<ChunkSearchResult>> SearchAsync(float[] queryEmbedding, int topK)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT content, source_file, embedding <=> $1 AS distance
            FROM chunks
            ORDER BY embedding <=> $1
            LIMIT $2";
        cmd.Parameters.AddWithValue(new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue(topK);

        var results = new List<ChunkSearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ChunkSearchResult
            {
                Content = reader.GetString(0),
                SourceFile = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Distance = reader.GetDouble(2)
            });
        }
        return results;
    }
}
