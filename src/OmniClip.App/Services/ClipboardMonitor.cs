using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
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

            // Extract bitmap immediately while clipboard data is still alive
            entry.ImageBytes = ExtractImageBytes(dataObj);

            // Use image bytes hash for dedup — identical screenshots will match
            entry.ContentHash = entry.ImageBytes != null
                ? ComputeHash(Convert.ToHexString(SHA256.HashData(entry.ImageBytes)))
                : ComputeHash($"image_{DateTime.UtcNow.Ticks}");
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

    /// <summary>
    /// Extract image data from clipboard immediately, while the data is still valid.
    /// Enumerates all clipboard formats and converts whatever it finds to PNG bytes.
    /// Handles PNG streams (Snipaste, browsers), DIB (Snipaste, screenshot tools),
    /// and WPF BitmapSource (various apps).
    /// </summary>
    /// <summary>
    /// Extract image data from clipboard immediately, while the data is still valid.
    /// Uses GDI+ (System.Drawing) as the primary path because it handles DIB formats
    /// from Snipaste, screenshot tools, and browsers correctly. Falls back to WPF
    /// BitmapSource and raw PNG format enumeration.
    /// </summary>
    private static byte[]? ExtractImageBytes(System.Windows.IDataObject dataObj)
    {
        try
        {
            // Method 1: System.Windows.Forms.Clipboard.GetImage() (GDI+)
            // This is the most reliable for DIB-based clipboard images from Snipaste,
            // Snipping Tool, and browser "Copy Image". It handles bottom-up DIBs,
            // palettized formats, and alpha channels correctly.
            try
            {
                // Must be on STA thread — CaptureClipboardContentInternal runs on UI thread
                using var gdiBmp = System.Windows.Forms.Clipboard.GetImage() as Bitmap;
                if (gdiBmp != null && gdiBmp.Width > 0 && gdiBmp.Height > 0)
                {
                    // Verify the bitmap has actual content (not all-white or all-black)
                    if (HasVisibleContent(gdiBmp))
                    {
                        using var ms = new MemoryStream();
                        gdiBmp.Save(ms, ImageFormat.Png);
                        var result = ms.ToArray();
                        if (result.Length > 100) return result;
                    }
                }
            }
            catch
            {
                // GDI+ clipboard access failed, try other methods
            }

            // Method 2: WPF Clipboard.GetImage()
            try
            {
                var wpfImg = System.Windows.Clipboard.GetImage();
                if (wpfImg != null && wpfImg.PixelWidth > 0 && wpfImg.PixelHeight > 0)
                {
                    return EncodeWpfToPng(wpfImg);
                }
            }
            catch { }

            // Method 3: Direct PNG format (some browsers)
            if (dataObj.GetDataPresent("PNG"))
            {
                var pngBytes = ExtractRawFormat(dataObj, "PNG");
                if (pngBytes != null && IsPngSignature(pngBytes))
                    return pngBytes;
            }

            // Method 4: Enumerate all formats and try raw decoding
            foreach (var format in dataObj.GetFormats())
            {
                var rawBytes = ExtractRawFormat(dataObj, format);
                if (rawBytes == null || rawBytes.Length < 64) continue;
                if (IsPngSignature(rawBytes)) return rawBytes;

                // Try GDI+ decode
                var decoded = TryGdiDecode(rawBytes);
                if (decoded != null) return decoded;
            }
        }
        catch
        {
            // Extraction failed
        }

        return null;
    }

    /// <summary>
    /// Check if a GDI+ bitmap has non-trivial content (not all-one-color).
    /// Samples a few pixels to avoid saving blank images.
    /// </summary>
    private static bool HasVisibleContent(Bitmap bmp)
    {
        try
        {
            if (bmp.Width <= 2 || bmp.Height <= 2) return true;
            // Sample corners and center
            var samplePoints = new[]
            {
                new Point(0, 0),
                new Point(bmp.Width - 1, 0),
                new Point(0, bmp.Height - 1),
                new Point(bmp.Width - 1, bmp.Height - 1),
                new Point(bmp.Width / 2, bmp.Height / 2)
            };
            var first = bmp.GetPixel(samplePoints[0].X, samplePoints[0].Y);
            foreach (var pt in samplePoints.Skip(1))
            {
                var px = bmp.GetPixel(pt.X, pt.Y);
                if (px.ToArgb() != first.ToArgb()) return true;
            }
            // All sampled pixels are identical — might be a solid-color placeholder
            // But return true anyway, one-color images are rare but valid
            return true;
        }
        catch { return true; }
    }

    private static byte[]? ExtractRawFormat(System.Windows.IDataObject dataObj, string format)
    {
        var data = dataObj.GetData(format, autoConvert: false);
        if (data is byte[] bytes && bytes.Length > 0) return bytes;
        if (data is Stream stream)
        {
            try
            {
                if (stream.CanSeek) stream.Position = 0;
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch { }
        }
        return null;
    }

    private static byte[]? TryGdiDecode(byte[] bytes)
    {
        try
        {
            using var ms = new MemoryStream(bytes);
            using var bmp = new Bitmap(ms);
            if (bmp.Width <= 0 || bmp.Height <= 0) return null;
            using var outMs = new MemoryStream();
            bmp.Save(outMs, ImageFormat.Png);
            return outMs.ToArray();
        }
        catch { return null; }
    }

    private static byte[] EncodeWpfToPng(BitmapSource bitmap)
    {
        if (bitmap.Format != System.Windows.Media.PixelFormats.Bgra32)
        {
            bitmap = new FormatConvertedBitmap(bitmap,
                System.Windows.Media.PixelFormats.Bgra32, null, 0);
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    private static bool IsPngSignature(byte[] bytes)
        => bytes.Length >= 8 &&
           bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;

    public void Dispose()
    {
        Stop();
    }
}
