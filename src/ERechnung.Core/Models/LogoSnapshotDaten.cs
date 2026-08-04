namespace ERechnung.Core.Models;

/// <summary>
/// Validierte Bilddaten für einen unveränderlichen Rechnungsabsender-Snapshot.
/// </summary>
public sealed class LogoSnapshotDaten
{
    private static readonly HashSet<string> UnterstuetzteMedientypen = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/bmp"
    };

    private readonly byte[] _inhalt;

    public LogoSnapshotDaten(byte[] inhalt, string medientyp)
    {
        ArgumentNullException.ThrowIfNull(inhalt);

        if (inhalt.Length == 0)
        {
            throw new ArgumentException("Der Logo-Inhalt darf nicht leer sein.", nameof(inhalt));
        }

        if (string.IsNullOrWhiteSpace(medientyp)
            || !UnterstuetzteMedientypen.Contains(medientyp.Trim()))
        {
            throw new ArgumentException("Der Logo-Medientyp wird nicht unterstützt.", nameof(medientyp));
        }

        _inhalt = inhalt.ToArray();
        Medientyp = medientyp.Trim().ToLowerInvariant();
    }

    public byte[] Inhalt => _inhalt.ToArray();
    public string Medientyp { get; }

    public static bool IstUnterstuetzterMedientyp(string? medientyp)
    {
        return !string.IsNullOrWhiteSpace(medientyp)
               && UnterstuetzteMedientypen.Contains(medientyp.Trim());
    }
}
