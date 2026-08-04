using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

/// <summary>
/// Erzeugt eine PDF für den zuletzt gelesenen Rechnungsstand und verknüpft sie atomar auf Datenbankebene.
/// </summary>
public sealed class RechnungsPdfService
{
    private const int MinimalePdfLaenge = 5;

    private readonly IRechnungRepository _repository;
    private readonly IRechnungsPdfGenerator _generator;
    private readonly IRechnungsPdfAblage _ablage;
    private readonly TimeProvider _timeProvider;

    public RechnungsPdfService(
        IRechnungRepository repository,
        IRechnungsPdfGenerator generator,
        IRechnungsPdfAblage ablage,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(ablage);

        _repository = repository;
        _generator = generator;
        _ablage = ablage;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Rechnung> ErzeugeAsync(
        int rechnungId,
        CancellationToken cancellationToken = default)
    {
        if (rechnungId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rechnungId),
                "Die Rechnungs-ID muss größer als 0 sein.");
        }

        var rechnung = await _repository.GetByIdAsync(rechnungId)
            ?? throw new KeyNotFoundException(
                $"Die Rechnung mit der ID {rechnungId} wurde nicht gefunden.");

        StelleGespeichertesAggregatSicher(rechnung, rechnungId);

        var rechnungsstandAm = rechnung.GeaendertAm;
        var vorherigeVerknuepfung = rechnung.PdfVerknuepfung;
        var erzeugtAm = _timeProvider.GetUtcNow().ToUniversalTime();
        var pdfInhalt = _generator.Erzeuge(rechnung, erzeugtAm);

        if (pdfInhalt is null || pdfInhalt.Length < MinimalePdfLaenge)
        {
            throw new InvalidOperationException("Der PDF-Generator hat keine gültige PDF-Ausgabe erzeugt.");
        }

        string? neuerRelativerPfad = null;
        RechnungsPdfVerknuepfung neueVerknuepfung;

        try
        {
            neuerRelativerPfad = await _ablage.SpeichereAsync(
                rechnung,
                pdfInhalt,
                cancellationToken);
            neueVerknuepfung = new RechnungsPdfVerknuepfung(
                neuerRelativerPfad,
                erzeugtAm.UtcDateTime,
                rechnungsstandAm);

            await _repository.SetPdfVerknuepfungAsync(
                rechnung.Id!.Value,
                neueVerknuepfung,
                rechnungsstandAm,
                vorherigeVerknuepfung);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(neuerRelativerPfad))
            {
                await LoescheBestmoeglichAsync(neuerRelativerPfad);
            }

            throw;
        }

        rechnung.PdfVerknuepfung = neueVerknuepfung;

        if (vorherigeVerknuepfung is not null
            && !string.Equals(
                vorherigeVerknuepfung.RelativerPfad,
                neueVerknuepfung.RelativerPfad,
                StringComparison.Ordinal))
        {
            await LoescheBestmoeglichAsync(vorherigeVerknuepfung.RelativerPfad);
        }

        return rechnung;
    }

    private static void StelleGespeichertesAggregatSicher(Rechnung rechnung, int erwarteteId)
    {
        if (rechnung.Id is null or <= 0 || rechnung.Id.Value != erwarteteId)
        {
            throw new InvalidOperationException("Die gespeicherte Rechnung besitzt keine gültige ID.");
        }

        if (string.IsNullOrWhiteSpace(rechnung.Nummer))
        {
            throw new InvalidOperationException("Die gespeicherte Rechnung besitzt keine Rechnungsnummer.");
        }

        if (rechnung.EmpfaengerSnapshot is null || rechnung.AbsenderSnapshot is null)
        {
            throw new InvalidOperationException("Die gespeicherte Rechnung besitzt nicht alle erforderlichen Snapshots.");
        }

        if (rechnung.Positionen is null
            || rechnung.Positionen.Count == 0
            || rechnung.Positionen.All(position => position is null))
        {
            throw new InvalidOperationException("Die gespeicherte Rechnung besitzt keine Rechnungsposition.");
        }
    }

    private async Task LoescheBestmoeglichAsync(string relativerPfad)
    {
        try
        {
            await _ablage.LoescheAsync(relativerPfad, CancellationToken.None);
        }
        catch (Exception)
        {
            // Aufräumfehler dürfen den ursprünglichen Vorgang nicht verdecken.
        }
    }
}
