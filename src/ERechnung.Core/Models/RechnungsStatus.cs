using System.Collections.ObjectModel;

namespace ERechnung.Core.Models;

/// <summary>
/// Definiert die persistenten Rechnungsstatus und ihre Anzeigenamen.
/// </summary>
public static class RechnungsStatus
{
    public const string Erstellt = "erstellt";
    public const string Versendet = "versendet";
    public const string Offen = "offen";
    public const string Bezahlt = "bezahlt";
    public const string Inklaerung = "inklaerung";
    public const string Storniert = "storniert";

    private static readonly IReadOnlyList<string> Statuswerte = Array.AsReadOnly(
    [
        Erstellt,
        Versendet,
        Offen,
        Bezahlt,
        Inklaerung,
        Storniert
    ]);

    private static readonly IReadOnlyDictionary<string, string> StatusAnzeigenamen =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            [Erstellt] = "Erstellt",
            [Versendet] = "Versendet",
            [Offen] = "Offen",
            [Bezahlt] = "Bezahlt",
            [Inklaerung] = "In Klärung",
            [Storniert] = "Storniert"
        });

    public static IReadOnlyList<string> Alle => Statuswerte;

    public static IReadOnlyDictionary<string, string> Anzeigenamen => StatusAnzeigenamen;

    public static bool IsValid(string? status)
    {
        return status is not null && StatusAnzeigenamen.ContainsKey(status);
    }

    public static string GetAnzeigename(string status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (!StatusAnzeigenamen.TryGetValue(status, out var anzeigename))
        {
            throw new ArgumentException("Der Rechnungsstatus ist ungültig.", nameof(status));
        }

        return anzeigename;
    }
}
