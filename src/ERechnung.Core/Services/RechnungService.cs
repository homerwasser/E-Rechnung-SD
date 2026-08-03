using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

public sealed class RechnungService
{
    private readonly IRechnungRepository _repository;

    public RechnungService(IRechnungRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Task<IReadOnlyList<RechnungsUebersicht>> GetAllAsync(string? status = null)
    {
        if (status is not null && !RechnungsStatus.IsValid(status))
        {
            throw new ArgumentException("Der Rechnungsstatus ist ungültig.", nameof(status));
        }

        return _repository.GetAllAsync(status);
    }

    public Task<Rechnung?> GetByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    public async Task<Rechnung> SpeichernAsync(Rechnung rechnung)
    {
        ArgumentNullException.ThrowIfNull(rechnung);

        RechnungCalculator.Berechnen(rechnung);
        var istNeueRechnung = rechnung.Id is null;
        var snapshotErrors = AktualisiereSnapshots(rechnung, istNeueRechnung);
        var validationErrors = RechnungValidator.Validate(
            rechnung,
            rechnungsnummerErforderlich: !istNeueRechnung);
        var errors = validationErrors.Concat(snapshotErrors).ToArray();

        if (errors.Length > 0)
        {
            throw new RechnungValidationException(errors);
        }

        if (istNeueRechnung)
        {
            var jetzt = DateTime.UtcNow;
            if (rechnung.ErstelltAm == default)
            {
                rechnung.ErstelltAm = jetzt;
            }

            rechnung.GeaendertAm = jetzt;
            return await _repository.CreateAsync(rechnung);
        }

        await _repository.UpdateAsync(rechnung);
        return rechnung;
    }

    public Task DeleteAsync(int id)
    {
        return _repository.DeleteAsync(id);
    }

    public async Task<Rechnung> StatusAendernAsync(int id, string status)
    {
        if (!RechnungsStatus.IsValid(status))
        {
            throw new RechnungValidationException(["Der Rechnungsstatus ist ungültig."]);
        }

        var rechnung = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Die Rechnung mit der ID {id} wurde nicht gefunden.");

        rechnung.Status = status;
        return await SpeichernAsync(rechnung);
    }

    private static IReadOnlyList<string> AktualisiereSnapshots(
        Rechnung rechnung,
        bool istNeueRechnung)
    {
        var errors = new List<string>();
        var empfaengerMussAktualisiertWerden = istNeueRechnung
            || rechnung.KundeId is > 0
            && (rechnung.EmpfaengerSnapshot is null
                || rechnung.EmpfaengerSnapshot.QuellId != rechnung.KundeId.Value);

        if (rechnung.Kunde is not null && rechnung.Kunde.Id != rechnung.KundeId)
        {
            errors.Add(
                "Die ID der Kundenstammdaten stimmt nicht mit der ausgewählten Kunden-ID überein.");
        }
        else if (empfaengerMussAktualisiertWerden)
        {
            if (rechnung.Kunde is null)
            {
                errors.Add(
                    "Die Stammdaten des ausgewählten Kunden fehlen für den Rechnungsempfänger-Snapshot.");
            }
            else if (rechnung.KundeId is > 0)
            {
                rechnung.EmpfaengerSnapshot = ErstelleEmpfaengerSnapshot(
                    rechnung.KundeId.Value,
                    rechnung.Kunde);
            }
        }

        var absenderMussAktualisiertWerden = istNeueRechnung
            || rechnung.FirmaProfilId is > 0
            && (rechnung.AbsenderSnapshot is null
                || rechnung.AbsenderSnapshot.QuellId != rechnung.FirmaProfilId.Value);

        if (rechnung.Absender is not null && rechnung.Absender.Id != rechnung.FirmaProfilId)
        {
            errors.Add(
                "Die ID der Firmenprofil-Stammdaten stimmt nicht mit der ausgewählten Firmenprofil-ID überein.");
        }
        else if (absenderMussAktualisiertWerden)
        {
            if (rechnung.Absender is null)
            {
                errors.Add(
                    "Die Stammdaten des ausgewählten Firmenprofils fehlen für den Rechnungsabsender-Snapshot.");
            }
            else if (rechnung.FirmaProfilId is > 0)
            {
                rechnung.AbsenderSnapshot = ErstelleAbsenderSnapshot(
                    rechnung.FirmaProfilId.Value,
                    rechnung.Absender);
            }
        }

        return errors;
    }

    private static RechnungsEmpfaengerSnapshot ErstelleEmpfaengerSnapshot(
        int quellId,
        Kunde kunde)
    {
        return new RechnungsEmpfaengerSnapshot
        {
            QuellId = quellId,
            Name = kunde.Firmenname,
            Ansprechpartner = kunde.Ansprechpartner,
            Strasse = kunde.Strasse,
            PLZ = kunde.PLZ,
            Ort = kunde.Ort,
            Land = kunde.Land,
            Email = kunde.Email,
            UstIdNr = kunde.UstIdNr
        };
    }

    private static RechnungsAbsenderSnapshot ErstelleAbsenderSnapshot(
        int quellId,
        FirmaProfil firmaProfil)
    {
        return new RechnungsAbsenderSnapshot
        {
            QuellId = quellId,
            Name = firmaProfil.Name,
            LogoPfad = firmaProfil.LogoPfad,
            Ansprechpartner = firmaProfil.Ansprechpartner,
            Strasse = firmaProfil.Strasse,
            PLZ = firmaProfil.PLZ,
            Ort = firmaProfil.Ort,
            Land = firmaProfil.Land,
            Email = firmaProfil.Email,
            Telefon = firmaProfil.Telefon,
            UstIdNr = firmaProfil.UstIdNr,
            IBAN = firmaProfil.IBAN,
            BIC = firmaProfil.BIC
        };
    }
}
