using OmniClip.Core.Models;

namespace OmniClip.Core.Interfaces;

public interface IClipboardMonitor : IDisposable
{
    event EventHandler<ClipboardEntry>? ClipboardChanged;
    bool IsMonitoring { get; }
    void Start();
    void Stop();
}
