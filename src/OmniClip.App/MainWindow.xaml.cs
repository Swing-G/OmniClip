using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OmniClip.App.Services;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;
using Clipboard = System.Windows.Clipboard;
using Application = System.Windows.Application;

namespace OmniClip.App;

public partial class MainWindow : Window
{
    private readonly IDatabaseService? _dbService;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(IDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        Loaded += MainWindow_Loaded;

        // Wire up style and template selectors for time-grouped feed
        EntryListMain.ItemContainerStyleSelector = new FeedItemStyleSelector();
        EntryListMain.ItemTemplateSelector = new FeedTemplateSelector();

        // Use preview handler (tunneling phase) to intercept Copy BEFORE TextBox handles it
        CommandManager.AddPreviewExecutedHandler(this, OnWindowCopy);
    }

    private void OnWindowCopy(object sender, ExecutedRoutedEventArgs e)
    {
        ClipboardMonitor.SuppressNextCapture = true;
        // Let the default copy handler run — our flag will prevent re-capture
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadEntriesAsync();
    }

    public async Task RefreshFeedAsync()
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            // Only refresh if no search/filter is active
            if (string.IsNullOrEmpty(MainSearchBox.Text))
                await LoadEntriesAsync();
        });
    }

    // === Sidebar ===

    private async void SidebarNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_dbService == null) return;
        if (SidebarNav.SelectedItem is not ListBoxItem item) return;

        var tag = item.Tag?.ToString() ?? "all";
        _activeTypeFilter = tag == "all" ? null : tag;
        _pinnedOnly = false;
        await LoadEntriesAsync();
    }

    // === Feed ===

    private string? _activeTypeFilter = null;
    private bool _pinnedOnly = false;

    private async Task LoadEntriesAsync(string? keyword = null)
    {
        if (_dbService == null) return;

        IReadOnlyList<ClipboardEntry> entries;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            entries = await _dbService.SearchEntriesAsync(keyword, 200);
        }
        else if (_activeTypeFilter != null)
        {
            var type = _activeTypeFilter switch
            {
                "text" => ContentType.Text,
                "code" => ContentType.Code,
                "url" => ContentType.Url,
                "image" => ContentType.Image,
                "file" => ContentType.File,
                _ => ContentType.Text
            };
            entries = await _dbService.GetEntriesByTypeAsync(type, 200);
        }
        else
        {
            entries = await _dbService.GetRecentEntriesAsync(200);
        }

        // Pinned chip: show only pinned items.  All Types: show everything.
        if (_pinnedOnly && string.IsNullOrWhiteSpace(keyword))
            entries = entries.Where(e => e.IsPinned).ToList().AsReadOnly();

        EntryListMain.ItemsSource = BuildGroupedList(entries);
    }

    /// <summary>
    /// Build a flat list interleaving section headers and entries by time group.
    /// </summary>
    private static List<object> BuildGroupedList(IReadOnlyList<ClipboardEntry> entries)
    {
        var items = new List<object>();
        string? currentGroup = null;
        bool pinnedHeaderAdded = false;

        foreach (var entry in entries)
        {
            // Add "PINNED" header before first pinned entry
            if (entry.IsPinned && !pinnedHeaderAdded)
            {
                items.Add(new SectionHeader { Label = "PINNED" });
                pinnedHeaderAdded = true;
                currentGroup = null; // reset time groups after pinned section
            }

            // Skip time headers for pinned items
            if (!entry.IsPinned)
            {
                var group = GetTimeGroup(entry.CreatedAt);
                if (group != currentGroup)
                {
                    currentGroup = group;
                    items.Add(new SectionHeader { Label = group });
                }
            }
            items.Add(entry);
        }

        return items;
    }

    private static string GetTimeGroup(DateTime dt)
    {
        var local = dt.ToLocalTime();
        var now = DateTime.Now;

        if (local.Date == now.Date) return "TODAY";
        if (local.Date == now.Date.AddDays(-1)) return "YESTERDAY";
        if (local.Date > now.Date.AddDays(-7)) return "THIS WEEK";
        return "EARLIER";
    }

    private async void MainSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(MainSearchBox.Text)
            ? Visibility.Visible : Visibility.Collapsed;
        await LoadEntriesAsync(MainSearchBox.Text);
    }

    private async void FilterChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn)
        {
            _pinnedOnly = btn == UnpinnedChip;
            // Toggle chip backgrounds
            var activeBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF0, 0xED, 0xED));
            var inactiveBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFC, 0xF9, 0xF8));
            var activeFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1B, 0x1B, 0x1B));
            var inactiveFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x40, 0x47, 0x52));

            AllChip.Background = _pinnedOnly ? inactiveBg : activeBg;
            AllChip.Foreground = _pinnedOnly ? inactiveFg : activeFg;
            UnpinnedChip.Background = _pinnedOnly ? activeBg : inactiveBg;
            UnpinnedChip.Foreground = _pinnedOnly ? activeFg : inactiveFg;
        }
        await LoadEntriesAsync();
    }

    private void EntryListMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntryListMain.SelectedItem is not ClipboardEntry entry) return;

        PreviewTitle.Text = entry.ContentType switch
        {
            ContentType.Code => "Code Snippet",
            ContentType.Url => "Web Link",
            ContentType.Image => "Image",
            ContentType.File => "File",
            _ => "Plain Text"
        };

        var sizeText = GetSizeText(entry);
        PreviewMeta.Text = $"Copied from {entry.SourceApp} · {FormatTime(entry.CreatedAt)} · {sizeText}";
        PreviewIcon.Text = entry.ContentType switch
        {
            ContentType.Code => "",
            ContentType.Url => "",
            ContentType.Image => "",
            ContentType.File => "",
            _ => ""
        };
        PreviewLangLabel.Text = entry.ContentType.ToString().ToLowerInvariant();

        // Toggle between text / image / file preview
        bool isImage = entry.ContentType == ContentType.Image
            && !string.IsNullOrEmpty(entry.FilePath)
            && File.Exists(entry.FilePath);

        bool isFile = entry.ContentType == ContentType.File
            && !string.IsNullOrEmpty(entry.FilePath)
            && File.Exists(entry.FilePath);

        if (isImage)
        {
            PreviewImage.Visibility = Visibility.Visible;
            PreviewText.Visibility = Visibility.Collapsed;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(entry.FilePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            PreviewImage.Source = bitmap;
        }
        else if (isFile)
        {
            var ext = Path.GetExtension(entry.FileName).ToLowerInvariant();
            bool isTextFile = ext is ".txt" or ".md" or ".json" or ".xml" or ".csv" or ".log" or ".ini" or ".cfg" or ".yaml" or ".yml";

            if (isTextFile)
            {
                try
                {
                    PreviewImage.Visibility = Visibility.Collapsed;
                    PreviewText.Visibility = Visibility.Visible;
                    PreviewText.Text = File.ReadAllText(entry.FilePath);
                }
                catch
                {
                    PreviewText.Text = $"Cannot read file:\n{entry.FilePath}";
                }
            }
            else
            {
                // Show file info for binary/non-text files
                PreviewImage.Visibility = Visibility.Collapsed;
                PreviewText.Visibility = Visibility.Visible;
                var fileInfo = new System.IO.FileInfo(entry.FilePath);
                var sizeKb = fileInfo.Length / 1024.0;
                var sizeStr = sizeKb > 1024
                    ? $"{sizeKb / 1024:F1} MB"
                    : $"{sizeKb:F1} KB";
                PreviewText.Text = $"📄 {entry.FileName}\n\n"
                    + $"Size: {sizeStr}\n"
                    + $"Type: {ext.TrimStart('.').ToUpper()} File\n\n"
                    + $"Path: {entry.FilePath}\n\n"
                    + $"Click card or ↗ to open";
            }

            // Enable Open button
            PreviewLangLabel.Text = ext.TrimStart('.').ToUpper();
        }
        else
        {
            PreviewImage.Visibility = Visibility.Collapsed;
            PreviewText.Visibility = Visibility.Visible;
            PreviewText.Text = entry.PlainText;
        }
    }

    // === Preview Actions ===

    private void CopyEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
        {
            ClipboardMonitor.SuppressNextCapture = true;
            CopyEntryToClipboard(entry);
        }
    }

    private static void CopyEntryToClipboard(ClipboardEntry entry)
    {
        // For file/image entries with a valid file path, copy the actual file
        if ((entry.ContentType == ContentType.File || entry.ContentType == ContentType.Image)
            && !string.IsNullOrEmpty(entry.FilePath) && File.Exists(entry.FilePath))
        {
            var fileList = new System.Collections.Specialized.StringCollection
            {
                entry.FilePath
            };
            Clipboard.SetFileDropList(fileList);
        }
        else
        {
            Clipboard.SetText(entry.PlainText);
        }
    }

    private async void DeleteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_dbService == null) return;
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
        {
            await _dbService.DeleteEntryAsync(entry.Id);
            await LoadEntriesAsync();
        }
    }

    // === Title Bar ===

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeOrRestore();
        }
        else if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            DragMove();
        }
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
    {
        MaximizeOrRestore();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MaximizeOrRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void PreviewCard_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        PreviewCard.RenderTransform = new ScaleTransform(1.01, 1.01);
    }

    private void PreviewCard_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        PreviewCard.RenderTransform = new ScaleTransform(1, 1);
    }

    private void PreviewCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            OpenSelectedFile();
    }

    private void OpenInApp_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedFile();
    }

    private void OpenSelectedFile()
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry &&
            !string.IsNullOrEmpty(entry.FilePath) && File.Exists(entry.FilePath))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = entry.FilePath,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    // === Card Buttons ===

    private void CardCopy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is ClipboardEntry entry)
        {
            ClipboardMonitor.SuppressNextCapture = true;
            CopyEntryToClipboard(entry);
        }
    }

    private async void CardPin_Click(object sender, RoutedEventArgs e)
    {
        if (_dbService == null) return;
        if (sender is System.Windows.Controls.Button btn && btn.Tag is ClipboardEntry entry)
        {
            entry.IsPinned = !entry.IsPinned;
            await _dbService.UpdateEntryAsync(entry);

            await LoadEntriesAsync();
        }
    }

    // === Footer Buttons ===

    private void AiInsights_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        System.Windows.MessageBox.Show("AI Insights will be available in a future update.", "OmniClip");
    }

    private void SettingsBtn_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        SettingsPanel.RenderTransform = new ScaleTransform(1.04, 1.04);
        SettingsBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEA, 0xE7, 0xE7));
    }

    private void SettingsBtn_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        SettingsPanel.RenderTransform = new ScaleTransform(1, 1);
        SettingsBtn.Background = System.Windows.Media.Brushes.Transparent;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        SettingsBtn_MouseLeave(sender, null!);
        var app = Application.Current as App;
        if (app == null) return;
        app.OpenSettings();
    }

    // === Helpers ===

    private static string GetSizeText(ClipboardEntry entry)
    {
        // For file/image entries, use actual file size
        if ((entry.ContentType == ContentType.File || entry.ContentType == ContentType.Image)
            && !string.IsNullOrEmpty(entry.FilePath) && File.Exists(entry.FilePath))
        {
            var len = new System.IO.FileInfo(entry.FilePath).Length;
            return len > 1024 * 1024
                ? $"{len / (1024.0 * 1024):F1} MB"
                : $"{len / 1024.0:F1} KB";
        }
        // For text entries, use character count
        if (entry.CharCount > 0)
            return $"{entry.CharCount / 1024.0:F1} KB";
        return "—";
    }

    private static string FormatTime(DateTime dt)
    {
        var local = dt.ToLocalTime();
        return local.Date == DateTime.Today
            ? $"Today, {local:h:mm tt}"
            : local.Date == DateTime.Today.AddDays(-1)
                ? "Yesterday"
                : local.ToString("MMM d");
    }
}

/// <summary>
/// Time-group section header displayed between card groups in the feed.
/// </summary>
public class SectionHeader
{
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Chooses ItemContainerStyle: SectionHeader style for headers, CardItem style for entries.
/// </summary>
public class FeedItemStyleSelector : StyleSelector
{
    public override Style SelectStyle(object item, DependencyObject container)
    {
        var window = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (window == null) return base.SelectStyle(item, container);

        if (item is SectionHeader)
            return window.FindResource("SectionHeader") as Style ?? base.SelectStyle(item, container);

        return window.FindResource("CardItem") as Style ?? base.SelectStyle(item, container);
    }
}

/// <summary>
/// Chooses DataTemplate: ImageCardTemplate for images, SectionTemplate for headers,
/// CardTemplate for everything else.
/// </summary>
public class FeedTemplateSelector : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var window = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (window == null) return base.SelectTemplate(item, container);

        if (item is SectionHeader)
            return window.FindResource("SectionTemplate") as DataTemplate ?? base.SelectTemplate(item, container);

        if (item is ClipboardEntry entry)
        {
            if (entry.ContentType == ContentType.Image)
                return window.FindResource("ImageCardTemplate") as DataTemplate ?? base.SelectTemplate(item, container);
            if (entry.ContentType == ContentType.File)
                return window.FindResource("FileCardTemplate") as DataTemplate ?? base.SelectTemplate(item, container);
        }

        return window.FindResource("CardTemplate") as DataTemplate ?? base.SelectTemplate(item, container);
    }
}
