using System.Diagnostics;
using System.IO;

namespace ERechnung.App.Services;

public sealed class WindowsDateiOeffner : IDateiOeffner
{
    private readonly IProzessStarter _prozessStarter;

    public WindowsDateiOeffner()
        : this(new SystemProzessStarter())
    {
    }

    public WindowsDateiOeffner(IProzessStarter prozessStarter)
    {
        _prozessStarter = prozessStarter ?? throw new ArgumentNullException(nameof(prozessStarter));
    }

    public void Oeffne(string dateiPfad)
    {
        var vollstaendigerPfad = ValidiereDateiPfad(dateiPfad);
        _prozessStarter.Starte(new ProcessStartInfo
        {
            FileName = vollstaendigerPfad,
            UseShellExecute = true
        });
    }

    public void ImExplorerAnzeigen(string dateiPfad)
    {
        var vollstaendigerPfad = ValidiereDateiPfad(dateiPfad);
        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true
        };
        startInfo.ArgumentList.Add("/select,");
        startInfo.ArgumentList.Add(vollstaendigerPfad);
        _prozessStarter.Starte(startInfo);
    }

    private static string ValidiereDateiPfad(string dateiPfad)
    {
        if (string.IsNullOrWhiteSpace(dateiPfad) || !Path.IsPathFullyQualified(dateiPfad))
        {
            throw new ArgumentException("Der Dateipfad muss absolut sein.", nameof(dateiPfad));
        }

        var vollstaendigerPfad = Path.GetFullPath(dateiPfad);
        if (!File.Exists(vollstaendigerPfad))
        {
            throw new FileNotFoundException("Die Datei wurde nicht gefunden.", vollstaendigerPfad);
        }

        return vollstaendigerPfad;
    }
}
