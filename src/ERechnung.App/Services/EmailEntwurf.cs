using System.IO;
using System.Net.Mail;

namespace ERechnung.App.Services;

public sealed class EmailEntwurf
{
    public EmailEntwurf(
        string empfaenger,
        string betreff,
        string nachricht,
        string anhangPfad)
    {
        Empfaenger = ValidiereEmpfaenger(empfaenger);
        Betreff = ValidiereEinzeiligenPflichttext(betreff, nameof(betreff));
        Nachricht = string.IsNullOrWhiteSpace(nachricht)
            ? throw new ArgumentException("Die Nachricht darf nicht leer sein.", nameof(nachricht))
            : nachricht;
        AnhangPfad = ValidiereAnhangPfad(anhangPfad);
    }

    public string Empfaenger { get; }
    public string Betreff { get; }
    public string Nachricht { get; }
    public string AnhangPfad { get; }

    private static string ValidiereEmpfaenger(string empfaenger)
    {
        var wert = ValidiereEinzeiligenPflichttext(empfaenger, nameof(empfaenger));
        if (!MailAddress.TryCreate(wert, out var mailadresse)
            || !string.Equals(mailadresse.Address, wert, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Die Empfängeradresse ist ungültig.", nameof(empfaenger));
        }

        return wert;
    }

    private static string ValidiereEinzeiligenPflichttext(string wert, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(wert))
        {
            throw new ArgumentException("Der Wert darf nicht leer sein.", parameterName);
        }

        var getrimmt = wert.Trim();
        if (getrimmt.Contains('\r') || getrimmt.Contains('\n'))
        {
            throw new ArgumentException("Der Wert darf keinen Zeilenumbruch enthalten.", parameterName);
        }

        return getrimmt;
    }

    private static string ValidiereAnhangPfad(string anhangPfad)
    {
        if (string.IsNullOrWhiteSpace(anhangPfad) || !Path.IsPathFullyQualified(anhangPfad))
        {
            throw new ArgumentException(
                "Der PDF-Anhang muss über einen absoluten Dateipfad angegeben werden.",
                nameof(anhangPfad));
        }

        try
        {
            var vollstaendigerPfad = Path.GetFullPath(anhangPfad);
            var datei = new FileInfo(vollstaendigerPfad);
            if (!datei.Exists
                || datei.Length == 0
                || (datei.Attributes & FileAttributes.Directory) != 0)
            {
                throw new ArgumentException(
                    "Der PDF-Anhang muss auf eine vorhandene, nichtleere PDF-Datei verweisen.",
                    nameof(anhangPfad));
            }

            return vollstaendigerPfad;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ArgumentException("Der PDF-Anhang ist ungültig.", nameof(anhangPfad), exception);
        }
    }
}
