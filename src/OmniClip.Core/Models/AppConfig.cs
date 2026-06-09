namespace OmniClip.Core.Models;

public class AppConfig
{
    public string StoragePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OmniClip", "data");

    // Storage
    public int RetentionDays { get; set; } = 30;
    public long MaxStorageBytes { get; set; } = 500 * 1024 * 1024; // 500MB

    // Content type filtering
    public bool CaptureText { get; set; } = true;
    public bool CaptureImage { get; set; } = true;
    public bool CaptureFile { get; set; } = true;

    // Text limits
    public int MaxTextLength { get; set; } = 512 * 1024;       // 512KB: truncate longer text
    public int MinContentLength { get; set; } = 1;

    // File limits
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;  // 50MB
    public int ImageMaxWidth { get; set; } = 1920;
    public int ImageMaxHeight { get; set; } = 1080;

    // Monitor
    public string Hotkey { get; set; } = "Win+Shift+V";
    public bool StartWithWindows { get; set; } = true;
    public bool MonitorEnabled { get; set; } = true;
    public List<string> ExcludedApps { get; set; } = new();
}
