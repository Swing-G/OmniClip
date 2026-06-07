using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using OmniClip.Core.Interfaces;
using OmniClip.Core.Models;
using Path = System.IO.Path;

namespace OmniClip.App.Services;

public class ClipboardMonitor : IClipboardMonitor
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    private const int WM_DRAWCLIPBOARD = 0x0308;
    private const int WM_CHANGECBCHAIN = 0x030D;

    private IntPtr _nextClipboardViewer = IntPtr.Zero;
    private IntPtr _hwnd = IntPtr.Zero;
    private System.Windows.Interop.HwndSource? _hwndSource;
    private bool _isMonitoring;

    public event EventHandler<ClipboardEntry>? ClipboardChanged;
    public bool IsMonitoring => _isMonitoring;

    private readonly System.Windows.Threading.Dispatcher? _dispatcher;

    public ClipboardMonitor(System.Windows.Threading.Dispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher;
    }

    public void Start()
    {
        if (_isMonitoring) return;

        _hwnd = CreateMessageWindow();
        _nextClipboardViewer = SetClipboardViewer(_hwnd);
        _isMonitoring = true;
    }

    public void Stop()
    {
        if (!_isMonitoring) return;

        ChangeClipboardChain(_hwnd, _nextClipboardViewer);
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
        _isMonitoring = false;
    }

    private IntPtr CreateMessageWindow()
    {
        var parameters = new System.Windows.Interop.HwndSourceParameters("OmniClipClipboardMonitor")
        {
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE
        };
        _hwndSource = new System.Windows.Interop.HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
        return _hwndSource.Handle;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_DRAWCLIPBOARD:
                OnClipboardChanged();
                SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                break;
            case WM_CHANGECBCHAIN:
                if (wParam == _nextClipboardViewer)
                    _nextClipboardViewer = lParam;
                else
                    SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                break;
        }

        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        if (!_isMonitoring) return;

        try
        {
            var entry = CaptureClipboardContent();
            if (entry != null)
            {
                ClipboardChanged?.Invoke(this, entry);
            }
        }
        catch
        {
            // Clipboard might be locked by another process, ignore
        }
    }

    private ClipboardEntry? CaptureClipboardContent()
    {
        if (_dispatcher != null && !_dispatcher.CheckAccess())
        {
            return _dispatcher.Invoke(CaptureClipboardContentInternal);
        }
        return CaptureClipboardContentInternal();
    }

    private ClipboardEntry? CaptureClipboardContentInternal()
    {
        var entry = new ClipboardEntry();

        if (System.Windows.Clipboard.ContainsText())
        {
            var text = System.Windows.Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return null;

            entry.ContentType = DetectContentType(text);
            entry.PlainText = text;
            entry.ContentHash = ComputeHash(text);
            entry.CharCount = text.Length;

            if (System.Windows.Clipboard.ContainsData(System.Windows.DataFormats.Html))
            {
                entry.RichText = System.Windows.Clipboard.GetData(System.Windows.DataFormats.Html) as string ?? string.Empty;
            }
        }
        else if (System.Windows.Clipboard.ContainsImage())
        {
            entry.ContentType = ContentType.Image;
            entry.ContentHash = ComputeHash($"image_{DateTime.UtcNow.Ticks}");
        }
        else if (System.Windows.Clipboard.ContainsFileDropList())
        {
            var files = System.Windows.Clipboard.GetFileDropList();
            if (files.Count == 0) return null;

            entry.ContentType = ContentType.File;
            entry.FilePath = files[0]!;
            entry.FileName = Path.GetFileName(files[0]);
            entry.ContentHash = ComputeHash(string.Join("|", files.Cast<string>()));
            entry.PlainText = string.Join(Environment.NewLine, files.Cast<string>());
        }
        else
        {
            return null;
        }

        CaptureSourceApplication(entry);
        return entry;
    }

    private static ContentType DetectContentType(string text)
    {
        if (Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            return ContentType.Url;
        }

        var codeIndicators = new[] { "{", "}", "=>", "->", "import ", "using ", "function ", "class ", "def ", "var ", "const ", "let " };
        var matchCount = codeIndicators.Count(ind => text.Contains(ind));
        if (matchCount >= 2)
            return ContentType.Code;

        return ContentType.Text;
    }

    private static void CaptureSourceApplication(ClipboardEntry entry)
    {
        try
        {
            var foregroundHandle = GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero) return;

            GetWindowThreadProcessId(foregroundHandle, out var processId);
            var process = System.Diagnostics.Process.GetProcessById((int)processId);
            entry.SourceApp = process.ProcessName;

            var sb = new StringBuilder(256);
            GetWindowText(foregroundHandle, sb, sb.Capacity);
            entry.SourceWindow = sb.ToString();
        }
        catch
        {
            // Cannot determine source, leave empty
        }
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public void Dispose()
    {
        Stop();
    }
}
