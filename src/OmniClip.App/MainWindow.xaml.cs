using System.Windows;
using OmniClip.Core.Interfaces;

namespace OmniClip.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
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
    }
}
