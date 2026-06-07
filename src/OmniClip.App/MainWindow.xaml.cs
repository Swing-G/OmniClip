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
        await LoadTagFiltersAsync();
        await LoadEntriesAsync();
    }

    private async Task LoadTagFiltersAsync()
    {
        if (_dbService == null) return;

        var tags = new List<TagFilterItem>
        {
            new("📋", "全部", await _dbService.GetEntryCountAsync()),
            new("💻", "代码", (await _dbService.GetEntriesByTypeAsync(ContentType.Code)).Count),
            new("🔗", "链接", (await _dbService.GetEntriesByTypeAsync(ContentType.Url)).Count),
            new("📝", "文本", (await _dbService.GetEntriesByTypeAsync(ContentType.Text)).Count),
            new("🖼️", "图片", (await _dbService.GetEntriesByTypeAsync(ContentType.Image)).Count),
            new("📁", "文件", (await _dbService.GetEntriesByTypeAsync(ContentType.File)).Count),
        };

        TagFilterList.ItemsSource = tags;
    }

    private async void TagFilterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_dbService == null) return;
        if (TagFilterList.SelectedItem is not TagFilterItem tag) return;

        if (tag.Name == "全部")
        {
            await LoadEntriesAsync();
        }
        else if (Enum.TryParse<ContentType>(tag.Name, true, out var type))
        {
            var entries = await _dbService.GetEntriesByTypeAsync(type);
            EntryListMain.ItemsSource = entries;
        }
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

    private void EntryListMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
        {
            PreviewText.Text = entry.PlainText;
            MetaInfo.Text = $"字符数: {entry.CharCount}\n来源: {entry.SourceApp}\n类型: {entry.ContentType}\n标签: {entry.Tags}";
        }
    }

    private async void MainSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadEntriesAsync(MainSearchBox.Text);
    }

    private void CopyEntry_Click(object sender, RoutedEventArgs e)
    {
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
        {
            Clipboard.SetText(entry.PlainText);
        }
    }

    private async void FavoriteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_dbService == null) return;
        if (EntryListMain.SelectedItem is ClipboardEntry entry)
        {
            entry.IsFavorite = !entry.IsFavorite;
            await _dbService.UpdateEntryAsync(entry);
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
}

public record TagFilterItem(string Icon, string Name, int Count);
