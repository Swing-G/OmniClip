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

        // Retry up to 5 times with 20ms delay — clipboard may be locked by source app
        for (int retry = 0; retry < 5; retry++)
        {
            try
            {
                var entry = CaptureClipboardContent();
                if (entry != null)
                {
                    ClipboardChanged?.Invoke(this, entry);
                    return;
                }
                return; // No content, stop retrying
            }
            catch
            {
                if (retry < 4)
                    System.Threading.Thread.Sleep(20);
            }
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

    private static ClipboardEntry? CaptureClipboardContentInternal()
    {
        var entry = new ClipboardEntry();

        // Check all formats via the data object for more reliable detection
        var dataObj = System.Windows.Clipboard.GetDataObject();
        if (dataObj == null) return null;

        var formats = dataObj.GetFormats();

        // 1) FileDrop first — file copies in Explorer also include a bitmap preview,
        //    so we must check files before images to capture the actual file path
        if (dataObj.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            var files = dataObj.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
            {
                var ext = Path.GetExtension(files[0])?.ToLowerInvariant();
                bool isImageFile = ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" or ".ico" or ".tiff";

                entry.ContentType = isImageFile ? ContentType.Image : ContentType.File;
                entry.FilePath = files[0];
                entry.FileName = Path.GetFileName(files[0]) ?? string.Empty;
                entry.ContentHash = ComputeHash(string.Join("|", files));
                entry.PlainText = string.Join(Environment.NewLine, files);
            }
        }
        // 2) Image (screenshots, browser "copy image", Snipaste etc.)
        else if (formats.Any(f =>
            f == System.Windows.DataFormats.Bitmap ||
            f == "PNG" ||
            f == "DeviceIndependentBitmap" ||
            f == System.Windows.DataFormats.Dib))
        {
            entry.ContentType = ContentType.Image;
            entry.PlainText = "[Image]";
            entry.ContentHash = ComputeHash($"image_{DateTime.UtcNow.Ticks}");
        }
        // 3) Text
        else if (dataObj.GetDataPresent(System.Windows.DataFormats.Text))
        {
            var text = dataObj.GetData(System.Windows.DataFormats.Text) as string;
            if (string.IsNullOrEmpty(text))
                return null;

            entry.ContentType = DetectContentType(text);
            entry.PlainText = text;
            entry.ContentHash = ComputeHash(text);
            entry.CharCount = text.Length;

            if (dataObj.GetDataPresent(System.Windows.DataFormats.Html))
            {
                entry.RichText = dataObj.GetData(System.Windows.DataFormats.Html) as string ?? string.Empty;
            }
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
