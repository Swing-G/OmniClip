namespace OmniClip.Core.Interfaces;

public interface IHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    bool IsRegistered { get; }
    bool Register(string hotkey);
    void Unregister();
}
