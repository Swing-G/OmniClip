using System.Windows;
using System.Windows.Controls;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;
using Clipboard = System.Windows.Clipboard;

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
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadEntriesAsync();
    }

    // === Sidebar ===

    private async void SidebarNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_dbService == null) return;
        if (SidebarNav.SelectedItem is not ListBoxItem item) return;

        var tag = item.Tag?.ToString() ?? "all";
        if (Enum.TryParse<ContentType>(tag, true, out var type))
            EntryListMain.ItemsSource = await _dbService.GetEntriesByTypeAsync(type);
        else
            await LoadEntriesAsync();
    }

    // === Feed ===

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

    private async void MainSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadEntriesAsync(MainSearchBox.Text);
    }

    private async void FilterChip_Click(object sender, RoutedEventArgs e)
    {
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

        PreviewMeta.Text = $"Copied from {entry.SourceApp} · {FormatTime(entry.CreatedAt)}";
        PreviewIcon.Text = entry.ContentType switch
        {
            ContentType.Code => "",
            ContentType.Url => "",
            ContentType.Image => "",
            ContentType.File => "",
            _ => ""
        };
        PreviewLangLabel.Text = entry.ContentType.ToString().ToLowerInvariant();

        PreviewText.Text = entry.PlainText;
    }

    // === Preview Actions ===

    private void CopyEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
            Clipboard.SetText(entry.PlainText);
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

    // === Settings ===

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Settings coming soon.", "OmniClip");
    }

    // === Helpers ===

    private static string FormatTime(DateTime dt)
    {
        var local = dt.ToLocalTime();
        return local.Date == DateTime.Today ? $"Today, {local:h:mm tt}" : local.ToString("MMM d");
    }
}
