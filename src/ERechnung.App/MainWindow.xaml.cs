using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ERechnung.App.ViewModels;
using ERechnung.App.Views;

namespace ERechnung.App;

public partial class MainWindow : Window
{
    private readonly KundenView _kundenView;

    public MainWindow(KundenViewModel kundenViewModel)
    {
        InitializeComponent();
        _kundenView = new KundenView { DataContext = kundenViewModel };
        ZeigeKunden();
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) =>
        ZeigePlatzhalter("Dashboard", "Auswertungen werden in einem späteren Meilenstein ergänzt.");

    private void NeueRechnung_Click(object sender, RoutedEventArgs e) =>
        ZeigePlatzhalter("Neue Rechnung", "Die Rechnungserstellung folgt in M3.");

    private void Kunden_Click(object sender, RoutedEventArgs e) => ZeigeKunden();

    private void Vorlagen_Click(object sender, RoutedEventArgs e) =>
        ZeigePlatzhalter("Vorlagen", "Die Vorlagenverwaltung folgt in einem späteren Meilenstein.");

    private void Einstellungen_Click(object sender, RoutedEventArgs e) =>
        ZeigePlatzhalter("Einstellungen", "Firmenprofile und Einstellungen folgen in einem späteren Meilenstein.");

    private void ZeigeKunden() => MainContent.Content = _kundenView;

    private void ZeigePlatzhalter(string titel, string beschreibung)
    {
        MainContent.Content = new StackPanel
        {
            Margin = new Thickness(32),
            Children =
            {
                new TextBlock
                {
                    Text = titel,
                    FontSize = 26,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = beschreibung,
                    Margin = new Thickness(0, 12, 0, 0),
                    FontSize = 15,
                    Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 190))
                }
            }
        };
    }
}
