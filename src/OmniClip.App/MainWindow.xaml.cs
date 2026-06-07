using System.Windows;
using System.Windows.Controls;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;

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

    private void EntryListMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
            PreviewText.Text = entry.PlainText;
    }
}
