using System;
using System.Threading.Tasks;
using System.Windows;
using ERechnung.Data;
using ERechnung.Data.Migrations;
using ERechnung.Data.Repositories;
using ERechnung.Core.Services;
using ERechnung.Core.Models;

namespace ERechnung.App;

public partial class App : Application
{
    public static IRepository<Kunde> KundeRepo { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            // 1. DB-Pfad initialisieren
            DbConnectionHelper.Initialize();

            // 2. Tabellen erstellen (Migration)
            await InitialMigration.AusfuehrenAsync(DbConnectionHelper.ConnectionString);

            // 3. Repository fuer globale Nutzung bereitlegen
            KundeRepo = new KundeRepository(DbConnectionHelper.ConnectionString);

            // 4. Hauptfenster oeffnen
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Start:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}