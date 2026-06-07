using System.Windows;
using System.Windows.Controls;
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
            EntryListMain.ItemsSource = BuildGroupedList(
                await _dbService.GetEntriesByTypeAsync(type, 200));
        else
            await LoadEntriesAsync();
    }

    // === Feed ===

    private async Task LoadEntriesAsync(string? keyword = null)
    {
        if (_dbService == null) return;

        IReadOnlyList<ClipboardEntry> entries;
        if (!string.IsNullOrWhiteSpace(keyword))
            entries = await _dbService.SearchEntriesAsync(keyword, 200);
        else
            entries = await _dbService.GetRecentEntriesAsync(200);

        EntryListMain.ItemsSource = BuildGroupedList(entries);
    }

    /// <summary>
    /// Build a flat list interleaving section headers and entries by time group.
    /// </summary>
    private static List<object> BuildGroupedList(IReadOnlyList<ClipboardEntry> entries)
    {
        var items = new List<object>();
        string? currentGroup = null;

        foreach (var entry in entries)
        {
            var group = GetTimeGroup(entry.CreatedAt);
            if (group != currentGroup)
            {
                currentGroup = group;
                items.Add(new SectionHeader { Label = group });
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

    // === Footer Buttons ===

    private void AiInsights_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        System.Windows.MessageBox.Show("AI Insights will be available in a future update.", "OmniClip");
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Settings coming soon.", "OmniClip");
    }

    // === Helpers ===

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
/// Chooses DataTemplate: simple label for SectionHeader, card layout for ClipboardEntry.
/// </summary>
public class FeedTemplateSelector : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var window = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (window == null) return base.SelectTemplate(item, container);

        if (item is SectionHeader)
            return window.FindResource("SectionTemplate") as DataTemplate ?? base.SelectTemplate(item, container);

        return window.FindResource("CardTemplate") as DataTemplate ?? base.SelectTemplate(item, container);
    }
}
