using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

/// <summary>
/// Berechnet ausschließlich die aus den Rechnungspositionen abgeleiteten Werte.
/// </summary>
public static class RechnungCalculator
{
    public static Rechnung Berechnen(Rechnung rechnung)
    {
        ArgumentNullException.ThrowIfNull(rechnung);

        var nettobetraegeNachSteuersatz = new Dictionary<decimal, decimal>();
        var gesamtbetragNetto = 0m;
        var naechsteReihenfolge = 1;

        foreach (var position in rechnung.Positionen ?? [])
        {
            if (position is null)
            {
                continue;
            }

            position.Reihenfolge = naechsteReihenfolge++;

            var positionsnetto = position.GesamtpreisNetto;
            gesamtbetragNetto += positionsnetto;

            nettobetraegeNachSteuersatz.TryGetValue(position.Steuersatz, out var gruppennetto);
            nettobetraegeNachSteuersatz[position.Steuersatz] = gruppennetto + positionsnetto;
        }

        var umsatzsteuerBetrag = nettobetraegeNachSteuersatz.Sum(gruppe =>
            RundeGeldbetrag(gruppe.Value * gruppe.Key / 100m));

        rechnung.GesamtbetragNetto = RundeGeldbetrag(gesamtbetragNetto);
        rechnung.UmsatzsteuerBetrag = RundeGeldbetrag(umsatzsteuerBetrag);
        rechnung.GesamtbetragBrutto = RundeGeldbetrag(
            rechnung.GesamtbetragNetto + rechnung.UmsatzsteuerBetrag);
        rechnung.GesamtsteuerRate = nettobetraegeNachSteuersatz.Count == 1
            ? nettobetraegeNachSteuersatz.Keys.Single()
            : 0m;

        return rechnung;
    }

    private static decimal RundeGeldbetrag(decimal betrag)
    {
        return decimal.Round(betrag, 2, MidpointRounding.AwayFromZero);
    }
}
