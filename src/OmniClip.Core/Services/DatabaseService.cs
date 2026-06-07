using Microsoft.Data.Sqlite;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;

namespace OmniClip.Core.Services;

public class DatabaseService : IDatabaseService
{
    private SqliteConnection? _connection;
    private string _dbPath = string.Empty;

    public async Task InitializeAsync(string dbPath)
    {
        _dbPath = dbPath;
        var dir = Path.GetDirectoryName(dbPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        await _connection.OpenAsync();

        await CreateTablesAsync();
    }

    private async Task CreateTablesAsync()
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS clipboard_entries (
                id              TEXT PRIMARY KEY,
                content_type    TEXT NOT NULL,
                plain_text      TEXT,
                rich_text       TEXT,
                file_path       TEXT,
                file_name       TEXT,
                source_app      TEXT,
                source_window   TEXT,
                content_hash    TEXT NOT NULL,
                char_count      INTEGER DEFAULT 0,
                tags            TEXT DEFAULT '[]',
                is_favorite     INTEGER DEFAULT 0,
                is_pinned       INTEGER DEFAULT 0,
                is_sensitive    INTEGER DEFAULT 0,
                in_knowledge_base INTEGER DEFAULT 0,
                importance_score REAL DEFAULT 0,
                created_at      TEXT NOT NULL,
                last_accessed   TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_entries_created ON clipboard_entries(created_at DESC);
            CREATE INDEX IF NOT EXISTS idx_entries_type ON clipboard_entries(content_type);

            CREATE TABLE IF NOT EXISTS config (
                key     TEXT PRIMARY KEY,
                value   TEXT
            );
        ";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<string> InsertEntryAsync(ClipboardEntry entry)
    {
        if (entry.CharCount == 0 && !string.IsNullOrEmpty(entry.PlainText))
            entry.CharCount = entry.PlainText.Length;

        var sql = @"
            INSERT INTO clipboard_entries
                (id, content_type, plain_text, rich_text, file_path, file_name,
                 source_app, source_window, content_hash, char_count, tags,
                 is_favorite, is_pinned, is_sensitive, in_knowledge_base,
                 importance_score, created_at, last_accessed)
            VALUES
                (@id, @content_type, @plain_text, @rich_text, @file_path, @file_name,
                 @source_app, @source_window, @content_hash, @char_count, @tags,
                 @is_favorite, @is_pinned, @is_sensitive, @in_knowledge_base,
                 @importance_score, @created_at, @last_accessed)";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        AddEntryParameters(cmd, entry);

        await cmd.ExecuteNonQueryAsync();
        return entry.Id;
    }

    public async Task<ClipboardEntry?> GetEntryAsync(string id)
    {
        var sql = "SELECT * FROM clipboard_entries WHERE id = @id";
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return ReadEntry(reader);

        return null;
    }

    public async Task<IReadOnlyList<ClipboardEntry>> GetRecentEntriesAsync(int limit = 100)
    {
        var sql = "SELECT * FROM clipboard_entries ORDER BY created_at DESC LIMIT @limit";
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@limit", limit);

        return await ReadEntriesAsync(cmd);
    }

    public async Task<IReadOnlyList<ClipboardEntry>> SearchEntriesAsync(string keyword, int limit = 50)
    {
        var sql = @"
            SELECT * FROM clipboard_entries
            WHERE plain_text LIKE @keyword
               OR source_app LIKE @keyword
               OR source_window LIKE @keyword
               OR file_name LIKE @keyword
            ORDER BY created_at DESC
            LIMIT @limit";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");
        cmd.Parameters.AddWithValue("@limit", limit);

        return await ReadEntriesAsync(cmd);
    }

    public async Task<IReadOnlyList<ClipboardEntry>> GetEntriesByTypeAsync(ContentType type, int limit = 50)
    {
        var sql = "SELECT * FROM clipboard_entries WHERE content_type = @type ORDER BY created_at DESC LIMIT @limit";
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@type", type.ToString().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@limit", limit);

        return await ReadEntriesAsync(cmd);
    }

    public async Task<IReadOnlyList<ClipboardEntry>> GetEntriesByDateRangeAsync(DateTime from, DateTime to, int limit = 100)
    {
        var sql = @"
            SELECT * FROM clipboard_entries
            WHERE created_at >= @from AND created_at <= @to
            ORDER BY created_at DESC LIMIT @limit";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@from", from.ToString("o"));
        cmd.Parameters.AddWithValue("@to", to.ToString("o"));
        cmd.Parameters.AddWithValue("@limit", limit);

        return await ReadEntriesAsync(cmd);
    }

    public async Task<bool> DeleteEntryAsync(string id)
    {
        var sql = "DELETE FROM clipboard_entries WHERE id = @id";
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@id", id);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<int> DeleteEntriesBeforeAsync(DateTime before)
    {
        var sql = "DELETE FROM clipboard_entries WHERE created_at < @before AND is_favorite = 0";
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@before", before.ToString("o"));

        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> UpdateEntryAsync(ClipboardEntry entry)
    {
        var sql = @"
            UPDATE clipboard_entries SET
                content_type = @content_type,
                plain_text = @plain_text,
                rich_text = @rich_text,
                file_path = @file_path,
                file_name = @file_name,
                source_app = @source_app,
                source_window = @source_window,
                char_count = @char_count,
                tags = @tags,
                is_favorite = @is_favorite,
                is_pinned = @is_pinned,
                is_sensitive = @is_sensitive,
                in_knowledge_base = @in_knowledge_base,
                importance_score = @importance_score,
                last_accessed = @last_accessed
            WHERE id = @id";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        AddEntryParameters(cmd, entry);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<int> GetEntryCountAsync()
    {
        var sql = "SELECT COUNT(*) FROM clipboard_entries";
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task<long> GetDatabaseSizeAsync()
    {
        if (!File.Exists(_dbPath)) return 0;
        var info = new FileInfo(_dbPath);
        return await Task.FromResult(info.Length);
    }

    private static void AddEntryParameters(SqliteCommand cmd, ClipboardEntry entry)
    {
        cmd.Parameters.AddWithValue("@id", entry.Id);
        cmd.Parameters.AddWithValue("@content_type", entry.ContentType.ToString().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@plain_text", entry.PlainText ?? string.Empty);
        cmd.Parameters.AddWithValue("@rich_text", entry.RichText ?? string.Empty);
        cmd.Parameters.AddWithValue("@file_path", entry.FilePath ?? string.Empty);
        cmd.Parameters.AddWithValue("@file_name", entry.FileName ?? string.Empty);
        cmd.Parameters.AddWithValue("@source_app", entry.SourceApp ?? string.Empty);
        cmd.Parameters.AddWithValue("@source_window", entry.SourceWindow ?? string.Empty);
        cmd.Parameters.AddWithValue("@content_hash", entry.ContentHash ?? string.Empty);
        cmd.Parameters.AddWithValue("@char_count", entry.CharCount);
        cmd.Parameters.AddWithValue("@tags", entry.Tags ?? "[]");
        cmd.Parameters.AddWithValue("@is_favorite", entry.IsFavorite ? 1 : 0);
        cmd.Parameters.AddWithValue("@is_pinned", entry.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@is_sensitive", entry.IsSensitive ? 1 : 0);
        cmd.Parameters.AddWithValue("@in_knowledge_base", entry.InKnowledgeBase ? 1 : 0);
        cmd.Parameters.AddWithValue("@importance_score", entry.ImportanceScore);
        cmd.Parameters.AddWithValue("@created_at", entry.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@last_accessed", entry.LastAccessed?.ToString("o") ?? (object)DBNull.Value);
    }

    private static ClipboardEntry ReadEntry(SqliteDataReader reader)
    {
        return new ClipboardEntry
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            ContentType = Enum.Parse<ContentType>(reader.GetString(reader.GetOrdinal("content_type")), true),
            PlainText = reader.IsDBNull(reader.GetOrdinal("plain_text")) ? string.Empty : reader.GetString(reader.GetOrdinal("plain_text")),
            RichText = reader.IsDBNull(reader.GetOrdinal("rich_text")) ? string.Empty : reader.GetString(reader.GetOrdinal("rich_text")),
            FilePath = reader.IsDBNull(reader.GetOrdinal("file_path")) ? string.Empty : reader.GetString(reader.GetOrdinal("file_path")),
            FileName = reader.IsDBNull(reader.GetOrdinal("file_name")) ? string.Empty : reader.GetString(reader.GetOrdinal("file_name")),
            SourceApp = reader.IsDBNull(reader.GetOrdinal("source_app")) ? string.Empty : reader.GetString(reader.GetOrdinal("source_app")),
            SourceWindow = reader.IsDBNull(reader.GetOrdinal("source_window")) ? string.Empty : reader.GetString(reader.GetOrdinal("source_window")),
            ContentHash = reader.GetString(reader.GetOrdinal("content_hash")),
            CharCount = reader.GetInt32(reader.GetOrdinal("char_count")),
            Tags = reader.IsDBNull(reader.GetOrdinal("tags")) ? "[]" : reader.GetString(reader.GetOrdinal("tags")),
            IsFavorite = reader.GetInt32(reader.GetOrdinal("is_favorite")) == 1,
            IsPinned = reader.GetInt32(reader.GetOrdinal("is_pinned")) == 1,
            IsSensitive = reader.GetInt32(reader.GetOrdinal("is_sensitive")) == 1,
            InKnowledgeBase = reader.GetInt32(reader.GetOrdinal("in_knowledge_base")) == 1,
            ImportanceScore = reader.GetDouble(reader.GetOrdinal("importance_score")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at")), null, System.Globalization.DateTimeStyles.RoundtripKind),
            LastAccessed = reader.IsDBNull(reader.GetOrdinal("last_accessed")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("last_accessed")), null, System.Globalization.DateTimeStyles.RoundtripKind)
        };
    }

    private static async Task<IReadOnlyList<ClipboardEntry>> ReadEntriesAsync(SqliteCommand cmd)
    {
        var entries = new List<ClipboardEntry>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            entries.Add(ReadEntry(reader));
        return entries;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
