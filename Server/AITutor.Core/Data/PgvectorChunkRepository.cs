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

    public async Task<List<string>> GetSubjectsAsync()
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT subject FROM chunks ORDER BY subject";
        var subjects = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) subjects.Add(reader.GetString(0));
        return subjects;
    }

    public async Task<List<MaterialInfo>> ListMaterialsAsync(string subject, string sourceType)
    {
        await using var cmd = _dataSource.CreateCommand(@"
            SELECT source_file, count(*)::int FROM chunks
            WHERE lower(subject) = lower($1) AND source_type = $2
            GROUP BY source_file ORDER BY min(id)");
        cmd.Parameters.AddWithValue(subject);
        cmd.Parameters.AddWithValue(sourceType);
        var list = new List<MaterialInfo>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) list.Add(new MaterialInfo(reader.IsDBNull(0) ? "" : reader.GetString(0), reader.GetInt32(1)));
        return list;
    }

    public async Task<int> DeleteMaterialAsync(string subject, string sourceType, string sourceFile)
    {
        await using var cmd = _dataSource.CreateCommand(
            "DELETE FROM chunks WHERE lower(subject) = lower($1) AND source_type = $2 AND source_file = $3");
        cmd.Parameters.AddWithValue(subject);
        cmd.Parameters.AddWithValue(sourceType);
        cmd.Parameters.AddWithValue(sourceFile);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<ChunkSearchResult?> GetNextChunkAsync(int id)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT n.content, n.source_file, n.source_type, n.id
            FROM chunks c
            JOIN chunks n ON n.source_file = c.source_file AND n.source_type = c.source_type
                         AND n.id = (SELECT min(id) FROM chunks
                                     WHERE source_file = c.source_file AND source_type = c.source_type AND id > c.id)
            WHERE c.id = $1";
        cmd.Parameters.AddWithValue(id);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new ChunkSearchResult
        {
            Content = reader.GetString(0),
            SourceFile = reader.IsDBNull(1) ? "" : reader.GetString(1),
            SourceType = reader.GetString(2),
            Id = reader.GetInt32(3),
            IsNeighbor = true
        };
    }

    private static readonly HashSet<string> StopWords = new() { "datu", "dati", "koji", "koja", "koje", "pomocu", "napisati", "definisati", "odrediti" };

    // Jeftino "stemovanje" skraćivanjem na 5 slova (srpska morfologija), bez dijakritika; prefiksno poređenje u tsquery-ju.
    internal static string BuildTsQuery(string text)
    {
        var normalized = text.ToLowerInvariant().Replace("dj", "d")
            .Replace('č', 'c').Replace('ć', 'c').Replace('ž', 'z').Replace('š', 's').Replace('đ', 'd');
        var tokens = System.Text.RegularExpressions.Regex.Matches(normalized, @"[a-z0-9]+")
            .Select(m => m.Value)
            .Where(t => t.Length >= 4 && !StopWords.Contains(t))
            .Select(t => t[..Math.Min(5, t.Length)] + ":*")
            .Distinct();
        return string.Join(" | ", tokens);
    }

    public async Task<List<ChunkSearchResult>> SearchHybridAsync(float[] queryEmbedding, string queryText, int topK, string[]? sourceTypes = null, string? subject = null)
    {
        var tsQuery = BuildTsQuery(queryText);
        if (tsQuery.Length == 0) return await SearchAsync(queryEmbedding, topK, sourceTypes, subject);

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            WITH base AS (
                SELECT id, content, source_file, source_type, embedding <=> $1 AS dist,
                       ts_rank(to_tsvector('simple', translate(lower(coalesce(source_file, '') || ' ' || content), 'čćžšđ', 'cczsd')),
                               to_tsquery('simple', $2)) AS fts
                FROM chunks
                WHERE (cardinality($4::text[]) = 0 OR source_type = ANY($4)) AND ($5::text IS NULL OR lower(subject) = lower($5))
            ),
            d AS (SELECT id, row_number() OVER (ORDER BY dist) AS rd FROM base),
            f AS (SELECT id, row_number() OVER (ORDER BY fts DESC) AS rf FROM base WHERE fts > 0)
            SELECT b.content, b.source_file, b.dist, b.source_type, b.id
            FROM base b
            JOIN d ON d.id = b.id
            LEFT JOIN f ON f.id = b.id
            ORDER BY 1.0 / (60 + d.rd) + COALESCE(1.0 / (60 + f.rf), 0) DESC
            LIMIT $3";
        cmd.Parameters.AddWithValue(new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue(tsQuery);
        cmd.Parameters.AddWithValue(topK);
        cmd.Parameters.AddWithValue(sourceTypes ?? Array.Empty<string>());
        cmd.Parameters.AddWithValue((object?)subject ?? DBNull.Value);

        var results = new List<ChunkSearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ChunkSearchResult
            {
                Content = reader.GetString(0),
                SourceFile = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Distance = reader.GetDouble(2),
                SourceType = reader.GetString(3),
                Id = reader.GetInt32(4)
            });
        }
        return results;
    }

    public async Task<List<ChunkSearchResult>> SearchAsync(float[] queryEmbedding, int topK, string[]? sourceTypes = null, string? subject = null)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT content, source_file, embedding <=> $1 AS distance, source_type, id
            FROM chunks
            WHERE (cardinality($3::text[]) = 0 OR source_type = ANY($3)) AND ($4::text IS NULL OR lower(subject) = lower($4))
            ORDER BY embedding <=> $1
            LIMIT $2";
        cmd.Parameters.AddWithValue(new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue(topK);
        cmd.Parameters.AddWithValue(sourceTypes ?? Array.Empty<string>());
        cmd.Parameters.AddWithValue((object?)subject ?? DBNull.Value);

        var results = new List<ChunkSearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ChunkSearchResult
            {
                Content = reader.GetString(0),
                SourceFile = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Distance = reader.GetDouble(2),
                SourceType = reader.GetString(3),
                Id = reader.GetInt32(4)
            });
        }
        return results;
    }
}
