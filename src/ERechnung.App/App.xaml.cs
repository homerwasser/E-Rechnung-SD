using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using ERechnung.App.Services;
using ERechnung.App.ViewModels;
using ERechnung.Core.Services;
using ERechnung.Data;
using ERechnung.Data.Migrations;
using ERechnung.Data.Pdf;
using ERechnung.Data.Repositories;
using ERechnung.PDF;
using ERechnung.PDF.Generators;

namespace ERechnung.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            QuestPdfLizenz.KonfiguriereCommunity();
            DbConnectionHelper.Initialize();
            await DatabaseMigrator.MigrateAsync(DbConnectionHelper.ConnectionString);

            var dialogService = new MessageBoxDialogService();
            var kundeRepository = new KundeRepository(DbConnectionHelper.ConnectionString);
            var firmaProfilRepository = new FirmaProfilRepository(DbConnectionHelper.ConnectionString);
            var rechnungRepository = new RechnungRepository(DbConnectionHelper.ConnectionString);
            var pdfAblage = new LocalRechnungsPdfAblage(DbConnectionHelper.PdfBasePath);
            var rechnungService = new RechnungService(
                rechnungRepository,
                new LocalLogoSnapshotLoader(),
                pdfAblage);
            var rechnungsPdfService = new RechnungsPdfService(
                rechnungRepository,
                new QuestPdfRechnungsPdfGenerator(),
                pdfAblage);
            var dateiOeffner = new WindowsDateiOeffner();
            var emailEntwurfComposer = new EmailEntwurfComposer();
            var emailEntwurfService = new WindowsEmailEntwurfService();

            var kundenViewModel = new KundenViewModel(kundeRepository, dialogService);
            var firmenprofileViewModel = new FirmenprofileViewModel(
                firmaProfilRepository,
                dialogService);
            var rechnungsUebersichtViewModel = new RechnungsUebersichtViewModel(
                rechnungService,
                rechnungsPdfService,
                pdfAblage,
                dateiOeffner,
                emailEntwurfComposer,
                emailEntwurfService,
                dialogService);
            var rechnungsEditorViewModel = new RechnungsEditorViewModel(
                kundeRepository,
                firmaProfilRepository,
                rechnungService,
                dialogService);
            var mainWindowViewModel = new MainWindowViewModel(
                rechnungsUebersichtViewModel,
                rechnungsEditorViewModel,
                kundenViewModel,
                firmenprofileViewModel,
                dialogService);

            await mainWindowViewModel.InitialisierenAsync();

            var mainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
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
