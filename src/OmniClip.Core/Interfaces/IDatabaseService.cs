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
    Task<ClipboardEntry?> FindByHashAsync(string contentHash);
    Task<int> GetEntryCountAsync();
    Task<long> GetDatabaseSizeAsync();
}
