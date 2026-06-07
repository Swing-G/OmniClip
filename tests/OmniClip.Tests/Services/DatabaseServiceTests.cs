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
