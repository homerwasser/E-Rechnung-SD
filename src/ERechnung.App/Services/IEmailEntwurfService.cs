namespace ERechnung.App.Services;

public enum EmailEntwurfErgebnisStatus
{
    MitAnhangGeoeffnet,
    OhneAnhangAngefordert,
    Abgebrochen,
    Fehlgeschlagen
}

public sealed record EmailEntwurfErgebnis(
    EmailEntwurfErgebnisStatus Status,
    string? Fehlermeldung = null);

public interface IEmailEntwurfService
{
    EmailEntwurfErgebnis Oeffne(EmailEntwurf entwurf);
}
