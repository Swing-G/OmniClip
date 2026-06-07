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
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
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
        var size = _storage.GetTotalSize();
        size.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CleanupOldFilesAsync_ShouldRemoveEmptyMonthDirectories()
    {
        _storage.EnsureDataDirectory();
        var oldMonthDir = Path.Combine(_testDir, "files", "2020-01");
        Directory.CreateDirectory(oldMonthDir);

        await _storage.CleanupOldFilesAsync();

        Directory.Exists(oldMonthDir).Should().BeFalse();
    }
}
