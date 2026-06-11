using System;
using System.Windows;
using System.Windows.Input;

namespace OmniClip.App.Views;

public partial class FloatingButton : Window
{
    public event Action? ToggleMainWindow;
    public event Action? Dismiss;

    private bool _isDragging;
    private System.Windows.Point _dragStart;

    public FloatingButton()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            Left = SystemParameters.PrimaryScreenWidth - Width - 20;
            Top = SystemParameters.PrimaryScreenHeight / 2 - Height / 2;
        };
    }

    private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            _dragStart = e.GetPosition(this);
            _isDragging = false;
        }
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(this);
            if (Math.Abs(pos.X - _dragStart.X) > 3 || Math.Abs(pos.Y - _dragStart.Y) > 3)
                _isDragging = true;

            if (_isDragging)
                DragMove();
        }
    }

    protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_isDragging)
            ToggleMainWindow?.Invoke();
    }

    private void Button_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { }

    private void CloseBtn_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
        Dismiss?.Invoke();
    }
}
