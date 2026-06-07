using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;
using OmniClip.Core.Services;
using OmniClip.App.Services;
using OmniClip.App.Views;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;

namespace OmniClip.App;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private IClipboardMonitor? _clipboardMonitor;
    private IHotkeyService? _hotkeyService;
    private IDatabaseService? _databaseService;
    private IStorageService? _storageService;
    private QuickPopup? _quickPopup;
    private MainWindow? _mainWindow;
    private AppConfig _config = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        _config = new AppConfig();
        _storageService = new StorageService(_config);
        _storageService.EnsureDataDirectory();

        _databaseService = new DatabaseService();
        await _databaseService.InitializeAsync(_storageService.GetDatabasePath());

        // Clipboard monitor
        _clipboardMonitor = new ClipboardMonitor(Dispatcher);
        _clipboardMonitor.ClipboardChanged += OnClipboardChanged;

        // Hotkey service
        _hotkeyService = new HotkeyService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        var registered = _hotkeyService.Register(_config.Hotkey);
        if (!registered)
        {
            System.Windows.MessageBox.Show(
                $"无法注册热键 {_config.Hotkey}，可能被其他程序占用。\n尝试注册 Ctrl+Alt+V...",
                "OmniClip - 热键冲突",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            _config.Hotkey = "Ctrl+Alt+V";
            registered = _hotkeyService.Register(_config.Hotkey);
        }

        // Quick popup
        _quickPopup = new QuickPopup(_databaseService);
        _quickPopup.EntryPasted += OnEntryPasted;

        // System tray
        SetupTrayIcon();

        // Start monitoring
        _clipboardMonitor.Start();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "OmniClip - 智能剪贴板",
            Visibility = Visibility.Visible,
            ContextMenu = CreateTrayContextMenu()
        };

        _trayIcon.TrayLeftMouseDown += (s, e) => ShowQuickPopup();
    }

    private ContextMenu CreateTrayContextMenu()
    {
        var menu = new ContextMenu();

        var historyItem = new MenuItem { Header = "📋 显示剪贴板历史" };
        historyItem.Click += (s, e) => ShowMainWindow();
        menu.Items.Add(historyItem);

        var searchItem = new MenuItem { Header = "🔍 快速搜索..." };
        searchItem.Click += (s, e) => ShowQuickPopup();
        menu.Items.Add(searchItem);

        menu.Items.Add(new Separator());

        var pauseItem = new MenuItem { Header = "⏸️ 暂停监听", IsCheckable = true };
        pauseItem.Click += (s, e) =>
        {
            if (pauseItem.IsChecked)
            {
                _clipboardMonitor?.Stop();
                pauseItem.Header = "▶️ 恢复监听";
            }
            else
            {
                _clipboardMonitor?.Start();
                pauseItem.Header = "⏸️ 暂停监听";
            }
        };
        menu.Items.Add(pauseItem);

        menu.Items.Add(new Separator());

        var settingsItem = new MenuItem { Header = "⚙️ 设置" };
        settingsItem.Click += (s, e) => ShowMainWindow();
        menu.Items.Add(settingsItem);

        var exitItem = new MenuItem { Header = "❌ 退出" };
        exitItem.Click += (s, e) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private async void OnClipboardChanged(object? sender, ClipboardEntry entry)
    {
        if (_databaseService == null) return;

        if (entry.CharCount > 0 && entry.CharCount < _config.MinContentLength)
            return;

        await _databaseService.InsertEntryAsync(entry);

        var count = await _databaseService.GetEntryCountAsync();
        Dispatcher.Invoke(() =>
        {
            if (_trayIcon != null)
                _trayIcon.ToolTipText = $"OmniClip - 已记录 {count} 条";
        });
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => ShowQuickPopup());
    }

    private async void ShowQuickPopup()
    {
        if (_quickPopup != null)
        {
            await _quickPopup.ShowAndLoadAsync();
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow(_databaseService!);
            _mainWindow.Closed += (s, e) => _mainWindow = null;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void OnEntryPasted(object? sender, ClipboardEntry entry)
    {
        try
        {
            Clipboard.SetText(entry.PlainText);

            // Delay to allow focus to return to previous window, then simulate Ctrl+V
            Task.Delay(150).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        System.Windows.Forms.SendKeys.SendWait("^v");
                    }
                    catch
                    {
                        // SendKeys may fail in some contexts
                    }
                });
            });
        }
        catch
        {
            // Clipboard may be locked
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _clipboardMonitor?.Dispose();
        _hotkeyService?.Dispose();
        _databaseService?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
