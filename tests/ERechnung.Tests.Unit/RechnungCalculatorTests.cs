using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.Tests.Unit;

public sealed class RechnungCalculatorTests
{
    [Fact]
    public void Berechnen_RoundsPositionNetAwayFromZero()
    {
        var position = CreatePosition(1m, 0.005m, 0m);
        var rechnung = new Rechnung { Positionen = [position] };

        RechnungCalculator.Berechnen(rechnung);

        Assert.Equal(0.01m, position.GesamtpreisNetto);
        Assert.Equal(0.01m, rechnung.GesamtbetragNetto);
        Assert.Equal(0.01m, rechnung.GesamtbetragBrutto);
    }

    [Fact]
    public void Berechnen_SumsRoundedPositionNetAmounts()
    {
        var rechnung = new Rechnung
        {
            Positionen =
            [
                CreatePosition(1m, 0.005m, 0m),
                CreatePosition(1m, 0.005m, 0m)
            ]
        };

        RechnungCalculator.Berechnen(rechnung);

        Assert.Equal(0.02m, rechnung.GesamtbetragNetto);
    }

    [Fact]
    public void Berechnen_RoundsTaxOncePerTaxRateGroup()
    {
        var rechnung = new Rechnung
        {
            Positionen =
            [
                CreatePosition(1m, 0.03m, 19m),
                CreatePosition(1m, 0.03m, 19m),
                CreatePosition(1m, 0.10m, 7m)
            ]
        };

        RechnungCalculator.Berechnen(rechnung);

        Assert.Equal(0.16m, rechnung.GesamtbetragNetto);
        Assert.Equal(0.02m, rechnung.UmsatzsteuerBetrag);
        Assert.Equal(0.18m, rechnung.GesamtbetragBrutto);
        Assert.Equal(0m, rechnung.GesamtsteuerRate);
    }

    [Fact]
    public void Berechnen_RoundsTaxMidpointAwayFromZero()
    {
        var rechnung = new Rechnung
        {
            Positionen = [CreatePosition(1m, 0.10m, 5m)]
        };

        RechnungCalculator.Berechnen(rechnung);

        Assert.Equal(0.01m, rechnung.UmsatzsteuerBetrag);
        Assert.Equal(0.11m, rechnung.GesamtbetragBrutto);
    }

    [Fact]
    public void Berechnen_WithSingleTaxRate_SetsOverallTaxRate()
    {
        var rechnung = new Rechnung
        {
            Positionen =
            [
                CreatePosition(2m, 10m, 19m),
                CreatePosition(1m, 5m, 19m)
            ]
        };

        RechnungCalculator.Berechnen(rechnung);

        Assert.Equal(19m, rechnung.GesamtsteuerRate);
        Assert.Equal(25m, rechnung.GesamtbetragNetto);
        Assert.Equal(4.75m, rechnung.UmsatzsteuerBetrag);
        Assert.Equal(29.75m, rechnung.GesamtbetragBrutto);
    }

    [Fact]
    public void Berechnen_AssignsStableSequentialOrder()
    {
        var rechnung = new Rechnung
        {
            Positionen =
            [
                CreatePosition(1m, 1m, 19m, reihenfolge: 20),
                CreatePosition(1m, 1m, 19m, reihenfolge: 10),
                CreatePosition(1m, 1m, 19m, reihenfolge: 30)
            ]
        };

        RechnungCalculator.Berechnen(rechnung);
        RechnungCalculator.Berechnen(rechnung);

        Assert.Equal([1, 2, 3], rechnung.Positionen.Select(position => position.Reihenfolge));
    }

    [Fact]
    public void Berechnen_WithoutPositions_ResetsDerivedValues()
    {
        var rechnung = new Rechnung
        {
            GesamtbetragNetto = 100m,
            UmsatzsteuerBetrag = 19m,
            GesamtbetragBrutto = 119m,
            GesamtsteuerRate = 19m
        };

        RechnungCalculator.Berechnen(rechnung);

        Assert.Equal(0m, rechnung.GesamtbetragNetto);
        Assert.Equal(0m, rechnung.UmsatzsteuerBetrag);
        Assert.Equal(0m, rechnung.GesamtbetragBrutto);
        Assert.Equal(0m, rechnung.GesamtsteuerRate);
    }

    [Fact]
    public void BerechneSteuergruppen_WithMixedRates_ReturnsSortedGroups()
    {
        var positionen = new[]
        {
            CreatePosition(2m, 10m, 19m),
            CreatePosition(1m, 10m, 0m),
            CreatePosition(1m, 10m, 7m)
        };

        var gruppen = RechnungCalculator.BerechneSteuergruppen(positionen);

        Assert.Equal(
            [
                new RechnungsSteuergruppe(0m, 10m, 0m),
                new RechnungsSteuergruppe(7m, 10m, 0.70m),
                new RechnungsSteuergruppe(19m, 20m, 3.80m)
            ],
            gruppen);
    }

    [Fact]
    public void BerechneSteuergruppen_RoundsPositionsBeforeGroupTax()
    {
        var positionen = new[]
        {
            CreatePosition(1m, 0.005m, 19m),
            CreatePosition(1m, 0.015m, 19m)
        };

        var gruppe = Assert.Single(RechnungCalculator.BerechneSteuergruppen(positionen));

        Assert.Equal(0.03m, gruppe.Nettobetrag);
        Assert.Equal(0.01m, gruppe.Steuerbetrag);
    }

    [Fact]
    public void BerechneSteuergruppen_EqualsCalculatedInvoiceTotals()
    {
        var rechnung = new Rechnung
        {
            Positionen =
            [
                CreatePosition(1m, 3.335m, 0m),
                CreatePosition(2m, 4.445m, 7m),
                CreatePosition(3m, 5.555m, 19m)
            ]
        };

        var gruppen = RechnungCalculator.BerechneSteuergruppen(rechnung.Positionen);
        RechnungCalculator.Berechnen(rechnung);

        Assert.Equal(gruppen.Sum(gruppe => gruppe.Nettobetrag), rechnung.GesamtbetragNetto);
        Assert.Equal(gruppen.Sum(gruppe => gruppe.Steuerbetrag), rechnung.UmsatzsteuerBetrag);
        Assert.Equal(
            gruppen.Sum(gruppe => gruppe.Nettobetrag + gruppe.Steuerbetrag),
            rechnung.GesamtbetragBrutto);
    }

    private static RechnungsPosition CreatePosition(
        decimal menge,
        decimal einzelpreisNetto,
        decimal steuersatz,
        int reihenfolge = 0)
    {
        return new RechnungsPosition
        {
            Beschreibung = "Synthetische Leistung",
            Menge = menge,
            EinzelpreisNetto = einzelpreisNetto,
            Steuersatz = steuersatz,
            Reihenfolge = reihenfolge
        };
    }
}
