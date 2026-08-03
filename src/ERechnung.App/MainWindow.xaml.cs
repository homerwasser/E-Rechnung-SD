using System.ComponentModel;
using System.Windows;
using ERechnung.App.ViewModels;

namespace ERechnung.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel
            && !viewModel.DarfFensterGeschlossenWerden())
        {
            e.Cancel = true;
        }
    }
}
