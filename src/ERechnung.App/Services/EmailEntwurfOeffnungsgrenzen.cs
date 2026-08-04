namespace ERechnung.App.Services;

public enum EmailOeffnungsversuch
{
    Geoeffnet,
    NichtVerfuegbar,
    Abgebrochen,
    Fehlgeschlagen
}

public interface IClassicOutlookEntwurfOeffner
{
    EmailOeffnungsversuch Oeffne(EmailEntwurf entwurf);
}

public interface IMailtoEntwurfOeffner
{
    EmailOeffnungsversuch Oeffne(Uri mailtoUri);
}
