using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;
using Button = System.Windows.Controls.Button;

namespace OmniClip.App.Views;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly AppConfig _original;
    private readonly IDatabaseService? _dbService;

    public bool Saved { get; private set; }

    public SettingsWindow(AppConfig config, IDatabaseService? dbService = null)
    {
        InitializeComponent();
        _config = config;
        _dbService = dbService;
        _original = CloneConfig(config);
        LoadConfig();
    }

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            DragMove();
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn)
        {
            var appName = btn.Content?.ToString();
            if (string.IsNullOrEmpty(appName)) return;

            var current = ExcludedAppsInput.Text
                .Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            if (!current.Contains(appName, StringComparer.OrdinalIgnoreCase))
            {
                current.Add(appName);
                ExcludedAppsInput.Text = string.Join(Environment.NewLine, current);
            }
        }
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Cancel_Click(sender, e);
    }

    private void LoadConfig()
    {
        StoragePathInput.Text = _config.StoragePath;
        RetentionInput.Text = _config.RetentionDays.ToString();
        MaxStorageInput.Text = (_config.MaxStorageBytes / (1024 * 1024)).ToString();
        MaxTextInput.Text = (_config.MaxTextLength / 1024).ToString();
        MinTextInput.Text = _config.MinContentLength.ToString();
        MaxFileInput.Text = (_config.MaxFileSizeBytes / (1024 * 1024)).ToString();
        ImageMaxWInput.Text = _config.ImageMaxWidth.ToString();
        ImageMaxHInput.Text = _config.ImageMaxHeight.ToString();
        HotkeyInput.Text = _config.Hotkey;

        CaptureTextChk.IsChecked = _config.CaptureText;
        CaptureImageChk.IsChecked = _config.CaptureImage;
        CaptureFileChk.IsChecked = _config.CaptureFile;
        MonitorChk.IsChecked = _config.MonitorEnabled;
        StartupChk.IsChecked = _config.StartWithWindows;

        ExcludedAppsInput.Text = string.Join(Environment.NewLine, _config.ExcludedApps);
    }

    private void BrowsePath_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select OmniClip data directory",
            UseDescriptionForTitle = true,
            SelectedPath = StoragePathInput.Text
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            StoragePathInput.Text = dialog.SelectedPath;
    }

    private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        var defaults = new AppConfig();
        StoragePathInput.Text = defaults.StoragePath;
        RetentionInput.Text = defaults.RetentionDays.ToString();
        MaxStorageInput.Text = (defaults.MaxStorageBytes / (1024 * 1024)).ToString();
        MaxTextInput.Text = (defaults.MaxTextLength / 1024).ToString();
        MinTextInput.Text = defaults.MinContentLength.ToString();
        MaxFileInput.Text = (defaults.MaxFileSizeBytes / (1024 * 1024)).ToString();
        ImageMaxWInput.Text = defaults.ImageMaxWidth.ToString();
        ImageMaxHInput.Text = defaults.ImageMaxHeight.ToString();
        HotkeyInput.Text = defaults.Hotkey;
        CaptureTextChk.IsChecked = defaults.CaptureText;
        CaptureImageChk.IsChecked = defaults.CaptureImage;
        CaptureFileChk.IsChecked = defaults.CaptureFile;
        MonitorChk.IsChecked = defaults.MonitorEnabled;
        StartupChk.IsChecked = defaults.StartWithWindows;
        ExcludedAppsInput.Text = string.Join(Environment.NewLine, defaults.ExcludedApps);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Revert to original values
        _config.StoragePath = _original.StoragePath;
        _config.RetentionDays = _original.RetentionDays;
        _config.MaxStorageBytes = _original.MaxStorageBytes;
        _config.MaxTextLength = _original.MaxTextLength;
        _config.MinContentLength = _original.MinContentLength;
        _config.MaxFileSizeBytes = _original.MaxFileSizeBytes;
        _config.ImageMaxWidth = _original.ImageMaxWidth;
        _config.ImageMaxHeight = _original.ImageMaxHeight;
        _config.Hotkey = _original.Hotkey;
        _config.CaptureText = _original.CaptureText;
        _config.CaptureImage = _original.CaptureImage;
        _config.CaptureFile = _original.CaptureFile;
        _config.MonitorEnabled = _original.MonitorEnabled;
        _config.ExcludedApps = _original.ExcludedApps;

        Saved = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _config.StoragePath = StoragePathInput.Text.Trim();
        _config.RetentionDays = ParseInt(RetentionInput.Text, 30);
        _config.MaxStorageBytes = ParseInt(MaxStorageInput.Text, 500) * 1024L * 1024;
        _config.MaxTextLength = ParseInt(MaxTextInput.Text, 512) * 1024;
        _config.MinContentLength = ParseInt(MinTextInput.Text, 1);
        _config.MaxFileSizeBytes = ParseInt(MaxFileInput.Text, 50) * 1024L * 1024;
        _config.ImageMaxWidth = ParseInt(ImageMaxWInput.Text, 1920);
        _config.ImageMaxHeight = ParseInt(ImageMaxHInput.Text, 1080);
        _config.Hotkey = HotkeyInput.Text.Trim();
        _config.CaptureText = CaptureTextChk.IsChecked ?? true;
        _config.CaptureImage = CaptureImageChk.IsChecked ?? true;
        _config.CaptureFile = CaptureFileChk.IsChecked ?? true;
        _config.MonitorEnabled = MonitorChk.IsChecked ?? true;
        _config.StartWithWindows = StartupChk.IsChecked ?? true;

        _config.ExcludedApps = ExcludedAppsInput.Text
            .Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        Saved = true;
        Close();
    }

    private static int ParseInt(string text, int fallback)
        => int.TryParse(text?.Trim(), out var v) && v >= 0 ? v : fallback;

    private static long ParseLong(string text, long fallback)
        => long.TryParse(text?.Trim(), out var v) && v >= 0 ? v : fallback;

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_dbService == null) return;
        if (sender is not System.Windows.Controls.Button btn) return;

        var fmt = btn.Tag?.ToString() ?? "md";
        var (ext, filter) = fmt switch
        {
            "json" => (".json", "JSON (*.json)|*.json"),
            "txt" => (".txt", "Plain Text (*.txt)|*.txt"),
            _ => (".md", "Markdown (*.md)|*.md")
        };

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = filter,
            DefaultExt = ext,
            FileName = $"omniclip_export_{DateTime.Now:yyyyMMdd}"
        };

        if (dialog.ShowDialog() != true) return;

        var entries = await _dbService.GetRecentEntriesAsync(500);
        var content = fmt switch
        {
            "json" => JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }),
            "txt" => ExportAsText(entries),
            _ => ExportAsMarkdown(entries)
        };

        await File.WriteAllTextAsync(dialog.FileName, content);
    }

    private static string ExportAsMarkdown(IReadOnlyList<ClipboardEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# OmniClip Export");
        sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        string? currentDay = null;
        foreach (var e in entries)
        {
            var day = e.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd");
            if (day != currentDay) { currentDay = day; sb.AppendLine($"## {day}\n"); }
            sb.AppendLine($"- **{e.ContentType}** · {e.SourceApp} · {e.CreatedAt.ToLocalTime():HH:mm}");
            var text = e.PlainText.Length > 200 ? e.PlainText[..200] + "..." : e.PlainText;
            sb.AppendLine($"  {text.Replace("\n", "\n  ")}");
            if (!string.IsNullOrEmpty(e.FilePath)) sb.AppendLine($"  📎 `{e.FilePath}`");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string ExportAsText(IReadOnlyList<ClipboardEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.AppendLine($"=== {e.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm} | {e.ContentType} | {e.SourceApp} ===");
            sb.AppendLine(e.PlainText);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static AppConfig CloneConfig(AppConfig src) => new()
    {
        StoragePath = src.StoragePath,
        RetentionDays = src.RetentionDays,
        MaxStorageBytes = src.MaxStorageBytes,
        MaxTextLength = src.MaxTextLength,
        MinContentLength = src.MinContentLength,
        MaxFileSizeBytes = src.MaxFileSizeBytes,
        ImageMaxWidth = src.ImageMaxWidth,
        ImageMaxHeight = src.ImageMaxHeight,
        Hotkey = src.Hotkey,
        CaptureText = src.CaptureText,
        CaptureImage = src.CaptureImage,
        CaptureFile = src.CaptureFile,
        MonitorEnabled = src.MonitorEnabled,
        StartWithWindows = src.StartWithWindows,
        ExcludedApps = new List<string>(src.ExcludedApps)
    };
}
