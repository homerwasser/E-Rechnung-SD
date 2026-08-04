using System.Diagnostics;

namespace ERechnung.App.Services;

public sealed class WindowsMailtoEntwurfOeffner : IMailtoEntwurfOeffner
{
    private readonly IProzessStarter _prozessStarter;

    public WindowsMailtoEntwurfOeffner(IProzessStarter prozessStarter)
    {
        _prozessStarter = prozessStarter ?? throw new ArgumentNullException(nameof(prozessStarter));
    }

    public EmailOeffnungsversuch Oeffne(Uri mailtoUri)
    {
        ArgumentNullException.ThrowIfNull(mailtoUri);

        try
        {
            _prozessStarter.Starte(new ProcessStartInfo
            {
                FileName = mailtoUri.OriginalString,
                UseShellExecute = true
            });
            return EmailOeffnungsversuch.Geoeffnet;
        }
        catch (OperationCanceledException)
        {
            return EmailOeffnungsversuch.Abgebrochen;
        }
        catch (Exception)
        {
            return EmailOeffnungsversuch.Fehlgeschlagen;
        }
    }
}
