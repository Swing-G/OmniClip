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
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                await Task.Run(() => Directory.Delete(dir, false));
            }
        }
    }

    public string GetDatabasePath() => Path.Combine(_config.StoragePath, "clipboard.db");

    public string GetFilesDirectory() => Path.Combine(_config.StoragePath, "files");
}
