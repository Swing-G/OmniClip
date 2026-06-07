using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;
using Clipboard = System.Windows.Clipboard;

namespace OmniClip.App;

public partial class MainWindow : Window
{
    private readonly IDatabaseService? _dbService;
    private string _currentFilter = "all";

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(IDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadEntriesAsync();
    }

    // === Window Chrome ===

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            MaximizeWindow_Click(sender, e);
        else if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeWindow_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
        => Hide();

    // === Sidebar Navigation ===

    private async void SidebarNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_dbService == null) return;
        if (SidebarNav.SelectedItem is not ListBoxItem item) return;

        var tag = item.Tag?.ToString() ?? "all";
        _currentFilter = tag;

        if (tag == "all")
        {
            await LoadEntriesAsync();
            return;
        }

        if (Enum.TryParse<ContentType>(tag, true, out var type))
        {
            var entries = await _dbService.GetEntriesByTypeAsync(type);
            EntryListMain.ItemsSource = entries;
        }
    }

    // === Filter Chips ===

    private async void FilterChip_Click(object sender, RoutedEventArgs e)
    {
        if (_dbService == null) return;
        await LoadEntriesAsync();
    }

    // === Feed List ===

    private async Task LoadEntriesAsync(string? keyword = null)
    {
        if (_dbService == null) return;

        IReadOnlyList<ClipboardEntry> entries;
        if (!string.IsNullOrWhiteSpace(keyword))
            entries = await _dbService.SearchEntriesAsync(keyword);
        else
            entries = await _dbService.GetRecentEntriesAsync(200);

        EntryListMain.ItemsSource = entries;
    }

    private void EntryListMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntryListMain.SelectedItem is not ClipboardEntry entry) return;

        // Update preview header
        PreviewTitle.Text = entry.ContentType switch
        {
            ContentType.Code => "Code Snippet",
            ContentType.Url => "Web Link",
            ContentType.Image => "Image",
            ContentType.File => "File",
            _ => "Plain Text"
        };

        PreviewMeta.Text = $"Copied from {entry.SourceApp} · {FormatTimestamp(entry.CreatedAt)} · {FormatSize(entry.CharCount)}";
        PreviewIcon.Text = entry.ContentType switch
        {
            ContentType.Code => "",
            ContentType.Url => "",
            ContentType.Image => "",
            ContentType.File => "",
            _ => ""
        };

        PreviewLangLabel.Text = entry.ContentType switch
        {
            ContentType.Code => "code",
            ContentType.Url => "url",
            _ => "text"
        };

        PreviewText.Text = entry.PlainText;
        PreviewText.Style = entry.ContentType == ContentType.Code
            ? (Style)FindResource("CodeSnippetStyle")
            : (Style)FindResource("BodyMd");
    }

    private async void MainSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadEntriesAsync(MainSearchBox.Text);
    }

    // === Preview Actions ===

    private void CopyEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
            Clipboard.SetText(entry.PlainText);
    }

    private void ShareEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryListMain.SelectedItem is not ClipboardEntry entry) return;
        Clipboard.SetText(entry.PlainText);
        // Future: integrate with Windows Share contract
    }

    private void OpenExternal_Click(object sender, RoutedEventArgs e)
    {
        if (EntryListMain.SelectedItem is not ClipboardEntry entry) return;
        if (entry.ContentType == ContentType.Url && Uri.TryCreate(entry.PlainText.Trim(), UriKind.Absolute, out var uri))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
        }
    }

    // === Other buttons ===

    private async void AiInsights_Click(object sender, RoutedEventArgs e)
    {
        // Placeholder: will trigger AI summary in future phase
        System.Windows.MessageBox.Show("AI Insights will be available in a future update.", "OmniClip");
        await Task.CompletedTask;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // Placeholder: will open settings window in future phase
        System.Windows.MessageBox.Show("Settings will be available in a future update.", "OmniClip");
    }

    // === Helpers ===

    private static string FormatTimestamp(DateTime dt)
    {
        var local = dt.ToLocalTime();
        if (local.Date == DateTime.Today)
            return "Today, " + local.ToString("h:mm tt");
        if (local.Date == DateTime.Today.AddDays(-1))
            return "Yesterday";
        return local.ToString("MMM d");
    }

    private static string FormatSize(int charCount)
    {
        if (charCount < 1024) return $"{charCount} B";
        if (charCount < 1024 * 1024) return $"{charCount / 1024.0:F1} KB";
        return $"{charCount / (1024.0 * 1024):F1} MB";
    }
}
