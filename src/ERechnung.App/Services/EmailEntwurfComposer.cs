using ERechnung.Core.Models;

namespace ERechnung.App.Services;

public sealed class EmailEntwurfComposer
{
    public EmailEntwurf Erstelle(Rechnung rechnung, string anhangPfad)
    {
        ArgumentNullException.ThrowIfNull(rechnung);

        if (rechnung.Id is null or <= 0)
        {
            throw new ArgumentException("Die Rechnung muss vor dem E-Mail-Entwurf gespeichert sein.", nameof(rechnung));
        }

        if (string.IsNullOrWhiteSpace(rechnung.Nummer))
        {
            throw new ArgumentException("Die gespeicherte Rechnungsnummer fehlt.", nameof(rechnung));
        }

        var empfaenger = rechnung.EmpfaengerSnapshot
            ?? throw new ArgumentException("Der gespeicherte Empfänger-Snapshot fehlt.", nameof(rechnung));
        var absender = rechnung.AbsenderSnapshot
            ?? throw new ArgumentException("Der gespeicherte Absender-Snapshot fehlt.", nameof(rechnung));

        if (string.IsNullOrWhiteSpace(empfaenger.Email))
        {
            throw new ArgumentException("Im Empfänger-Snapshot fehlt die E-Mail-Adresse.", nameof(rechnung));
        }

        if (string.IsNullOrWhiteSpace(absender.Name))
        {
            throw new ArgumentException("Im Absender-Snapshot fehlt der Name.", nameof(rechnung));
        }

        var nummer = rechnung.Nummer.Trim();
        var absenderName = absender.Name.Trim();
        var betreff = $"Rechnung {nummer} – {absenderName}";
        var nachricht = string.Join(
            "\r\n",
            "Guten Tag,",
            string.Empty,
            $"anbei erhalten Sie die Rechnung {nummer} als PDF.",
            string.Empty,
            "Mit freundlichen Grüßen",
            absenderName);

        return new EmailEntwurf(empfaenger.Email, betreff, nachricht, anhangPfad);
    }
}
