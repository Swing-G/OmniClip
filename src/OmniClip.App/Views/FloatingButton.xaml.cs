using System.Windows;
using System.Windows.Input;

namespace OmniClip.App.Views;

public partial class FloatingButton : Window
{
    public event Action? OpenMainWindow;
    public event Action? Dismiss;

    public FloatingButton()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            Left = SystemParameters.PrimaryScreenWidth - Width - 20;
            Top = SystemParameters.PrimaryScreenHeight / 2 - Height / 2;
        };
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Button_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            OpenMainWindow?.Invoke();
    }

    private void CloseBtn_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Dismiss?.Invoke();
    }
}
