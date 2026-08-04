using System.Runtime.InteropServices;

namespace ERechnung.App.Services;

public sealed class WindowsClassicOutlookEntwurfOeffner : IClassicOutlookEntwurfOeffner
{
    private const string OutlookProgId = "Outlook.Application";
    private const int OutlookMailItem = 0;

    public EmailOeffnungsversuch Oeffne(EmailEntwurf entwurf)
    {
        ArgumentNullException.ThrowIfNull(entwurf);

        if (!OperatingSystem.IsWindows())
        {
            return EmailOeffnungsversuch.NichtVerfuegbar;
        }

        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            return EmailOeffnungsversuch.Fehlgeschlagen;
        }

        object? outlook = null;
        object? mail = null;
        object? anhaenge = null;

        try
        {
            var outlookTyp = Type.GetTypeFromProgID(OutlookProgId, throwOnError: false);
            if (outlookTyp is null)
            {
                return EmailOeffnungsversuch.NichtVerfuegbar;
            }

            outlook = Activator.CreateInstance(outlookTyp);
            if (outlook is null)
            {
                return EmailOeffnungsversuch.NichtVerfuegbar;
            }

            dynamic outlookAutomation = outlook;
            mail = outlookAutomation.CreateItem(OutlookMailItem);
            if (mail is null)
            {
                return EmailOeffnungsversuch.Fehlgeschlagen;
            }

            dynamic mailAutomation = mail;
            mailAutomation.To = entwurf.Empfaenger;
            mailAutomation.Subject = entwurf.Betreff;
            mailAutomation.Body = entwurf.Nachricht;
            anhaenge = mailAutomation.Attachments;
            dynamic anhangAutomation = anhaenge;
            anhangAutomation.Add(entwurf.AnhangPfad);
            mailAutomation.Display(false);
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
        finally
        {
            GibComObjektFrei(anhaenge);
            GibComObjektFrei(mail);
            GibComObjektFrei(outlook);
        }
    }

    private static void GibComObjektFrei(object? comObjekt)
    {
        if (comObjekt is null || !Marshal.IsComObject(comObjekt))
        {
            return;
        }

        try
        {
            Marshal.FinalReleaseComObject(comObjekt);
        }
        catch (Exception)
        {
            // Das Freigeben darf das bereits geöffnete Entwurfsfenster nicht beeinträchtigen.
        }
    }
}
