using System.Net.Mail;
using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

public static class FirmaProfilValidator
{
    public static IReadOnlyList<string> Validate(FirmaProfil firmaProfil)
    {
        ArgumentNullException.ThrowIfNull(firmaProfil);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(firmaProfil.Name))
        {
            errors.Add("Der Firmenname ist erforderlich.");
        }
        else if (firmaProfil.Name.Trim().Length > 200)
        {
            errors.Add("Der Firmenname darf höchstens 200 Zeichen lang sein.");
        }

        if (!string.IsNullOrWhiteSpace(firmaProfil.Email)
            && !MailAddress.TryCreate(firmaProfil.Email.Trim(), out _))
        {
            errors.Add("Die E-Mail-Adresse ist ungültig.");
        }

        if (!IstZweistelligerAsciiBuchstabencode(firmaProfil.Land))
        {
            errors.Add("Das Land muss als zweistelliger Ländercode angegeben werden, z. B. DE.");
        }

        if ((firmaProfil.PLZ?.Length ?? 0) > 20)
        {
            errors.Add("Die Postleitzahl darf höchstens 20 Zeichen lang sein.");
        }

        return errors;
    }

    private static bool IstZweistelligerAsciiBuchstabencode(string? wert)
    {
        return wert is { Length: 2 }
               && wert.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
    }
}
