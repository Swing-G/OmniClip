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
