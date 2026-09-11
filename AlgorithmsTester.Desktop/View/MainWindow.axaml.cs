using AlgorithmsTester.Desktop.ViewMadels;
using Avalonia.Controls;

namespace AlgorithmsTester.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}