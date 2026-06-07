# OmniClip Phase 1 Implementation Plan — MVP 基础框架

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个可用的 Windows 剪贴板管理器 MVP——复制即记录、热键弹窗快速查找粘贴、系统托盘常驻、关键词搜索。

**Architecture:** 分层架构：OmniClip.Core（无 UI 依赖的服务层）+ OmniClip.App（WPF 界面层）+ OmniClip.Tests（单元测试）。Core 层通过接口解耦，便于测试和后续扩展。

**Tech Stack:** C# .NET 8, WPF, SQLite (Microsoft.Data.Sqlite), xUnit

---

## File Structure

```
OmniClip/
├── OmniClip.sln
├── src/
│   ├── OmniClip.Core/
│   │   ├── OmniClip.Core.csproj
│   │   ├── Models/
│   │   │   ├── ClipboardEntry.cs
│   │   │   ├── ContentType.cs
│   │   │   └── AppConfig.cs
│   │   ├── Interfaces/
│   │   │   ├── IClipboardMonitor.cs
│   │   │   ├── IDatabaseService.cs
│   │   │   ├── IStorageService.cs
│   │   │   └── IHotkeyService.cs
│   │   └── Services/
│   │       ├── DatabaseService.cs
│   │       ├── StorageService.cs
│   │       ├── ClipboardMonitor.cs
│   │       └── HotkeyService.cs
│   └── OmniClip.App/
│       ├── OmniClip.App.csproj
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── MainWindow.xaml
│       ├── MainWindow.xaml.cs
│       ├── Views/
│       │   └── QuickPopup.xaml
│       │   └── QuickPopup.xaml.cs
│       ├── Controls/
│       ├── Converters/
│       │   └── RelativeTimeConverter.cs
│       └── Resources/
│           └── app.ico
└── tests/
    └── OmniClip.Tests/
        ├── OmniClip.Tests.csproj
        ├── Services/
        │   ├── DatabaseServiceTests.cs
        │   └── StorageServiceTests.cs
        └── Models/
            └── ClipboardEntryTests.cs
```

---

### Task 1: Solution 脚手架

**Files:**
- Create: `OmniClip.sln`
- Create: `src/OmniClip.Core/OmniClip.Core.csproj`
- Create: `src/OmniClip.App/OmniClip.App.csproj`
- Create: `tests/OmniClip.Tests/OmniClip.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

```bash
cd "D:/Java/Java_Projects/OmniClip"
dotnet new sln -n OmniClip
mkdir -p src/OmniClip.Core src/OmniClip.App tests/OmniClip.Tests
dotnet new classlib -n OmniClip.Core -o src/OmniClip.Core -f net8.0
dotnet new wpf -n OmniClip.App -o src/OmniClip.App -f net8.0-windows
dotnet new xunit -n OmniClip.Tests -o tests/OmniClip.Tests -f net8.0
```

- [ ] **Step 2: Add projects to solution and set references**

```bash
dotnet sln add src/OmniClip.Core/OmniClip.Core.csproj
dotnet sln add src/OmniClip.App/OmniClip.App.csproj
dotnet sln add tests/OmniClip.Tests/OmniClip.Tests.csproj
dotnet add src/OmniClip.App/OmniClip.App.csproj reference src/OmniClip.Core/OmniClip.Core.csproj
dotnet add tests/OmniClip.Tests/OmniClip.Tests.csproj reference src/OmniClip.Core/OmniClip.Core.csproj
```

- [ ] **Step 3: Add NuGet packages**

```bash
dotnet add src/OmniClip.Core/OmniClip.Core.csproj package Microsoft.Data.Sqlite
dotnet add src/OmniClip.Core/OmniClip.Core.csproj package Serilog
dotnet add src/OmniClip.Core/OmniClip.Core.csproj package Serilog.Sinks.File
dotnet add src/OmniClip.App/OmniClip.App.csproj package Hardcodet.NotifyIcon.Wpf
dotnet add tests/OmniClip.Tests/OmniClip.Tests.csproj package FluentAssertions
```

- [ ] **Step 4: Delete auto-generated Class1.cs and verify build**

```bash
rm -f src/OmniClip.Core/Class1.cs tests/OmniClip.Tests/UnitTest1.cs
dotnet build OmniClip.sln
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: scaffold solution with Core, App, and Tests projects"
```

---

### Task 2: 数据模型

**Files:**
- Create: `src/OmniClip.Core/Models/ContentType.cs`
- Create: `src/OmniClip.Core/Models/ClipboardEntry.cs`
- Create: `src/OmniClip.Core/Models/AppConfig.cs`

- [ ] **Step 1: Create ContentType enum**

```csharp
// src/OmniClip.Core/Models/ContentType.cs
namespace OmniClip.Core.Models;

public enum ContentType
{
    Text,
    Image,
    File,
    Url,
    Code
}
```

- [ ] **Step 2: Create ClipboardEntry model**

```csharp
// src/OmniClip.Core/Models/ClipboardEntry.cs
namespace OmniClip.Core.Models;

public class ClipboardEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ContentType ContentType { get; set; } = ContentType.Text;
    public string PlainText { get; set; } = string.Empty;
    public string RichText { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SourceApp { get; set; } = string.Empty;
    public string SourceWindow { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int CharCount { get; set; }
    public string Tags { get; set; } = "[]";
    public bool IsFavorite { get; set; }
    public bool IsPinned { get; set; }
    public bool IsSensitive { get; set; }
    public bool InKnowledgeBase { get; set; }
    public double ImportanceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessed { get; set; }
}
```

- [ ] **Step 3: Create AppConfig model**

```csharp
// src/OmniClip.Core/Models/AppConfig.cs
namespace OmniClip.Core.Models;

public class AppConfig
{
    public string StoragePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OmniClip", "data");

    public int RetentionDays { get; set; } = 30;
    public long MaxStorageBytes { get; set; } = 500 * 1024 * 1024; // 500MB
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;  // 50MB
    public int MinContentLength { get; set; } = 1;
    public string Hotkey { get; set; } = "Win+Shift+V";
    public bool StartWithWindows { get; set; } = true;
    public bool MonitorEnabled { get; set; } = true;
    public List<string> ExcludedApps { get; set; } = new();
}
```

- [ ] **Step 4: Build and verify**

```bash
dotnet build src/OmniClip.Core/OmniClip.Core.csproj
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add data models - ClipboardEntry, ContentType, AppConfig"
```

---

### Task 3: 数据库服务 — 接口与测试先行

**Files:**
- Create: `src/OmniClip.Core/Interfaces/IDatabaseService.cs`
- Create: `tests/OmniClip.Tests/Services/DatabaseServiceTests.cs`

- [ ] **Step 1: Define IDatabaseService interface**

```csharp
// src/OmniClip.Core/Interfaces/IDatabaseService.cs
using OmniClip.Core.Models;

namespace OmniClip.Core.Interfaces;

public interface IDatabaseService : IDisposable
{
    Task InitializeAsync(string dbPath);
    Task<string> InsertEntryAsync(ClipboardEntry entry);
    Task<ClipboardEntry?> GetEntryAsync(string id);
    Task<IReadOnlyList<ClipboardEntry>> GetRecentEntriesAsync(int limit = 100);
    Task<IReadOnlyList<ClipboardEntry>> SearchEntriesAsync(string keyword, int limit = 50);
    Task<IReadOnlyList<ClipboardEntry>> GetEntriesByTypeAsync(ContentType type, int limit = 50);
    Task<IReadOnlyList<ClipboardEntry>> GetEntriesByDateRangeAsync(DateTime from, DateTime to, int limit = 100);
    Task<bool> DeleteEntryAsync(string id);
    Task<int> DeleteEntriesBeforeAsync(DateTime before);
    Task<bool> UpdateEntryAsync(ClipboardEntry entry);
    Task<int> GetEntryCountAsync();
    Task<long> GetDatabaseSizeAsync();
}
```

- [ ] **Step 2: Write failing tests for DatabaseService**

```csharp
// tests/OmniClip.Tests/Services/DatabaseServiceTests.cs
using FluentAssertions;
using OmniClip.Core.Models;
using OmniClip.Core.Services;

namespace OmniClip.Tests.Services;

public class DatabaseServiceTests : IDisposable
{
    private readonly DatabaseService _db;
    private readonly string _dbPath;

    public DatabaseServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_omniclip_{Guid.NewGuid()}.db");
        _db = new DatabaseService();
        _db.InitializeAsync(_dbPath).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task InsertEntryAsync_ShouldReturnId()
    {
        var entry = new ClipboardEntry
        {
            PlainText = "Hello, OmniClip!",
            ContentType = ContentType.Text,
            ContentHash = "abc123"
        };

        var id = await _db.InsertEntryAsync(entry);

        id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEntryAsync_ShouldReturnInsertedEntry()
    {
        var entry = new ClipboardEntry
        {
            PlainText = "Test content",
            ContentType = ContentType.Text,
            ContentHash = "hash1"
        };
        var id = await _db.InsertEntryAsync(entry);

        var result = await _db.GetEntryAsync(id);

        result.Should().NotBeNull();
        result!.PlainText.Should().Be("Test content");
        result.ContentType.Should().Be(ContentType.Text);
    }

    [Fact]
    public async Task GetEntryAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _db.GetEntryAsync("nonexistent-id");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentEntriesAsync_ShouldReturnInDescendingOrder()
    {
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "First", ContentHash = "h1", CreatedAt = DateTime.UtcNow.AddMinutes(-2) });
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "Second", ContentHash = "h2", CreatedAt = DateTime.UtcNow.AddMinutes(-1) });
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "Third", ContentHash = "h3", CreatedAt = DateTime.UtcNow });

        var results = await _db.GetRecentEntriesAsync(10);

        results.Should().HaveCount(3);
        results[0].PlainText.Should().Be("Third");
        results[2].PlainText.Should().Be("First");
    }

    [Fact]
    public async Task SearchEntriesAsync_ShouldMatchKeyword()
    {
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "Redis cache configuration", ContentHash = "h1" });
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "PostgreSQL query optimization", ContentHash = "h2" });
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "Redis cluster setup guide", ContentHash = "h3" });

        var results = await _db.SearchEntriesAsync("Redis");

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteEntryAsync_ShouldRemoveEntry()
    {
        var id = await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "To delete", ContentHash = "h1" });

        var deleted = await _db.DeleteEntryAsync(id);

        deleted.Should().BeTrue();
        var entry = await _db.GetEntryAsync(id);
        entry.Should().BeNull();
    }

    [Fact]
    public async Task GetEntryCountAsync_ShouldReturnCorrectCount()
    {
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "A", ContentHash = "h1" });
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "B", ContentHash = "h2" });

        var count = await _db.GetEntryCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task UpdateEntryAsync_ShouldModifyEntry()
    {
        var entry = new ClipboardEntry { PlainText = "Original", ContentHash = "h1", IsFavorite = false };
        var id = await _db.InsertEntryAsync(entry);

        entry.Id = id;
        entry.IsFavorite = true;
        await _db.UpdateEntryAsync(entry);

        var updated = await _db.GetEntryAsync(id);
        updated!.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteEntriesBeforeAsync_ShouldRemoveOldEntries()
    {
        var oldDate = DateTime.UtcNow.AddDays(-60);
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "Old", ContentHash = "h1", CreatedAt = oldDate });
        await _db.InsertEntryAsync(new ClipboardEntry { PlainText = "New", ContentHash = "h2", CreatedAt = DateTime.UtcNow });

        var deleted = await _db.DeleteEntriesBeforeAsync(DateTime.UtcNow.AddDays(-30));

        deleted.Should().Be(1);
        var count = await _db.GetEntryCountAsync();
        count.Should().Be(1);
    }
}
```

- [ ] **Step 3: Run tests — verify they fail**

```bash
dotnet test tests/OmniClip.Tests/OmniClip.Tests.csproj --filter "DatabaseServiceTests" -v n
```

Expected: Build error — `DatabaseService` does not exist yet.

- [ ] **Step 4: Commit (red tests are expected, will fix next task)**

```bash
git add -A
git commit -m "test: add IDatabaseService interface and failing DatabaseService tests"
```

---

### Task 4: 数据库服务 — 实现

**Files:**
- Create: `src/OmniClip.Core/Services/DatabaseService.cs`

- [ ] **Step 1: Implement DatabaseService**

```csharp
// src/OmniClip.Core/Services/DatabaseService.cs
using Microsoft.Data.Sqlite;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;
using System.Text.Json;

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
```

- [ ] **Step 2: Run tests — verify they pass**

```bash
dotnet test tests/OmniClip.Tests/OmniClip.Tests.csproj --filter "DatabaseServiceTests" -v n
```

Expected: All 8 tests pass.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: implement DatabaseService with SQLite CRUD and passing tests"
```

---

### Task 5: 存储服务

**Files:**
- Create: `src/OmniClip.Core/Interfaces/IStorageService.cs`
- Create: `src/OmniClip.Core/Services/StorageService.cs`
- Create: `tests/OmniClip.Tests/Services/StorageServiceTests.cs`

- [ ] **Step 1: Write failing tests for StorageService**

```csharp
// tests/OmniClip.Tests/Services/StorageServiceTests.cs
using FluentAssertions;
using OmniClip.Core.Models;
using OmniClip.Core.Services;

namespace OmniClip.Tests.Services;

public class StorageServiceTests : IDisposable
{
    private readonly StorageService _storage;
    private readonly string _testDir;

    public StorageServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"omniclip_test_{Guid.NewGuid()}");
        var config = new AppConfig { StoragePath = _testDir };
        _storage = new StorageService(config);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void EnsureDataDirectory_ShouldCreateDirectory()
    {
        _storage.EnsureDataDirectory();

        Directory.Exists(_testDir).Should().BeTrue();
    }

    [Fact]
    public async Task SaveFileAsync_ShouldCopyFileToMonthSubdirectory()
    {
        _storage.EnsureDataDirectory();
        var sourceFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(sourceFile, "test content");

        var savedPath = await _storage.SaveFileAsync(sourceFile, "png");

        savedPath.Should().NotBeNullOrEmpty();
        File.Exists(savedPath).Should().BeTrue();
        var expectedMonth = DateTime.UtcNow.ToString("yyyy-MM");
        savedPath.Should().Contain(expectedMonth);

        File.Delete(sourceFile);
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveFile()
    {
        _storage.EnsureDataDirectory();
        var sourceFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(sourceFile, "test content");

        var savedPath = await _storage.SaveFileAsync(sourceFile, "png");
        var deleted = await _storage.DeleteFileAsync(savedPath);

        deleted.Should().BeTrue();
        File.Exists(savedPath).Should().BeFalse();

        File.Delete(sourceFile);
    }

    [Fact]
    public void GetTotalSize_ShouldReturnDirectorySize()
    {
        _storage.EnsureDataDirectory();
        // Empty directory should return 0 or very small
        var size = _storage.GetTotalSize();
        size.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task CleanupOldFilesAsync_ShouldRemoveEmptyMonthDirectories()
    {
        _storage.EnsureDataDirectory();
        var oldMonthDir = Path.Combine(_testDir, "files", "2020-01");
        Directory.CreateDirectory(oldMonthDir);
        // Empty month directory

        await _storage.CleanupOldFilesAsync();

        Directory.Exists(oldMonthDir).Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

```bash
dotnet test tests/OmniClip.Tests/OmniClip.Tests.csproj --filter "StorageServiceTests" -v n
```

Expected: Build error — `StorageService` does not exist yet.

- [ ] **Step 3: Implement IStorageService interface and StorageService**

```csharp
// src/OmniClip.Core/Interfaces/IStorageService.cs
namespace OmniClip.Core.Interfaces;

public interface IStorageService
{
    void EnsureDataDirectory();
    Task<string> SaveFileAsync(string sourcePath, string extension);
    Task<bool> DeleteFileAsync(string relativeOrAbsolutePath);
    long GetTotalSize();
    Task CleanupOldFilesAsync();
    string GetDatabasePath();
    string GetFilesDirectory();
}
```

```csharp
// src/OmniClip.Core/Services/StorageService.cs
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;

namespace OmniClip.Core.Services;

public class StorageService : IStorageService
{
    private readonly AppConfig _config;

    public StorageService(AppConfig config)
    {
        _config = config;
    }

    public void EnsureDataDirectory()
    {
        if (!Directory.Exists(_config.StoragePath))
            Directory.CreateDirectory(_config.StoragePath);

        var filesDir = GetFilesDirectory();
        if (!Directory.Exists(filesDir))
            Directory.CreateDirectory(filesDir);
    }

    public async Task<string> SaveFileAsync(string sourcePath, string extension)
    {
        var monthDir = Path.Combine(GetFilesDirectory(), DateTime.UtcNow.ToString("yyyy-MM"));
        if (!Directory.Exists(monthDir))
            Directory.CreateDirectory(monthDir);

        var fileName = $"{Guid.NewGuid()}.{extension.TrimStart('.')}";
        var destPath = Path.Combine(monthDir, fileName);

        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write);
        await sourceStream.CopyToAsync(destStream);

        return destPath;
    }

    public async Task<bool> DeleteFileAsync(string relativeOrAbsolutePath)
    {
        var fullPath = Path.IsPathRooted(relativeOrAbsolutePath)
            ? relativeOrAbsolutePath
            : Path.Combine(_config.StoragePath, relativeOrAbsolutePath);

        if (!File.Exists(fullPath))
            return false;

        await Task.Run(() => File.Delete(fullPath));
        return true;
    }

    public long GetTotalSize()
    {
        if (!Directory.Exists(_config.StoragePath))
            return 0;

        return Directory.EnumerateFiles(_config.StoragePath, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
    }

    public async Task CleanupOldFilesAsync()
    {
        var filesDir = GetFilesDirectory();
        if (!Directory.Exists(filesDir))
            return;

        var monthDirs = Directory.GetDirectories(filesDir);
        foreach (var dir in monthDirs)
        {
            if (!Directory.EnumerateFiles(dir).Any())
            {
                await Task.Run(() => Directory.Delete(dir, false));
            }
        }
    }

    public string GetDatabasePath() => Path.Combine(_config.StoragePath, "clipboard.db");

    public string GetFilesDirectory() => Path.Combine(_config.StoragePath, "files");
}
```

- [ ] **Step 4: Run tests — verify they pass**

```bash
dotnet test tests/OmniClip.Tests/OmniClip.Tests.csproj --filter "StorageServiceTests" -v n
```

Expected: All 5 tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: implement StorageService with file management and passing tests"
```

---

### Task 6: 剪贴板监听服务

**Files:**
- Create: `src/OmniClip.Core/Interfaces/IClipboardMonitor.cs`
- Create: `src/OmniClip.Core/Services/ClipboardMonitor.cs`

- [ ] **Step 1: Define IClipboardMonitor interface**

```csharp
// src/OmniClip.Core/Interfaces/IClipboardMonitor.cs
using OmniClip.Core.Models;

namespace OmniClip.Core.Interfaces;

public interface IClipboardMonitor : IDisposable
{
    event EventHandler<ClipboardEntry>? ClipboardChanged;
    bool IsMonitoring { get; }
    void Start();
    void Stop();
}
```

- [ ] **Step 2: Implement ClipboardMonitor with Win32 Hook**

```csharp
// src/OmniClip.Core/Services/ClipboardMonitor.cs
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;

namespace OmniClip.Core.Services;

public class ClipboardMonitor : IClipboardMonitor
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    private const int WM_DRAWCLIPBOARD = 0x0308;
    private const int WM_CHANGECBCHAIN = 0x030D;

    private IntPtr _nextClipboardViewer = IntPtr.Zero;
    private IntPtr _hwnd = IntPtr.Zero;
    private bool _isMonitoring;

    public event EventHandler<ClipboardEntry>? ClipboardChanged;
    public bool IsMonitoring => _isMonitoring;

    private readonly System.Windows.Threading.Dispatcher? _dispatcher;

    public ClipboardMonitor(System.Windows.Threading.Dispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher;
    }

    public void Start()
    {
        if (_isMonitoring) return;

        // We use a hidden WPF window to receive clipboard messages
        _hwnd = GetHiddenWindowHandle();
        _nextClipboardViewer = SetClipboardViewer(_hwnd);
        _isMonitoring = true;
    }

    public void Stop()
    {
        if (!_isMonitoring) return;

        ChangeClipboardChain(_hwnd, _nextClipboardViewer);
        _isMonitoring = false;
    }

    private IntPtr GetHiddenWindowHandle()
    {
        // Use the WPF application main window's handle if available
        // Otherwise create a message-only window
        if (System.Windows.Application.Current?.MainWindow != null)
        {
            var source = System.Windows.PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow);
            if (source != null)
            {
                var hwndSource = (System.Windows.Interop.HwndSource)source;
                hwndSource.AddHook(WndProc);
                return hwndSource.Handle;
            }
        }

        // Fallback: create a message-only hidden window
        var parameters = new System.Windows.Interop.HwndSourceParameters("OmniClipClipboardMonitor")
        {
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE
        };
        var hwndSource2 = new System.Windows.Interop.HwndSource(parameters);
        hwndSource2.AddHook(WndProc);
        return hwndSource2.Handle;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_DRAWCLIPBOARD:
                OnClipboardChanged();
                SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                break;
            case WM_CHANGECBCHAIN:
                if (wParam == _nextClipboardViewer)
                    _nextClipboardViewer = lParam;
                else
                    SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                break;
        }

        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        if (!_isMonitoring) return;

        try
        {
            var entry = CaptureClipboardContent();
            if (entry != null)
            {
                ClipboardChanged?.Invoke(this, entry);
            }
        }
        catch
        {
            // Clipboard might be locked by another process, ignore
        }
    }

    private ClipboardEntry? CaptureClipboardContent()
    {
        // Must run on UI thread for WPF Clipboard access
        if (_dispatcher != null && !_dispatcher.CheckAccess())
        {
            return _dispatcher.Invoke(CaptureClipboardContentInternal);
        }
        return CaptureClipboardContentInternal();
    }

    private ClipboardEntry? CaptureClipboardContentInternal()
    {
        var entry = new ClipboardEntry();

        if (System.Windows.Clipboard.ContainsText())
        {
            var text = System.Windows.Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return null;

            entry.ContentType = DetectContentType(text);
            entry.PlainText = text;
            entry.ContentHash = ComputeHash(text);
            entry.CharCount = text.Length;

            // Try to get rich text
            if (System.Windows.Clipboard.ContainsData(System.Windows.DataFormats.Html))
            {
                entry.RichText = System.Windows.Clipboard.GetData(System.Windows.DataFormats.Html) as string ?? string.Empty;
            }
        }
        else if (System.Windows.Clipboard.ContainsImage())
        {
            entry.ContentType = ContentType.Image;
            entry.ContentHash = ComputeHash($"image_{DateTime.UtcNow.Ticks}");
        }
        else if (System.Windows.Clipboard.ContainsFileDropList())
        {
            var files = System.Windows.Clipboard.GetFileDropList();
            if (files.Count == 0) return null;

            entry.ContentType = ContentType.File;
            entry.FilePath = files[0]!;
            entry.FileName = Path.GetFileName(files[0]);
            entry.ContentHash = ComputeHash(string.Join("|", files.Cast<string>()));
            entry.PlainText = string.Join(Environment.NewLine, files.Cast<string>());
        }
        else
        {
            return null;
        }

        // Capture source application
        CaptureSourceApplication(entry);

        return entry;
    }

    private static ContentType DetectContentType(string text)
    {
        // URL detection
        if (Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            return ContentType.Url;
        }

        // Code detection: heuristics
        var codeIndicators = new[] { "{", "}", "=>", "->", "import ", "using ", "function ", "class ", "def ", "var ", "const ", "let " };
        var matchCount = codeIndicators.Count(ind => text.Contains(ind));
        if (matchCount >= 2)
            return ContentType.Code;

        return ContentType.Text;
    }

    private static void CaptureSourceApplication(ClipboardEntry entry)
    {
        try
        {
            var foregroundHandle = GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero) return;

            var sb = new StringBuilder(256);
            GetWindowThreadProcessId(foregroundHandle, out var processId);
            var process = System.Diagnostics.Process.GetProcessById((int)processId);
            entry.SourceApp = process.ProcessName;

            GetWindowText(foregroundHandle, sb, sb.Capacity);
            entry.SourceWindow = sb.ToString();
        }
        catch
        {
            // Cannot determine source, leave empty
        }
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    public void Dispose()
    {
        Stop();
    }
}
```

- [ ] **Step 3: Build and verify (no unit test for Win32 interop — tested manually later)**

```bash
dotnet build src/OmniClip.Core/OmniClip.Core.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: implement ClipboardMonitor with Win32 clipboard hook"
```

---

### Task 7: 全局热键服务

**Files:**
- Create: `src/OmniClip.Core/Interfaces/IHotkeyService.cs`
- Create: `src/OmniClip.Core/Services/HotkeyService.cs`

- [ ] **Step 1: Define IHotkeyService interface**

```csharp
// src/OmniClip.Core/Interfaces/IHotkeyService.cs
namespace OmniClip.Core.Interfaces;

public interface IHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    bool IsRegistered { get; }
    bool Register(string hotkey);
    void Unregister();
}
```

- [ ] **Step 2: Implement HotkeyService**

```csharp
// src/OmniClip.Core/Services/HotkeyService.cs
using System.Runtime.InteropServices;
using OmniClip.Core.Interfaces;

namespace OmniClip.Core.Services;

public class HotkeyService : IHotkeyService
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 0x0001;

    // Modifier constants
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    private IntPtr _hwnd = IntPtr.Zero;
    private System.Windows.Interop.HwndSource? _hwndSource;
    private bool _isRegistered;

    public event EventHandler? HotkeyPressed;
    public bool IsRegistered => _isRegistered;

    public bool Register(string hotkey)
    {
        if (_isRegistered) Unregister();

        var (modifiers, key) = ParseHotkey(hotkey);
        _hwnd = CreateMessageWindow();

        _isRegistered = RegisterHotKey(_hwnd, HOTKEY_ID, modifiers, key);
        return _isRegistered;
    }

    public void Unregister()
    {
        if (!_isRegistered) return;

        UnregisterHotKey(_hwnd, HOTKEY_ID);
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
        _isRegistered = false;
    }

    private IntPtr CreateMessageWindow()
    {
        var parameters = new System.Windows.Interop.HwndSourceParameters("OmniClipHotkey")
        {
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE
        };
        _hwndSource = new System.Windows.Interop.HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
        return _hwndSource.Handle;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private static (uint modifiers, uint key) ParseHotkey(string hotkey)
    {
        uint modifiers = 0;
        uint key = 0;

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "win": modifiers |= MOD_WIN; break;
                case "ctrl":
                case "control": modifiers |= MOD_CONTROL; break;
                case "alt": modifiers |= MOD_ALT; break;
                case "shift": modifiers |= MOD_SHIFT; break;
                default:
                    key = MapVirtualKey(part);
                    break;
            }
        }

        return (modifiers, key);
    }

    private static uint MapVirtualKey(string keyName)
    {
        return keyName.ToUpperInvariant() switch
        {
            "A" => 0x41, "B" => 0x42, "C" => 0x43, "D" => 0x44,
            "E" => 0x45, "F" => 0x46, "G" => 0x47, "H" => 0x48,
            "I" => 0x49, "J" => 0x4A, "K" => 0x4B, "L" => 0x4C,
            "M" => 0x4D, "N" => 0x4E, "O" => 0x4F, "P" => 0x50,
            "Q" => 0x51, "R" => 0x52, "S" => 0x53, "T" => 0x54,
            "U" => 0x55, "V" => 0x56, "W" => 0x57, "X" => 0x58,
            "Y" => 0x59, "Z" => 0x5A,
            "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33,
            "4" => 0x34, "5" => 0x35, "6" => 0x36, "7" => 0x37,
            "8" => 0x38, "9" => 0x39,
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            "SPACE" => 0x20,
            "TAB" => 0x09,
            "ENTER" => 0x0D,
            "ESC" or "ESCAPE" => 0x1B,
            _ => 0x56 // Default to V
        };
    }

    public void Dispose()
    {
        Unregister();
    }
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build src/OmniClip.Core/OmniClip.Core.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: implement HotkeyService with Win32 RegisterHotKey"
```

---

### Task 8: QuickPopup 迷你弹窗

**Files:**
- Create: `src/OmniClip.App/Views/QuickPopup.xaml`
- Create: `src/OmniClip.App/Views/QuickPopup.xaml.cs`
- Create: `src/OmniClip.App/Converters/RelativeTimeConverter.cs`

- [ ] **Step 1: Create RelativeTimeConverter**

```csharp
// src/OmniClip.App/Converters/RelativeTimeConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace OmniClip.App.Converters;

public class RelativeTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime) return string.Empty;

        var diff = DateTime.UtcNow - dateTime;

        if (diff.TotalMinutes < 1) return "刚刚";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}分钟前";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}小时前";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}天前";
        return dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 2: Create QuickPopup XAML**

```xml
<!-- src/OmniClip.App/Views/QuickPopup.xaml -->
<Window x:Class="OmniClip.App.Views.QuickPopup"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:conv="clr-namespace:OmniClip.App.Converters"
        Title="OmniClip"
        Width="340" Height="440"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="True"
        ResizeMode="NoResize"
        Deactivated="Window_Deactivated">

    <Window.Resources>
        <conv:RelativeTimeConverter x:Key="RelTime"/>
        <BooleanToVisibilityConverter x:Key="BoolVis"/>
    </Window.Resources>

    <Border Background="#FF2D2D3D" CornerRadius="8" BorderBrush="#FF444466" BorderThickness="1">
        <DockPanel>
            <!-- Search Bar -->
            <Border DockPanel.Dock="Top" Padding="8,6" Background="#FF252535" CornerRadius="8,8,0,0">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="🔍" FontSize="14" VerticalAlignment="Center" Margin="0,0,6,0"/>
                    <TextBox Grid.Column="1" x:Name="SearchBox"
                             Background="Transparent" Foreground="#FFCCCCCC"
                             BorderThickness="0" FontSize="13"
                             CaretBrush="#FF6677EE"
                             TextChanged="SearchBox_TextChanged"/>
                </Grid>
            </Border>

            <!-- Entry List -->
            <ListBox x:Name="EntryList"
                     DockPanel.Dock="Top"
                     Background="Transparent"
                     Foreground="#FFCCCCCC"
                     BorderThickness="0"
                     FontSize="12"
                     SelectionChanged="EntryList_SelectionChanged"
                     KeyDown="EntryList_KeyDown">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Margin="2,4">
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="{Binding CreatedAt, Converter={StaticResource RelTime}}"
                                           FontSize="10" Foreground="#FF888888"/>
                                <TextBlock Text=" · " FontSize="10" Foreground="#FF888888"/>
                                <TextBlock Text="{Binding SourceApp}" FontSize="10" Foreground="#FF888888"/>
                            </StackPanel>
                            <TextBlock Text="{Binding PlainText}"
                                       FontSize="12" Foreground="#FFDDDDDD"
                                       TextTrimming="CharacterEllipsis"
                                       MaxHeight="36" TextWrapping="NoWrap"/>
                        </StackPanel>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>

            <!-- Bottom Bar -->
            <Border DockPanel.Dock="Bottom" Padding="8,4" Background="#FF252535" CornerRadius="0,0,8,8">
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                    <TextBlock Text="↑↓ 选择  " FontSize="10" Foreground="#FF666666"/>
                    <TextBlock Text="Enter 粘贴  " FontSize="10" Foreground="#FF666666"/>
                    <TextBlock Text="Esc 关闭" FontSize="10" Foreground="#FF666666"/>
                </StackPanel>
            </Border>
        </DockPanel>
    </Border>
</Window>
```

- [ ] **Step 3: Create QuickPopup code-behind**

```csharp
// src/OmniClip.App/Views/QuickPopup.xaml.cs
using System.Windows;
using System.Windows.Input;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;

namespace OmniClip.App.Views;

public partial class QuickPopup : Window
{
    private readonly IDatabaseService _dbService;

    public event EventHandler<ClipboardEntry>? EntryPasted;

    public QuickPopup(IDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
    }

    public async Task ShowAndLoadAsync()
    {
        PositionNearCursor();
        Show();
        Activate();
        SearchBox.Focus();

        await LoadEntriesAsync();
    }

    private void PositionNearCursor()
    {
        var screen = System.Windows.Forms.Cursor.Position;
        var x = screen.X;
        var y = screen.Y;

        // Ensure window stays on screen
        if (x + Width > SystemParameters.PrimaryScreenWidth)
            x = (int)SystemParameters.PrimaryScreenWidth - (int)Width;
        if (y + Height > SystemParameters.PrimaryScreenHeight)
            y = (int)SystemParameters.PrimaryScreenHeight - (int)Height;

        Left = x;
        Top = y;
    }

    private async Task LoadEntriesAsync(string? keyword = null)
    {
        IReadOnlyList<ClipboardEntry> entries;
        if (!string.IsNullOrWhiteSpace(keyword))
            entries = await _dbService.SearchEntriesAsync(keyword);
        else
            entries = await _dbService.GetRecentEntriesAsync(50);

        EntryList.ItemsSource = entries;
        if (entries.Count > 0)
            EntryList.SelectedIndex = 0;
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadEntriesAsync(SearchBox.Text);
    }

    private void EntryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Selection changed, preview could update here in future
    }

    private void EntryList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PasteSelectedEntry();
        }
        else if (e.Key == Key.Escape)
        {
            Hide();
        }
    }

    private void PasteSelectedEntry()
    {
        if (EntryList.SelectedItem is ClipboardEntry entry)
        {
            EntryPasted?.Invoke(this, entry);
            Hide();
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Hide();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            Hide();
        }
        else if (e.Key == Key.Enter)
        {
            PasteSelectedEntry();
        }
        else if (e.Key == Key.Up)
        {
            if (EntryList.SelectedIndex > 0)
                EntryList.SelectedIndex--;
            EntryList.ScrollIntoView(EntryList.SelectedItem);
        }
        else if (e.Key == Key.Down)
        {
            if (EntryList.SelectedIndex < EntryList.Items.Count - 1)
                EntryList.SelectedIndex++;
            EntryList.ScrollIntoView(EntryList.SelectedItem);
        }
    }
}
```

- [ ] **Step 4: Add Windows Forms reference for cursor position (needed in .csproj)**

Add to `src/OmniClip.App/OmniClip.App.csproj` inside the first `<PropertyGroup>`:

```xml
<UseWindowsForms>true</UseWindowsForms>
```

- [ ] **Step 5: Build and verify**

```bash
dotnet build src/OmniClip.App/OmniClip.App.csproj
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add QuickPopup with search, keyboard navigation, and cursor positioning"
```

---

### Task 9: 系统托盘 + 一键粘贴

**Files:**
- Modify: `src/OmniClip.App/App.xaml`
- Modify: `src/OmniClip.App/App.xaml.cs`

- [ ] **Step 1: Update App.xaml to hide main window on startup**

```xml
<!-- src/OmniClip.App/App.xaml -->
<Application x:Class="OmniClip.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources>
    </Application.Resources>
</Application>
```

- [ ] **Step 2: Update App.xaml.cs — full app lifecycle with tray, hotkey, monitor, paste**

```csharp
// src/OmniClip.App/App.xaml.cs
using System.Windows;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;
using OmniClip.Core.Services;
using OmniClip.App.Views;

namespace OmniClip.App;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private IClipboardMonitor? _clipboardMonitor;
    private IHotkeyService? _hotkeyService;
    private IDatabaseService? _databaseService;
    private IStorageService? _storageService;
    private QuickPopup? _quickPopup;
    private MainWindow? _mainWindow;
    private AppConfig _config = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        _config = new AppConfig();
        _storageService = new StorageService(_config);
        _storageService.EnsureDataDirectory();

        _databaseService = new DatabaseService();
        await _databaseService.InitializeAsync(_storageService.GetDatabasePath());

        // Clipboard monitor
        _clipboardMonitor = new ClipboardMonitor(Dispatcher);
        _clipboardMonitor.ClipboardChanged += OnClipboardChanged;

        // Hotkey service
        _hotkeyService = new HotkeyService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.Register(_config.Hotkey);

        // Quick popup
        _quickPopup = new QuickPopup(_databaseService);
        _quickPopup.EntryPasted += OnEntryPasted;

        // System tray
        SetupTrayIcon();

        // Start monitoring
        _clipboardMonitor.Start();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "OmniClip - 智能剪贴板",
            IconSource = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Resources/app.ico", UriKind.Absolute)),
            Visibility = Visibility.Visible,
            ContextMenu = CreateTrayContextMenu()
        };

        _trayIcon.TrayLeftMouseDown += (s, e) => ShowQuickPopup();
    }

    private System.Windows.Controls.ContextMenu CreateTrayContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var historyItem = new System.Windows.Controls.MenuItem { Header = "📋 显示剪贴板历史" };
        historyItem.Click += (s, e) => ShowMainWindow();
        menu.Items.Add(historyItem);

        var searchItem = new System.Windows.Controls.MenuItem { Header = "🔍 快速搜索..." };
        searchItem.Click += (s, e) => ShowQuickPopup();
        menu.Items.Add(searchItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var pauseItem = new System.Windows.Controls.MenuItem { Header = "⏸️ 暂停监听", IsCheckable = true };
        pauseItem.Click += (s, e) =>
        {
            if (pauseItem.IsChecked)
            {
                _clipboardMonitor?.Stop();
                pauseItem.Header = "▶️ 恢复监听";
            }
            else
            {
                _clipboardMonitor?.Start();
                pauseItem.Header = "⏸️ 暂停监听";
            }
        };
        menu.Items.Add(pauseItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "⚙️ 设置" };
        settingsItem.Click += (s, e) => ShowMainWindow();
        menu.Items.Add(settingsItem);

        var exitItem = new System.Windows.Controls.MenuItem { Header = "❌ 退出" };
        exitItem.Click += (s, e) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private async void OnClipboardChanged(object? sender, ClipboardEntry entry)
    {
        if (_databaseService == null) return;

        // Skip if content too short
        if (entry.CharCount > 0 && entry.CharCount < _config.MinContentLength)
            return;

        await _databaseService.InsertEntryAsync(entry);

        // Update tray tooltip
        var count = await _databaseService.GetEntryCountAsync();
        Dispatcher.Invoke(() =>
        {
            if (_trayIcon != null)
                _trayIcon.ToolTipText = $"OmniClip - 已记录 {count} 条";
        });
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => ShowQuickPopup());
    }

    private async void ShowQuickPopup()
    {
        if (_quickPopup != null)
        {
            await _quickPopup.ShowAndLoadAsync();
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow(_databaseService!);
            _mainWindow.Closed += (s, e) => _mainWindow = null;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void OnEntryPasted(object? sender, ClipboardEntry entry)
    {
        // Set clipboard content and simulate Ctrl+V
        System.Windows.Clipboard.SetText(entry.PlainText);

        // Delay to allow focus to return to previous window
        Task.Delay(100).ContinueWith(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                SendKeys.SendWait("^v");
            });
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _clipboardMonitor?.Dispose();
        _hotkeyService?.Dispose();
        _databaseService?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 3: Add SendKeys using — add to OmniClip.App.csproj**

Ensure the `<UseWindowsForms>true</UseWindowsForms>` line is in the csproj (added in Task 8). Also add:

```xml
<ItemGroup>
    <Reference Include="System.Windows.Forms" />
</ItemGroup>
```

Actually, `SendKeys` requires `System.Windows.Forms`. With `<UseWindowsForms>true</UseWindowsForms>` in the PropertyGroup, this reference is auto-available. No extra `<Reference>` needed.

- [ ] **Step 4: Create a placeholder icon resource**

Create a minimal `.ico` file. For now, use any small `.ico` file at:

```
src/OmniClip.App/Resources/app.ico
```

If you don't have one handy, create a 16x16 placeholder. You can download a free icon or generate one with any tool. The file just needs to exist for the build.

- [ ] **Step 5: Build and verify**

```bash
dotnet build src/OmniClip.App/OmniClip.App.csproj
```

Expected: Build succeeded (may have warning about missing icon if not placed yet).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: wire up App lifecycle with tray, hotkey, monitor, and paste"
```

---

### Task 10: MainWindow 主窗口

**Files:**
- Modify: `src/OmniClip.App/MainWindow.xaml`
- Modify: `src/OmniClip.App/MainWindow.xaml.cs`

- [ ] **Step 1: Create MainWindow XAML with three-column layout**

```xml
<!-- src/OmniClip.App/MainWindow.xaml -->
<Window x:Class="OmniClip.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="OmniClip - 剪贴板管理器"
        Width="800" Height="600"
        Background="#FF1E1E2E"
        WindowStartupLocation="CenterScreen">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="160"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="240"/>
        </Grid.ColumnDefinitions>

        <!-- Left: Tag Filter -->
        <Border Grid.Column="0" Background="#FF252535" BorderBrush="#FF333344" BorderThickness="0,0,1,0">
            <StackPanel Margin="0,8">
                <TextBlock Text="分类筛选" FontSize="11" Foreground="#FF888888" Margin="12,0,0,8"
                           FontWeight="Bold"/>

                <ListBox x:Name="TagFilterList" Background="Transparent"
                         Foreground="#FFCCCCCC" BorderThickness="0"
                         FontSize="12" SelectionChanged="TagFilterList_SelectionChanged">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal" Margin="4,2">
                                <TextBlock Text="{Binding Icon}" Margin="0,0,6,0"/>
                                <TextBlock Text="{Binding Name}"/>
                                <TextBlock Text="{Binding Count, StringFormat=' {0}'}"
                                           Foreground="#FF666666" FontSize="10" Margin="4,0,0,0"/>
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </StackPanel>
        </Border>

        <!-- Center: Entry List -->
        <Grid Grid.Column="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Search bar -->
            <Border Grid.Row="0" Padding="8,6" Background="#FF252535"
                    BorderBrush="#FF333344" BorderThickness="0,0,0,1">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBox Grid.Column="0" x:Name="MainSearchBox"
                             Background="#FF2A2A3E" Foreground="#FFCCCCCC"
                             BorderThickness="0" FontSize="13"
                             Padding="6,4" Margin="0,0,8,0"
                             CaretBrush="#FF6677EE"
                             TextChanged="MainSearchBox_TextChanged"/>
                    <ComboBox Grid.Column="1" x:Name="SortCombo"
                              Background="#FF2A2A3E" Foreground="#FFCCCCCC"
                              BorderThickness="0" FontSize="11"
                              SelectedIndex="0">
                        <ComboBoxItem Content="时间降序"/>
                        <ComboBoxItem Content="相关性"/>
                    </ComboBox>
                </Grid>
            </Border>

            <!-- Entry list -->
            <ListBox Grid.Row="1" x:Name="EntryListMain"
                     Background="Transparent" Foreground="#FFCCCCCC"
                     BorderThickness="0" FontSize="12"
                     SelectionChanged="EntryListMain_SelectionChanged">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Margin="4,6">
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="{Binding CreatedAt, StringFormat='{}{0:HH:mm}'}"
                                           FontSize="10" Foreground="#FF888888"/>
                                <TextBlock Text=" · " FontSize="10" Foreground="#FF888888"/>
                                <TextBlock Text="{Binding SourceApp}" FontSize="10" Foreground="#FF888888"/>
                            </StackPanel>
                            <TextBlock Text="{Binding PlainText}" FontSize="12"
                                       TextTrimming="CharacterEllipsis" MaxHeight="36"/>
                            <TextBlock Text="{Binding Tags}" FontSize="10" Foreground="#FF6677EE"
                                       TextTrimming="CharacterEllipsis"/>
                        </StackPanel>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </Grid>

        <!-- Right: Preview -->
        <Border Grid.Column="2" Background="#FF252535" BorderBrush="#FF333344" BorderThickness="1,0,0,0"
                Padding="12">
            <StackPanel>
                <TextBlock Text="预览" FontSize="11" Foreground="#FF888888" FontWeight="Bold" Margin="0,0,0,8"/>

                <ScrollViewer VerticalScrollBarVisibility="Auto" MaxHeight="300">
                    <TextBlock x:Name="PreviewText" Text="选择一条记录查看预览"
                               Foreground="#FFCCCCCC" FontSize="12" TextWrapping="Wrap"/>
                </ScrollViewer>

                <StackPanel x:Name="EntryMetadata" Margin="0,12,0,0">
                    <TextBlock x:Name="MetaInfo" FontSize="10" Foreground="#FF888888" TextWrapping="Wrap"/>
                </StackPanel>

                <StackPanel Orientation="Horizontal" Margin="0,12,0,0">
                    <Button Content="📋 复制" Click="CopyEntry_Click"
                            Background="#FF6677EE" Foreground="White"
                            BorderThickness="0" Padding="8,4" Margin="0,0,6,0"
                            FontSize="11"/>
                    <Button Content="⭐ 收藏" Click="FavoriteEntry_Click"
                            Background="#FF444466" Foreground="White"
                            BorderThickness="0" Padding="8,4" Margin="0,0,6,0"
                            FontSize="11"/>
                    <Button Content="🗑️ 删除" Click="DeleteEntry_Click"
                            Background="#FF444466" Foreground="White"
                            BorderThickness="0" Padding="8,4"
                            FontSize="11"/>
                </StackPanel>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 2: Create MainWindow code-behind**

```csharp
// src/OmniClip.App/MainWindow.xaml.cs
using System.Windows;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;

namespace OmniClip.App;

public partial class MainWindow : Window
{
    private readonly IDatabaseService _dbService;

    public MainWindow(IDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadTagFiltersAsync();
        await LoadEntriesAsync();
    }

    private async Task LoadTagFiltersAsync()
    {
        var tags = new List<TagFilterItem>
        {
            new("📋", "全部", await _dbService.GetEntryCountAsync()),
            new("💻", "代码", (await _dbService.GetEntriesByTypeAsync(ContentType.Code)).Count),
            new("🔗", "链接", (await _dbService.GetEntriesByTypeAsync(ContentType.Url)).Count),
            new("📝", "文本", (await _dbService.GetEntriesByTypeAsync(ContentType.Text)).Count),
            new("🖼️", "图片", (await _dbService.GetEntriesByTypeAsync(ContentType.Image)).Count),
            new("📁", "文件", (await _dbService.GetEntriesByTypeAsync(ContentType.File)).Count),
        };

        TagFilterList.ItemsSource = tags;
    }

    private async void TagFilterList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TagFilterList.SelectedItem is TagFilterItem tag)
        {
            if (tag.Name == "全部")
            {
                await LoadEntriesAsync();
            }
            else if (Enum.TryParse<ContentType>(tag.Name, true, out var type))
            {
                var entries = await _dbService.GetEntriesByTypeAsync(type);
                EntryListMain.ItemsSource = entries;
            }
        }
    }

    private async Task LoadEntriesAsync(string? keyword = null)
    {
        IReadOnlyList<ClipboardEntry> entries;
        if (!string.IsNullOrWhiteSpace(keyword))
            entries = await _dbService.SearchEntriesAsync(keyword);
        else
            entries = await _dbService.GetRecentEntriesAsync(200);

        EntryListMain.ItemsSource = entries;
    }

    private void EntryListMain_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
        {
            PreviewText.Text = entry.PlainText;
            MetaInfo.Text = $"字符数: {entry.CharCount}\n来源: {entry.SourceApp}\n类型: {entry.ContentType}\n标签: {entry.Tags}";
        }
    }

    private async void MainSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        await LoadEntriesAsync(MainSearchBox.Text);
    }

    private void CopyEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
        {
            Clipboard.SetText(entry.PlainText);
        }
    }

    private async void FavoriteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
        {
            entry.IsFavorite = !entry.IsFavorite;
            await _dbService.UpdateEntryAsync(entry);
        }
    }

    private async void DeleteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
        {
            await _dbService.DeleteEntryAsync(entry.Id);
            await LoadEntriesAsync();
        }
    }
}

public record TagFilterItem(string Icon, string Name, int Count);
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build src/OmniClip.App/OmniClip.App.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add MainWindow with three-column layout, search, preview, and actions"
```

---

### Task 11: 运行并手动验证 MVP

- [ ] **Step 1: Run the application**

```bash
cd src/OmniClip.App
dotnet run
```

Expected behavior:
1. App starts, system tray icon appears
2. Copy some text — tray tooltip updates with count
3. Press `Win+Shift+V` — QuickPopup appears near cursor
4. Type in search — list filters
5. Click entry or press Enter — text pasted to previous window
6. Double-click tray icon — MainWindow opens with three-column layout
7. Right-click tray — context menu works
8. Esc closes popup

- [ ] **Step 2: Run all unit tests**

```bash
dotnet test tests/OmniClip.Tests/OmniClip.Tests.csproj -v n
```

Expected: All tests pass.

- [ ] **Step 3: Commit any fixes**

```bash
git add -A
git commit -m "fix: address issues found during manual MVP testing"
```

---

### Task 12: 集成测试 & 清理

- [ ] **Step 1: Run full solution build**

```bash
dotnet build OmniClip.sln -c Release
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 2: Run all tests in Release mode**

```bash
dotnet test OmniClip.sln -c Release -v n
```

Expected: All tests pass.

- [ ] **Step 3: Add .gitignore entries for build artifacts**

Ensure `.gitignore` contains:

```
# Superpowers brainstorming artifacts
.superpowers/

# .NET
bin/
obj/
*.user
*.suo
```

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "chore: MVP Phase 1 complete - basic clipboard manager working"
```

---

## Spec Coverage Check

| Spec Requirement (P0) | Task |
|------------------------|------|
| 剪贴板监听 | Task 6 |
| 历史时间线 | Task 10 (MainWindow list, sorted by date) |
| 全局热键 Win+Shift+V | Task 7 + Task 9 |
| 系统托盘 | Task 9 |
| 基础搜索 | Task 8 (QuickPopup) + Task 10 (MainWindow) |
| 一键粘贴 | Task 9 (OnEntryPasted) |
| 存储管理 (自定义路径) | Task 5 (AppConfig.StoragePath) |
