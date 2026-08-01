using System.Windows;
using ERechnung.App.Views;
using ERechnung.App.ViewModels;

namespace ERechnung.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // ViewModel mit Repository verknuepfen
        var kundeVm = new KundenViewModel(App.KundeRepo);
        
        // View erstellen und DataContext setzen
        var kundenView = new KundenView();
        kundenView.DataContext = kundeVm;

        // Im Frame anzeigen
        MainFrame.Content = kundenView;
    }
}