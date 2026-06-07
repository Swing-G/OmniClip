using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace OmniClip.App.Views;

public partial class QuickPopup : Window
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private readonly IDatabaseService _dbService;

    public event EventHandler<ClipboardEntry>? EntryPasted;

    public QuickPopup(IDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
    }

    public async Task ShowAndLoadAsync()
    {
        PositionNearCursor();
        Show();
        Activate();
        SearchBox.Focus();

        await LoadEntriesAsync();
    }

    private void PositionNearCursor()
    {
        GetCursorPos(out var point);

        var x = (double)point.X;
        var y = (double)point.Y;

        if (x + Width > SystemParameters.PrimaryScreenWidth)
            x = SystemParameters.PrimaryScreenWidth - Width;
        if (y + Height > SystemParameters.PrimaryScreenHeight)
            y = SystemParameters.PrimaryScreenHeight - Height;

        Left = x;
        Top = y;
    }

    private async Task LoadEntriesAsync(string? keyword = null)
    {
        IReadOnlyList<ClipboardEntry> entries;
        if (!string.IsNullOrWhiteSpace(keyword))
            entries = await _dbService.SearchEntriesAsync(keyword);
        else
            entries = await _dbService.GetRecentEntriesAsync(50);

        EntryList.ItemsSource = entries;
        if (entries.Count > 0)
            EntryList.SelectedIndex = 0;
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadEntriesAsync(SearchBox.Text);
    }

    private void EntryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void EntryList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            PasteSelectedEntry();
        else if (e.Key == Key.Escape)
            Hide();
    }

    private void PasteSelectedEntry()
    {
        if (EntryList.SelectedItem is ClipboardEntry entry)
        {
            EntryPasted?.Invoke(this, entry);
            Hide();
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Hide();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
            Hide();
        else if (e.Key == Key.Enter)
            PasteSelectedEntry();
        else if (e.Key == Key.Up)
        {
            if (EntryList.SelectedIndex > 0)
                EntryList.SelectedIndex--;
            EntryList.ScrollIntoView(EntryList.SelectedItem);
        }
        else if (e.Key == Key.Down)
        {
            if (EntryList.SelectedIndex < EntryList.Items.Count - 1)
                EntryList.SelectedIndex++;
            EntryList.ScrollIntoView(EntryList.SelectedItem);
        }
    }
}
