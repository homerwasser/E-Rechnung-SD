namespace ERechnung.Core.Models;

/// <summary>
/// Verknüpft eine Rechnung mit einer versioniert abgelegten PDF-Datei.
/// </summary>
public sealed class RechnungsPdfVerknuepfung
{
    public RechnungsPdfVerknuepfung(
        string relativerPfad,
        DateTime erstelltAm,
        DateTime rechnungsstandAm)
    {
        if (string.IsNullOrWhiteSpace(relativerPfad))
        {
            throw new ArgumentException("Der relative PDF-Pfad darf nicht leer sein.", nameof(relativerPfad));
        }

        if (erstelltAm.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Der PDF-Erstellungszeitpunkt muss in UTC angegeben sein.", nameof(erstelltAm));
        }

        RelativerPfad = relativerPfad;
        ErstelltAm = erstelltAm;
        RechnungsstandAm = rechnungsstandAm;
    }

    public string RelativerPfad { get; }
    public DateTime ErstelltAm { get; }
    public DateTime RechnungsstandAm { get; }
}
