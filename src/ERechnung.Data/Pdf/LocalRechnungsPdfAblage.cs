using System.Globalization;
using System.Text;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Data.Pdf;

/// <summary>
/// Speichert versionierte Rechnungs-PDFs ausschließlich unterhalb eines lokalen Basisverzeichnisses.
/// </summary>
public sealed class LocalRechnungsPdfAblage : IRechnungsPdfAblage
{
    private const int MaximaleBereinigteNummerLaenge = 80;

    private readonly string _basisVerzeichnis;
    private readonly string _basisPrefix;
    private readonly StringComparison _pfadvergleich;

    public LocalRechnungsPdfAblage(string basisVerzeichnis)
    {
        if (string.IsNullOrWhiteSpace(basisVerzeichnis))
        {
            throw new ArgumentException(
                "Das PDF-Basisverzeichnis darf nicht leer sein.",
                nameof(basisVerzeichnis));
        }

        try
        {
            _basisVerzeichnis = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(basisVerzeichnis));
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new ArgumentException(
                "Das PDF-Basisverzeichnis ist ungültig.",
                nameof(basisVerzeichnis),
                exception);
        }

        if (File.Exists(_basisVerzeichnis))
        {
            throw new ArgumentException(
                "Das PDF-Basisverzeichnis verweist auf eine Datei.",
                nameof(basisVerzeichnis));
        }

        Directory.CreateDirectory(_basisVerzeichnis);
        StelleSicherDassKeineReparsePointsVerwendetWerden(_basisVerzeichnis);

        _basisPrefix = EndetMitVerzeichnistrenner(_basisVerzeichnis)
            ? _basisVerzeichnis
            : _basisVerzeichnis + Path.DirectorySeparatorChar;
        _pfadvergleich = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    public async Task<string> SpeichereAsync(
        Rechnung rechnung,
        ReadOnlyMemory<byte> pdfInhalt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rechnung);

        if (rechnung.Id is null or <= 0)
        {
            throw new InvalidOperationException(
                "Nur eine gespeicherte Rechnung mit gültiger ID kann als PDF abgelegt werden.");
        }

        if (string.IsNullOrWhiteSpace(rechnung.Nummer))
        {
            throw new InvalidOperationException(
                "Nur eine gespeicherte Rechnung mit Rechnungsnummer kann als PDF abgelegt werden.");
        }

        if (pdfInhalt.Length < 5 || !pdfInhalt.Span[..5].SequenceEqual("%PDF-"u8))
        {
            throw new ArgumentException(
                "Der Inhalt besitzt keine gültige PDF-Signatur (%PDF-).",
                nameof(pdfInhalt));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var jahresordner = rechnung.Rechnungsdatum.Year.ToString(
            "D4",
            CultureInfo.InvariantCulture);
        var dateiname = string.Concat(
            "rechnung-",
            rechnung.Id.Value.ToString(CultureInfo.InvariantCulture),
            "-",
            BereinigeRechnungsnummer(rechnung.Nummer),
            "-",
            rechnung.GeaendertAm.Ticks.ToString(CultureInfo.InvariantCulture),
            "-",
            Guid.NewGuid().ToString("N"),
            ".pdf");
        var relativerPfad = string.Concat(jahresordner, "/", dateiname);
        var zielpfad = LoeseVollstaendigenPfadAuf(relativerPfad);
        var zielordner = Path.GetDirectoryName(zielpfad)
            ?? throw new InvalidOperationException("Der PDF-Zielordner konnte nicht bestimmt werden.");

        Directory.CreateDirectory(zielordner);
        StelleSicherDassKeineReparsePointsVerwendetWerden(zielpfad);

        var temporaererPfad = Path.Combine(
            zielordner,
            string.Concat(".", dateiname, ".", Guid.NewGuid().ToString("N"), ".tmp"));
        var temporaereDateiVorhanden = false;

        try
        {
            await using (var stream = new FileStream(
                temporaererPfad,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous | FileOptions.WriteThrough
                }))
            {
                temporaereDateiVorhanden = true;
                await stream.WriteAsync(pdfInhalt, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaererPfad, zielpfad, overwrite: true);
            temporaereDateiVorhanden = false;
            return relativerPfad;
        }
        catch
        {
            if (temporaereDateiVorhanden)
            {
                LoescheTemporaereDateiBestmoeglich(temporaererPfad);
            }

            throw;
        }
    }

    public bool Existiert(string relativerPfad)
    {
        return File.Exists(LoeseVollstaendigenPfadAuf(relativerPfad));
    }

    public string LoeseVollstaendigenPfadAuf(string relativerPfad)
    {
        if (string.IsNullOrWhiteSpace(relativerPfad))
        {
            throw new ArgumentException(
                "Der relative PDF-Pfad darf nicht leer sein.",
                nameof(relativerPfad));
        }

        if (Path.IsPathRooted(relativerPfad)
            || relativerPfad.StartsWith(Path.DirectorySeparatorChar)
            || relativerPfad.StartsWith(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException(
                "Der PDF-Pfad muss relativ zum verwalteten Basisverzeichnis sein.",
                nameof(relativerPfad));
        }

        var segmente = relativerPfad.Split(
            ['/', '\\'],
            StringSplitOptions.None);
        if (segmente.Any(IstUngueltigesPfadsegment))
        {
            throw new ArgumentException(
                "Der relative PDF-Pfad enthält ein unzulässiges Pfadsegment.",
                nameof(relativerPfad));
        }

        string vollstaendigerPfad;
        try
        {
            var normalisierterPfad = string.Join(Path.DirectorySeparatorChar, segmente);
            vollstaendigerPfad = Path.GetFullPath(
                Path.Combine(_basisVerzeichnis, normalisierterPfad));
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new ArgumentException(
                "Der relative PDF-Pfad ist ungültig.",
                nameof(relativerPfad),
                exception);
        }

        if (!vollstaendigerPfad.StartsWith(_basisPrefix, _pfadvergleich))
        {
            throw new ArgumentException(
                "Der PDF-Pfad liegt außerhalb des verwalteten Basisverzeichnisses.",
                nameof(relativerPfad));
        }

        StelleSicherDassKeineReparsePointsVerwendetWerden(vollstaendigerPfad);
        return vollstaendigerPfad;
    }

    public Task LoescheAsync(string relativerPfad, CancellationToken cancellationToken)
    {
        var vollstaendigerPfad = LoeseVollstaendigenPfadAuf(relativerPfad);
        cancellationToken.ThrowIfCancellationRequested();
        File.Delete(vollstaendigerPfad);
        return Task.CompletedTask;
    }

    private void StelleSicherDassKeineReparsePointsVerwendetWerden(string vollstaendigerPfad)
    {
        var wurzelPfad = Path.GetPathRoot(_basisVerzeichnis)
            ?? throw new InvalidOperationException("Die Wurzel der PDF-Ablage konnte nicht bestimmt werden.");
        var aktuellerPfad = wurzelPfad;
        PruefeBestehendenPfad(aktuellerPfad);

        var relativerPfad = Path.GetRelativePath(wurzelPfad, vollstaendigerPfad);
        foreach (var segment in relativerPfad.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            aktuellerPfad = Path.Combine(aktuellerPfad, segment);
            PruefeBestehendenPfad(aktuellerPfad);
        }
    }

    private static void PruefeBestehendenPfad(string pfad)
    {
        if (!Directory.Exists(pfad) && !File.Exists(pfad))
        {
            return;
        }

        if ((File.GetAttributes(pfad) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                "Die PDF-Ablage darf keine symbolischen Links oder Verzeichnisverknüpfungen enthalten.");
        }
    }

    private static string BereinigeRechnungsnummer(string rechnungsnummer)
    {
        var bereinigt = new StringBuilder(
            Math.Min(rechnungsnummer.Length, MaximaleBereinigteNummerLaenge));
        var letztesZeichenWarTrenner = false;

        foreach (var zeichen in rechnungsnummer.Trim())
        {
            if (char.IsAsciiLetterOrDigit(zeichen))
            {
                bereinigt.Append(zeichen);
                letztesZeichenWarTrenner = false;
            }
            else if (!letztesZeichenWarTrenner && bereinigt.Length > 0)
            {
                bereinigt.Append('-');
                letztesZeichenWarTrenner = true;
            }

            if (bereinigt.Length >= MaximaleBereinigteNummerLaenge)
            {
                break;
            }
        }

        var ergebnis = bereinigt.ToString().TrimEnd('-');
        return ergebnis.Length > 0 ? ergebnis : "ohne-nummer";
    }

    private static bool IstUngueltigesPfadsegment(string segment)
    {
        return segment.Length == 0
               || segment is "." or ".."
               || segment.Contains(':')
               || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
    }

    private static bool EndetMitVerzeichnistrenner(string pfad)
    {
        return pfad.EndsWith(Path.DirectorySeparatorChar)
               || pfad.EndsWith(Path.AltDirectorySeparatorChar);
    }

    private static void LoescheTemporaereDateiBestmoeglich(string temporaererPfad)
    {
        try
        {
            File.Delete(temporaererPfad);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            // Ein Aufräumfehler darf den ursprünglichen Schreibfehler nicht verdecken.
        }
    }
}
