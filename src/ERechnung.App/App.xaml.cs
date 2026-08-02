using System;
using System.Windows;
using ERechnung.App.Services;
using ERechnung.App.ViewModels;
using ERechnung.Data;
using ERechnung.Data.Migrations;
using ERechnung.Data.Repositories;

namespace ERechnung.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            DbConnectionHelper.Initialize();
            await DatabaseMigrator.MigrateAsync(DbConnectionHelper.ConnectionString);

            var kundeRepository = new KundeRepository(DbConnectionHelper.ConnectionString);
            var kundenViewModel = new KundenViewModel(kundeRepository, new MessageBoxDialogService());
            await kundenViewModel.InitialisierenAsync();

            var mainWindow = new MainWindow(kundenViewModel);
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Die Anwendung konnte nicht gestartet werden.\n\n{ex.Message}\n\nDatenbank: {DbConnectionHelper.DatabasePath}",
                "Startfehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
