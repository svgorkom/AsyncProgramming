using System.Windows;

namespace AsynAwaitExamples;

/// <summary>
/// MainWindow acts as the navigation shell.
/// DataContext is set in XAML to MainWindowViewModel which manages navigation.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
