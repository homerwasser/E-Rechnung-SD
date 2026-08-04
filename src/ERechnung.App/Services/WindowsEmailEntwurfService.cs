namespace ERechnung.App.Services;

public sealed class WindowsEmailEntwurfService : IEmailEntwurfService
{
    private readonly IClassicOutlookEntwurfOeffner _classicOutlookOeffner;
    private readonly IMailtoEntwurfOeffner _mailtoOeffner;

    public WindowsEmailEntwurfService()
        : this(
            new WindowsClassicOutlookEntwurfOeffner(),
            new WindowsMailtoEntwurfOeffner(new SystemProzessStarter()))
    {
    }

    public WindowsEmailEntwurfService(
        IClassicOutlookEntwurfOeffner classicOutlookOeffner,
        IMailtoEntwurfOeffner mailtoOeffner)
    {
        _classicOutlookOeffner = classicOutlookOeffner
            ?? throw new ArgumentNullException(nameof(classicOutlookOeffner));
        _mailtoOeffner = mailtoOeffner ?? throw new ArgumentNullException(nameof(mailtoOeffner));
    }

    public EmailEntwurfErgebnis Oeffne(EmailEntwurf entwurf)
    {
        ArgumentNullException.ThrowIfNull(entwurf);

        EmailOeffnungsversuch classicErgebnis;
        try
        {
            classicErgebnis = _classicOutlookOeffner.Oeffne(entwurf);
        }
        catch (OperationCanceledException)
        {
            return new EmailEntwurfErgebnis(EmailEntwurfErgebnisStatus.Abgebrochen);
        }
        catch (Exception)
        {
            classicErgebnis = EmailOeffnungsversuch.Fehlgeschlagen;
        }

        if (classicErgebnis == EmailOeffnungsversuch.Geoeffnet)
        {
            return new EmailEntwurfErgebnis(EmailEntwurfErgebnisStatus.MitAnhangGeoeffnet);
        }

        if (classicErgebnis == EmailOeffnungsversuch.Abgebrochen)
        {
            return new EmailEntwurfErgebnis(EmailEntwurfErgebnisStatus.Abgebrochen);
        }

        try
        {
            var mailtoErgebnis = _mailtoOeffner.Oeffne(MailtoUriBuilder.Erstelle(entwurf));
            return mailtoErgebnis switch
            {
                EmailOeffnungsversuch.Geoeffnet => new EmailEntwurfErgebnis(
                    EmailEntwurfErgebnisStatus.OhneAnhangAngefordert),
                EmailOeffnungsversuch.Abgebrochen => new EmailEntwurfErgebnis(
                    EmailEntwurfErgebnisStatus.Abgebrochen),
                _ => new EmailEntwurfErgebnis(
                    EmailEntwurfErgebnisStatus.Fehlgeschlagen,
                    "Es konnte weder klassisches Outlook noch der Standard-E-Mail-Client geöffnet werden.")
            };
        }
        catch (OperationCanceledException)
        {
            return new EmailEntwurfErgebnis(EmailEntwurfErgebnisStatus.Abgebrochen);
        }
        catch (Exception)
        {
            return new EmailEntwurfErgebnis(
                EmailEntwurfErgebnisStatus.Fehlgeschlagen,
                "Es konnte weder klassisches Outlook noch der Standard-E-Mail-Client geöffnet werden.");
        }
    }
}
