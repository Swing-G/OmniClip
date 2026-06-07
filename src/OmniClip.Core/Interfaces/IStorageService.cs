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
