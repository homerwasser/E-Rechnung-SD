namespace ERechnung.App.Services;

public interface IDateiOeffner
{
    void Oeffne(string dateiPfad);
    void ImExplorerAnzeigen(string dateiPfad);
}
