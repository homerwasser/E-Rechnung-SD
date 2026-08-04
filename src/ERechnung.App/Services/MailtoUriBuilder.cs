namespace ERechnung.App.Services;

public static class MailtoUriBuilder
{
    public static Uri Erstelle(EmailEntwurf entwurf)
    {
        ArgumentNullException.ThrowIfNull(entwurf);

        var nachrichtMitCrLf = entwurf.Nachricht.ReplaceLineEndings("\r\n");
        var kodierterEmpfaenger = Uri
            .EscapeDataString(entwurf.Empfaenger)
            .Replace("%40", "@", StringComparison.OrdinalIgnoreCase);
        var uriText = string.Concat(
            "mailto:",
            kodierterEmpfaenger,
            "?subject=",
            Uri.EscapeDataString(entwurf.Betreff),
            "&body=",
            Uri.EscapeDataString(nachrichtMitCrLf));
        return new Uri(uriText, UriKind.Absolute);
    }
}
