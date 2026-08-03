using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

public static class RechnungValidator
{
    private const int MaximaleNummerLaenge = 50;
    private const int MaximaleTitelLaenge = 200;
    private const int MaximaleBemerkungLaenge = 2_000;
    private const int MaximaleBeschreibungsLaenge = 500;
    private const int MaximaleEinheitLaenge = 20;

    private static readonly DateTime FruehestesRechnungsdatum = new(1900, 1, 1);

    public static IReadOnlyList<string> Validate(
        Rechnung rechnung,
        bool rechnungsnummerErforderlich = true)
    {
        ArgumentNullException.ThrowIfNull(rechnung);
        var errors = new List<string>();

        ValidateNummer(rechnung, rechnungsnummerErforderlich, errors);

        if (rechnung.Rechnungsdatum.Date < FruehestesRechnungsdatum)
        {
            errors.Add("Das Rechnungsdatum ist ungültig.");
        }

        if (rechnung.KundeId is null or <= 0)
        {
            errors.Add("Ein Kunde ist erforderlich.");
        }

        if (rechnung.FirmaProfilId is null or <= 0)
        {
            errors.Add("Ein Firmenprofil ist erforderlich.");
        }

        if (rechnung.Faeligkeitsdatum?.Date < rechnung.Rechnungsdatum.Date)
        {
            errors.Add("Das Fälligkeitsdatum darf nicht vor dem Rechnungsdatum liegen.");
        }

        if (!RechnungsStatus.IsValid(rechnung.Status))
        {
            errors.Add("Der Rechnungsstatus ist ungültig.");
        }

        if (!IstDreistelligerBuchstabencode(rechnung.Waehrung))
        {
            errors.Add("Die Währung muss aus genau drei Buchstaben bestehen.");
        }

        ValidateTextlaengen(rechnung, errors);
        ValidatePositionen(rechnung.Positionen, errors);

        return errors;
    }

    private static void ValidateNummer(
        Rechnung rechnung,
        bool rechnungsnummerErforderlich,
        ICollection<string> errors)
    {
        if (rechnungsnummerErforderlich && string.IsNullOrWhiteSpace(rechnung.Nummer))
        {
            errors.Add("Die Rechnungsnummer ist erforderlich.");
        }
        else if (!string.IsNullOrWhiteSpace(rechnung.Nummer)
                 && rechnung.Nummer.Trim().Length > MaximaleNummerLaenge)
        {
            errors.Add($"Die Rechnungsnummer darf höchstens {MaximaleNummerLaenge} Zeichen lang sein.");
        }
    }

    private static void ValidateTextlaengen(Rechnung rechnung, ICollection<string> errors)
    {
        if ((rechnung.Titel?.Length ?? 0) > MaximaleTitelLaenge)
        {
            errors.Add($"Der Rechnungstitel darf höchstens {MaximaleTitelLaenge} Zeichen lang sein.");
        }

        if ((rechnung.Bemerkung?.Length ?? 0) > MaximaleBemerkungLaenge)
        {
            errors.Add($"Die Bemerkung darf höchstens {MaximaleBemerkungLaenge} Zeichen lang sein.");
        }
    }

    private static void ValidatePositionen(
        IReadOnlyList<RechnungsPosition>? positionen,
        ICollection<string> errors)
    {
        if (positionen is null || positionen.Count == 0)
        {
            errors.Add("Die Rechnung muss mindestens eine Position enthalten.");
            return;
        }

        for (var index = 0; index < positionen.Count; index++)
        {
            var position = positionen[index];
            var bezeichnung = $"Position {index + 1}";

            if (position is null)
            {
                errors.Add($"{bezeichnung} ist ungültig.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(position.Beschreibung))
            {
                errors.Add($"{bezeichnung}: Die Beschreibung ist erforderlich.");
            }
            else if (position.Beschreibung.Trim().Length > MaximaleBeschreibungsLaenge)
            {
                errors.Add(
                    $"{bezeichnung}: Die Beschreibung darf höchstens {MaximaleBeschreibungsLaenge} Zeichen lang sein.");
            }

            if (position.Menge <= 0m)
            {
                errors.Add($"{bezeichnung}: Die Menge muss größer als 0 sein.");
            }

            if (position.EinzelpreisNetto < 0m)
            {
                errors.Add($"{bezeichnung}: Der Nettopreis darf nicht negativ sein.");
            }

            if (position.Steuersatz is < 0m or > 100m)
            {
                errors.Add($"{bezeichnung}: Der Steuersatz muss zwischen 0 und 100 Prozent liegen.");
            }

            if ((position.Einheit?.Length ?? 0) > MaximaleEinheitLaenge)
            {
                errors.Add(
                    $"{bezeichnung}: Die Einheit darf höchstens {MaximaleEinheitLaenge} Zeichen lang sein.");
            }
        }
    }

    private static bool IstDreistelligerBuchstabencode(string? wert)
    {
        return wert is { Length: 3 }
               && wert.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
    }
}
