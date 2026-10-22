using Npgsql;

namespace AITutor.Core.Data;

public record Profile(int Id, string Name);

// Sačuvana poruka razgovora; izvori i aktivni zadatak se čuvaju kao JSON tekst. Origin: poreklo odgovora (vidi RagAnswer.Origin).
public record StoredMessage(string Role, string Content, string? Intent, string? SourcesJson, string? ActiveTaskJson, string? Origin = null);

// Razgovor je skup poruka jednog profila za jedan predmet; naslov je početak prvog pitanja.
public record ConversationInfo(string Subject, int Id, string Title, int Messages, DateTime Updated);

public interface IProfileRepository
{
    Task<List<Profile>> ListAsync();
    Task<Profile> GetOrCreateAsync(string name);
    Task<bool> RenameAsync(int id, string name);
    Task DeleteAsync(int id);
    Task AddMessageAsync(int profileId, string subject, int conversationId, StoredMessage message);
    Task<List<StoredMessage>> GetMessagesAsync(int profileId, string subject, int conversationId, int limit = 200);
    Task<List<ConversationInfo>> ListConversationsAsync(int profileId, string? subject);
    Task DeleteConversationAsync(int profileId, string subject, int conversationId);
}

// Lokalni profil (samo ime, bez lozinke): instanca aplikacije je namenjena jednom studentu ili maloj grupi na istom računaru.
public class PgProfileRepository : IProfileRepository
{
    private const int TitleLength = 60;

    private readonly NpgsqlDataSource _dataSource;

    public PgProfileRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    // Idempotentno kreiranje i dopuna tabela; poziva se pri pokretanju API-ja, pa radi i na već postojećoj bazi.
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
            ALTER TABLE messages ADD COLUMN IF NOT EXISTS origin TEXT;
            ALTER TABLE messages ADD COLUMN IF NOT EXISTS conversation_id INT NOT NULL DEFAULT 1;
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

    // false ako ime već koristi drugi profil.
    public async Task<bool> RenameAsync(int id, string name)
    {
        await using var cmd = _dataSource.CreateCommand("UPDATE profiles SET name = $2 WHERE id = $1");
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(name);
        try
        {
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }
    }

    // Brisanjem profila brišu se i sve njegove poruke (ON DELETE CASCADE).
    public async Task DeleteAsync(int id)
    {
        await using var cmd = _dataSource.CreateCommand("DELETE FROM profiles WHERE id = $1");
        cmd.Parameters.AddWithValue(id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddMessageAsync(int profileId, string subject, int conversationId, StoredMessage m)
    {
        await using var cmd = _dataSource.CreateCommand(@"
            INSERT INTO messages (profile_id, subject, conversation_id, role, content, intent, sources, active_task, origin)
            VALUES ($1, $2, $3, $4, $5, $6, $7::jsonb, $8::jsonb, $9)");
        cmd.Parameters.AddWithValue(profileId);
        cmd.Parameters.AddWithValue(subject);
        cmd.Parameters.AddWithValue(conversationId);
        cmd.Parameters.AddWithValue(m.Role);
        cmd.Parameters.AddWithValue(m.Content);
        cmd.Parameters.AddWithValue((object?)m.Intent ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)m.SourcesJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)m.ActiveTaskJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)m.Origin ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<StoredMessage>> GetMessagesAsync(int profileId, string subject, int conversationId, int limit = 200)
    {
        await using var cmd = _dataSource.CreateCommand(@"
            SELECT role, content, intent, sources::text, active_task::text, origin FROM (
                SELECT id, role, content, intent, sources, active_task, origin FROM messages
                WHERE profile_id = $1 AND lower(subject) = lower($2) AND conversation_id = $3 ORDER BY id DESC LIMIT $4
            ) recent ORDER BY id");
        cmd.Parameters.AddWithValue(profileId);
        cmd.Parameters.AddWithValue(subject);
        cmd.Parameters.AddWithValue(conversationId);
        cmd.Parameters.AddWithValue(limit);

        var messages = new List<StoredMessage>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            messages.Add(new StoredMessage(
                reader.GetString(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        return messages;
    }

    // Najskoriji razgovor je prvi. subject == null: razgovori iz svih predmeta.
    public async Task<List<ConversationInfo>> ListConversationsAsync(int profileId, string? subject)
    {
        await using var cmd = _dataSource.CreateCommand(@"
            SELECT subject, conversation_id, count(*), max(created_at),
                   (array_agg(content ORDER BY id) FILTER (WHERE role = 'user'))[1]
            FROM messages
            WHERE profile_id = $1 AND ($2::text IS NULL OR lower(subject) = lower($2))
            GROUP BY subject, conversation_id
            ORDER BY max(created_at) DESC");
        cmd.Parameters.AddWithValue(profileId);
        cmd.Parameters.AddWithValue((object?)subject ?? DBNull.Value);

        var list = new List<ConversationInfo>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var first = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim().Replace("\n", " ");
            var title = first.Length <= TitleLength ? first : first[..TitleLength].TrimEnd() + "...";
            list.Add(new ConversationInfo(reader.GetString(0), reader.GetInt32(1), title, (int)reader.GetInt64(2), reader.GetDateTime(3)));
        }
        return list;
    }

    public async Task DeleteConversationAsync(int profileId, string subject, int conversationId)
    {
        await using var cmd = _dataSource.CreateCommand(
            "DELETE FROM messages WHERE profile_id = $1 AND lower(subject) = lower($2) AND conversation_id = $3");
        cmd.Parameters.AddWithValue(profileId);
        cmd.Parameters.AddWithValue(subject);
        cmd.Parameters.AddWithValue(conversationId);
        await cmd.ExecuteNonQueryAsync();
    }
}
