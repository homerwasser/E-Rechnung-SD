using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

/// <summary>
/// Lädt validierte Logo-Daten für einen Rechnungssnapshot.
/// </summary>
/// <remarks>
/// Fehlende, ungültige oder zu große Dateien werden als <see langword="null"/>
/// zurückgegeben und nicht als ungefilterte Ladefehler weitergereicht.
/// </remarks>
public interface ILogoSnapshotLoader
{
    LogoSnapshotDaten? Lade(string logoPfad);
}
