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

        var positionen = (rechnung.Positionen ?? [])
            .Where(position => position is not null)
            .ToArray();

        for (var index = 0; index < positionen.Length; index++)
        {
            positionen[index].Reihenfolge = index + 1;
        }

        var steuergruppen = BerechneSteuergruppen(positionen);

        rechnung.GesamtbetragNetto = RundeGeldbetrag(
            steuergruppen.Sum(gruppe => gruppe.Nettobetrag));
        rechnung.UmsatzsteuerBetrag = RundeGeldbetrag(
            steuergruppen.Sum(gruppe => gruppe.Steuerbetrag));
        rechnung.GesamtbetragBrutto = RundeGeldbetrag(
            rechnung.GesamtbetragNetto + rechnung.UmsatzsteuerBetrag);
        rechnung.GesamtsteuerRate = steuergruppen.Count == 1
            ? steuergruppen[0].Steuersatz
            : 0m;

        return rechnung;
    }

    public static IReadOnlyList<RechnungsSteuergruppe> BerechneSteuergruppen(
        IEnumerable<RechnungsPosition> positionen)
    {
        ArgumentNullException.ThrowIfNull(positionen);

        return positionen
            .Where(position => position is not null)
            .GroupBy(position => position.Steuersatz)
            .OrderBy(gruppe => gruppe.Key)
            .Select(gruppe =>
            {
                var nettobetrag = RundeGeldbetrag(
                    gruppe.Sum(position => position.GesamtpreisNetto));
                var steuerbetrag = RundeGeldbetrag(nettobetrag * gruppe.Key / 100m);

                return new RechnungsSteuergruppe(
                    gruppe.Key,
                    nettobetrag,
                    steuerbetrag);
            })
            .ToArray();
    }

    private static decimal RundeGeldbetrag(decimal betrag)
    {
        return decimal.Round(betrag, 2, MidpointRounding.AwayFromZero);
    }
}
