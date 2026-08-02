using System;
using System.Collections.Generic;
using System.Net.Mail;
using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

public static class KundeValidator
{
    public static IReadOnlyList<string> Validate(Kunde kunde)
    {
        ArgumentNullException.ThrowIfNull(kunde);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(kunde.Firmenname))
        {
            errors.Add("Der Firmenname ist erforderlich.");
        }
        else if (kunde.Firmenname.Trim().Length > 200)
        {
            errors.Add("Der Firmenname darf höchstens 200 Zeichen lang sein.");
        }

        if (!string.IsNullOrWhiteSpace(kunde.Email) && !MailAddress.TryCreate(kunde.Email.Trim(), out _))
        {
            errors.Add("Die E-Mail-Adresse ist ungültig.");
        }

        if (string.IsNullOrWhiteSpace(kunde.Land) || kunde.Land.Trim().Length != 2)
        {
            errors.Add("Das Land muss als zweistelliger Ländercode angegeben werden, z. B. DE.");
        }

        if (kunde.PLZ.Length > 20)
        {
            errors.Add("Die Postleitzahl darf höchstens 20 Zeichen lang sein.");
        }

        return errors;
    }
}
