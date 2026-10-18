using Npgsql;

namespace AITutor.Core.Data;

public record Profile(int Id, string Name);

// Sačuvana poruka razgovora; izvori i aktivni zadatak se čuvaju kao JSON tekst.
public record StoredMessage(string Role, string Content, string? Intent, string? SourcesJson, string? ActiveTaskJson);

public interface IProfileRepository
{
    Task<List<Profile>> ListAsync();
    Task<Profile> GetOrCreateAsync(string name);
    Task AddMessageAsync(int profileId, string subject, StoredMessage message);
    Task<List<StoredMessage>> GetMessagesAsync(int profileId, string subject, int limit = 200);
    Task ClearMessagesAsync(int profileId, string subject);
}

// Lokalni profil (samo ime, bez lozinke): instanca aplikacije je namenjena jednom studentu ili maloj grupi na istom računaru.
public class PgProfileRepository : IProfileRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PgProfileRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    // Idempotentno kreiranje tabela; poziva se pri pokretanju API-ja, pa radi i na već postojećoj bazi.
    public static async Task EnsureSchemaAsync(NpgsqlDataSource dataSource)
    {
        await using var cmd = dataSource.CreateCommand(@"
            CREATE TABLE IF NOT EXISTS profiles (
                id         SERIAL PRIMARY KEY,
                name       TEXT NOT NULL UNIQUE,
                created_at TIMESTAMPTZ DEFAULT now()
            );
            CREATE TABLE IF NOT EXISTS messages (
                id          SERIAL PRIMARY KEY,
                profile_id  INT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
                subject     TEXT NOT NULL,
                role        TEXT NOT NULL,
                content     TEXT NOT NULL,
                intent      TEXT,
                sources     JSONB,
                active_task JSONB,
                created_at  TIMESTAMPTZ DEFAULT now()
            );
            CREATE INDEX IF NOT EXISTS messages_profile_subject_idx ON messages (profile_id, subject, id);");
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Profile>> ListAsync()
    {
        await using var cmd = _dataSource.CreateCommand("SELECT id, name FROM profiles ORDER BY name");
        var profiles = new List<Profile>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) profiles.Add(new Profile(reader.GetInt32(0), reader.GetString(1)));
        return profiles;
    }

    public async Task<Profile> GetOrCreateAsync(string name)
    {
        await using var cmd = _dataSource.CreateCommand(@"
            INSERT INTO profiles (name) VALUES ($1)
            ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name
            RETURNING id, name");
        cmd.Parameters.AddWithValue(name);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new Profile(reader.GetInt32(0), reader.GetString(1));
    }

    public async Task AddMessageAsync(int profileId, string subject, StoredMessage m)
    {
        await using var cmd = _dataSource.CreateCommand(@"
            INSERT INTO messages (profile_id, subject, role, content, intent, sources, active_task)
            VALUES ($1, $2, $3, $4, $5, $6::jsonb, $7::jsonb)");
        cmd.Parameters.AddWithValue(profileId);
        cmd.Parameters.AddWithValue(subject);
        cmd.Parameters.AddWithValue(m.Role);
        cmd.Parameters.AddWithValue(m.Content);
        cmd.Parameters.AddWithValue((object?)m.Intent ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)m.SourcesJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)m.ActiveTaskJson ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<StoredMessage>> GetMessagesAsync(int profileId, string subject, int limit = 200)
    {
        await using var cmd = _dataSource.CreateCommand(@"
            SELECT role, content, intent, sources::text, active_task::text FROM (
                SELECT id, role, content, intent, sources, active_task FROM messages
                WHERE profile_id = $1 AND lower(subject) = lower($2) ORDER BY id DESC LIMIT $3
            ) recent ORDER BY id");
        cmd.Parameters.AddWithValue(profileId);
        cmd.Parameters.AddWithValue(subject);
        cmd.Parameters.AddWithValue(limit);

        var messages = new List<StoredMessage>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            messages.Add(new StoredMessage(
                reader.GetString(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        return messages;
    }

    public async Task ClearMessagesAsync(int profileId, string subject)
    {
        await using var cmd = _dataSource.CreateCommand("DELETE FROM messages WHERE profile_id = $1 AND lower(subject) = lower($2)");
        cmd.Parameters.AddWithValue(profileId);
        cmd.Parameters.AddWithValue(subject);
        await cmd.ExecuteNonQueryAsync();
    }
}
