using System.Xml.Linq;
using ERechnung.Core.Models;
using ERechnung.Core.Services;
using ERechnung.XML.Generators;

namespace ERechnung.Tests.Unit;

public sealed class UblGeneratorTests
{
    private static readonly XNamespace Rsm =
        "urn:oasis:names:specification:ubl:schema:xsd:CrossIndustryInvoice-1";
    private static readonly XNamespace Ram =
        "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc =
        "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Qdt =
        "urn:oasis:names:specification:ubl:schema:xsd:QualifiedDataTypes-2";

    private readonly UblGenerator _generator = new();

    [Fact]
    public void Generate_ProducesValidXmlDocument()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);

        Assert.NotNull(doc.Root);
        Assert.Equal(Rsm + "CrossIndustryInvoice", doc.Root.Name);
    }

    [Fact]
    public void Generate_RootHasAllRequiredNamespaces()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);

        Assert.NotNull(doc.Root!.Attribute(XNamespace.Xmlns + "rsm"));
        Assert.NotNull(doc.Root!.Attribute(XNamespace.Xmlns + "qdt"));
        Assert.NotNull(doc.Root!.Attribute(XNamespace.Xmlns + "ram"));
        Assert.NotNull(doc.Root!.Attribute(XNamespace.Xmlns + "cbc"));
    }

    [Fact]
    public void Generate_ContainsEn16931Guideline()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var context = doc.Root!.Element(Rsm + "ExchangedDocumentContext")!;
        var guideline = context.Element(Ram + "GuidelineSpecifiedDocumentContextParameter")!;
        var id = guideline.Element(Qdt + "ID")!;

        Assert.Equal("urn:cen.eu:en16931:2017", id.Value);
    }

    [Fact]
    public void Generate_SetsInvoiceNumberAndTypeCode()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var exchangedDoc = doc.Root!.Element(Rsm + "ExchangedDocument")!;

        Assert.Equal("RE-2026-0042", exchangedDoc.Element(Cbc + "ID")!.Value);
        Assert.Equal("380", exchangedDoc.Element(Cbc + "TypeCode")!.Value);
    }

    [Fact]
    public void Generate_EncodesIssueDateAsyyyyMMdd()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var issueDate = doc.Root!.Element(Rsm + "ExchangedDocument")!
            .Element(Cbc + "IssueDate")!;

        Assert.Equal("20260803", issueDate.Value);
        Assert.Equal("102", issueDate.Attribute("format")!.Value);
    }

    [Fact]
    public void Generate_IncludesSellerParty()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var transaction = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!;
        var agreement = transaction.Element(Ram + "ApplicableHeaderTradeAgreement")!;

        var seller = agreement.Element(Ram + "SellerTradeParty")!;
        Assert.Equal("Synthetischer Absender GmbH", seller.Element(Cbc + "Name")!.Value);
    }

    [Fact]
    public void Generate_IncludesBuyerParty()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var transaction = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!;
        var agreement = transaction.Element(Ram + "ApplicableHeaderTradeAgreement")!;

        var buyer = agreement.Element(Ram + "BuyerTradeParty")!;
        Assert.Equal("Synthetischer Kunde GmbH", buyer.Element(Cbc + "Name")!.Value);
    }

    [Fact]
    public void Generate_SellerParty_IncludesVatRegistration()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var transaction = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!;
        var agreement = transaction.Element(Ram + "ApplicableHeaderTradeAgreement")!;
        var seller = agreement.Element(Ram + "SellerTradeParty")!;
        var taxReg = seller.Element(Ram + "SpecifiedTaxRegistration")!;
        var vatId = taxReg.Element(Cbc + "ID")!;

        Assert.Equal("VA", vatId.Attribute("schemeID")!.Value);
        Assert.Equal("DE987654321", vatId.Value);
    }

    [Fact]
    public void Generate_BuyerParty_IncludesVatRegistration()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var transaction = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!;
        var agreement = transaction.Element(Ram + "ApplicableHeaderTradeAgreement")!;
        var buyer = agreement.Element(Ram + "BuyerTradeParty")!;
        var taxReg = buyer.Element(Ram + "SpecifiedTaxRegistration")!;
        var vatId = taxReg.Element(Cbc + "ID")!;

        Assert.Equal("VA", vatId.Attribute("schemeID")!.Value);
        Assert.Equal("DE123456789", vatId.Value);
    }

    [Fact]
    public void Generate_IncludesMonetarySummation()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var settlement = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Element(Ram + "ApplicableHeaderTradeSettlement")!;

        var summation = settlement.Element(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation")!;

        Assert.Equal("200.00", summation.Element(Cbc + "LineExtensionAmount")!.Value);
        Assert.Equal("38.00", summation.Element(Cbc + "TaxTotalAmount")!.Value);
        Assert.Equal("238.00", summation.Element(Cbc + "GrandTotalAmount")!.Value);
        Assert.Equal("238.00", summation.Element(Cbc + "PayableAmount")!.Value);
    }

    [Fact]
    public void Generate_IncludesTradeTaxWithVATTypeCode()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var settlement = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Element(Ram + "ApplicableHeaderTradeSettlement")!;

        var appTradeTax = settlement.Element(Ram + "ApplicableHeaderTradeTax")!;
        var taxEntries = appTradeTax.Elements(Ram + "ApplicableTradeTax").ToList();

        Assert.Single(taxEntries);
        Assert.Equal("VAT", taxEntries[0].Element(Cbc + "TypeCode")!.Value);
    }

    [Fact]
    public void Generate_TaxCategoryCode_IsStandardForPositiveRate()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var settlement = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Element(Ram + "ApplicableHeaderTradeSettlement")!;

        var taxEntries = settlement.Element(Ram + "ApplicableHeaderTradeTax")!
            .Elements(Ram + "ApplicableTradeTax")
            .ToList();

        var category = taxEntries[0].Element(Ram + "TaxCategory")!;
        Assert.Equal("S", category.Element(Cbc + "ID")!.Value);
    }

    [Fact]
    public void Generate_IncludesLineItemDescription()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var lineItems = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Elements(Ram + "IncludedSupplyChainTradeLineItem")
            .ToList();

        Assert.Single(lineItems);
        var product = lineItems[0].Element(Ram + "SpecifiedTradeProduct")!;
        Assert.Equal("Synthetische Dienstleistung", product.Element(Cbc + "Name")!.Value);
    }

    [Fact]
    public void Generate_LineItem_UnitPriceMatchesSingleLine()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var lineItems = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Elements(Ram + "IncludedSupplyChainTradeLineItem")
            .ToList();

        var pricing = lineItems[0].Element(Ram + "SpecifiedLineTradePricing")!;
        var itemPrice = pricing.Element(Ram + "ItemPriceAmount")!;
        Assert.Equal("100.00", itemPrice.Value);
    }

    [Fact]
    public void Generate_LineItem_LineExtensionMatchesQuantityTimesUnitPrice()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var lineItems = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Elements(Ram + "IncludedSupplyChainTradeLineItem")
            .ToList();

        var pricing = lineItems[0].Element(Ram + "SpecifiedLineTradePricing")!;
        var ext = pricing.Element(Cbc + "LineExtensionAmount")!;
        // 2 * 100 = 200
        Assert.Equal("200.00", ext.Value);
    }

    [Fact]
    public void Generate_MultiplePositions_ProducesMultipleLines()
    {
        var invoice = CreateInvoice();
        invoice.Positionen.Add(new RechnungsPosition
        {
            Reihenfolge = 2,
            Beschreibung = "Zusätzliche Leistung",
            Menge = 1m,
            EinzelpreisNetto = 50m,
            Steuersatz = 19m
        });
        RechnungCalculator.Berechnen(invoice);

        var xml = _generator.Generate(invoice);
        var doc = XDocument.Parse(xml);
        var lines = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Elements(Ram + "IncludedSupplyChainTradeLineItem")
            .ToList();

        Assert.Equal(2, lines.Count);
        Assert.Equal("200.00", lines[0].Element(Ram + "SpecifiedLineTradePricing")!
            .Element(Cbc + "LineExtensionAmount")!.Value);
        Assert.Equal("50.00", lines[1].Element(Ram + "SpecifiedLineTradePricing")!
            .Element(Cbc + "LineExtensionAmount")!.Value);
    }

    [Fact]
    public void Generate_MixedTaxRates_ProducesSeparateTaxEntries()
    {
        var invoice = CreateInvoice();
        invoice.Positionen.Clear();
        invoice.Positionen.AddRange([
            CreatePosition(1m, 100m, 19m, 1),
            CreatePosition(1m, 50m, 7m, 2)
        ]);
        RechnungCalculator.Berechnen(invoice);

        var xml = _generator.Generate(invoice);
        var doc = XDocument.Parse(xml);
        var settlement = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Element(Ram + "ApplicableHeaderTradeSettlement")!;

        var taxEntries = settlement.Element(Ram + "ApplicableHeaderTradeTax")!
            .Elements(Ram + "ApplicableTradeTax")
            .ToList();

        Assert.Equal(2, taxEntries.Count);
        Assert.Equal("VAT", taxEntries[0].Element(Cbc + "TypeCode")!.Value);
        Assert.Equal("VAT", taxEntries[1].Element(Cbc + "TypeCode")!.Value);
    }

    [Fact]
    public void Generate_ZeroTaxRate_SetsZeroCategoryCodeInLineItem()
    {
        var invoice = CreateInvoice();
        invoice.Positionen.Clear();
        invoice.Positionen.Add(CreatePosition(1m, 100m, 0m, 1));
        RechnungCalculator.Berechnen(invoice);

        var xml = _generator.Generate(invoice);
        var doc = XDocument.Parse(xml);
        var lineItems = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Elements(Ram + "IncludedSupplyChainTradeLineItem")
            .ToList();

        var lineTax = lineItems[0].Element(Ram + "SpecifiedLineTradeTax")!;
        var category = lineTax.Element(Ram + "TaxCategory")!;
        Assert.Equal("Z", category.Element(Cbc + "ID")!.Value);
    }

    [Fact]
    public void Generate_IncludesCommentAsIncludedNote()
    {
        var invoice = CreateInvoice();
        invoice.Bemerkung = "Bitte binnen 14 Tagen bezahlen.";

        var xml = _generator.Generate(invoice);
        var doc = XDocument.Parse(xml);
        var exchangedDoc = doc.Root!.Element(Rsm + "ExchangedDocument")!;

        var note = exchangedDoc.Element(Ram + "IncludedNote")!;
        Assert.Equal("Bitte binnen 14 Tagen bezahlen.", note.Element(Cbc + "Content")!.Value);
    }

    [Fact]
    public void Generate_OmitsCommentWhenEmpty()
    {
        var invoice = CreateInvoice();
        invoice.Bemerkung = string.Empty;

        var xml = _generator.Generate(invoice);
        var doc = XDocument.Parse(xml);
        var exchangedDoc = doc.Root!.Element(Rsm + "ExchangedDocument")!;

        Assert.Null(exchangedDoc.Element(Ram + "IncludedNote"));
    }

    [Fact]
    public void Generate_ThrowsWhenInvoiceNumberIsEmpty()
    {
        var invoice = CreateInvoice();
        invoice.Nummer = string.Empty;

        Assert.Throws<ArgumentException>(() => _generator.Generate(invoice));
    }

    [Fact]
    public void Generate_PerformancePeriod_IncludesActualDeliveryDate()
    {
        var invoice = CreateInvoice();
        invoice.Leistungsdatum = new DateTime(2026, 7, 15);

        var xml = _generator.Generate(invoice);
        var doc = XDocument.Parse(xml);
        var delivery = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Element(Ram + "ApplicableHeaderTradeDelivery")!;

        var deliveryEvent = delivery.Element(Ram + "ActualDeliverySupplyChainEvent")!;
        var date = deliveryEvent.Element(Cbc + "OccurrenceDateTime")!;
        Assert.Equal("20260715", date.Value);
        Assert.Equal("102", date.Attribute("format")!.Value);
    }

    [Fact]
    public void Generate_PaymentTerms_IncludeDueDate()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var settlement = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Element(Ram + "ApplicableHeaderTradeSettlement")!;

        var terms = settlement.Element(Ram + "SpecifiedTradePaymentTerms")!;
        var dueDate = terms.Element(Cbc + "DescriptionDueDate")!;
        Assert.Equal("20260817", dueDate.Value);
    }

    [Fact]
    public void Generate_CurrencyCodes_AreEUR()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var settlement = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Element(Ram + "ApplicableHeaderTradeSettlement")!;

        Assert.Equal("EUR", settlement.Element(Cbc + "InvoiceCurrencyCode")!.Value);
        Assert.Equal("EUR", settlement.Element(Cbc + "PayableCurrencyCode")!.Value);
    }

    [Fact]
    public void Generate_NonEurCurrency_PropagatesToLineItems()
    {
        var rechnung = CreateInvoice();
        rechnung.Waehrung = "GBP";
        RechnungCalculator.Berechnen(rechnung);
        var xml = _generator.Generate(rechnung);
        var doc = XDocument.Parse(xml);

        // Header level
        var settlement = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!
            .Element(Ram + "ApplicableHeaderTradeSettlement")!;
        Assert.Equal("GBP", settlement.Element(Cbc + "InvoiceCurrencyCode")!.Value);

        // Line items
        var lineItems = doc.Root!
            .Element(Rsm + "SupplyChainTradeTransaction")!
            .Elements(Ram + "IncludedSupplyChainTradeLineItem");
        foreach (var lineItem in lineItems)
        {
            var pricing = lineItem.Element(Ram + "SpecifiedLineTradePricing")!;
            var itemPrice = pricing.Element(Ram + "ItemPriceAmount")!;
            Assert.Equal("GBP", itemPrice.Attribute("currencyID")!.Value);

            var lineExt = pricing.Element(Cbc + "LineExtensionAmount")!;
            Assert.Equal("GBP", lineExt.Attribute("currencyID")!.Value);

            var lineTax = lineItem.Element(Ram + "SpecifiedLineTradeTax")!;
            Assert.Equal("GBP", lineTax.Element(Cbc + "CalculatedAmount")!.Attribute("currencyID")!.Value);
            Assert.Equal("GBP", lineTax.Element(Cbc + "BasisAmount")!.Attribute("currencyID")!.Value);
        }
    }

    [Fact]
    public void Generate_PostalAddress_ContainsCityAndPostalCode()
    {
        var xml = _generator.Generate(CreateInvoice());
        var doc = XDocument.Parse(xml);
        var transaction = doc.Root!.Element(Rsm + "SupplyChainTradeTransaction")!;
        var agreement = transaction.Element(Ram + "ApplicableHeaderTradeAgreement")!;

        var buyer = agreement.Element(Ram + "BuyerTradeParty")!;
        var address = buyer.Element(Ram + "PostalTradeAddress")!;

        Assert.Equal("Musterstraße 1", address.Element(Cbc + "StreetName")!.Value);
        Assert.Equal("10115", address.Element(Cbc + "PostcodeCode")!.Value);
        Assert.Equal("Berlin", address.Element(Cbc + "CityName")!.Value);
    }

    private static Rechnung CreateInvoice()
    {
        var invoice = new Rechnung
        {
            Nummer = "RE-2026-0042",
            Titel = "Synthetische Testrechnung",
            Rechnungsdatum = new DateTime(2026, 8, 3),
            Erstellungsdatum = new DateTime(2026, 8, 3),
            Faeligkeitsdatum = new DateTime(2026, 8, 17),
            Waehrung = "EUR",
            Bemerkung = "Synthetische Testrechnung",
            EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
            {
                QuellId = 11,
                Name = "Synthetischer Kunde GmbH",
                Ansprechpartner = "Max Mustermann",
                Strasse = "Musterstraße 1",
                PLZ = "10115",
                Ort = "Berlin",
                Land = "DE",
                Email = "kunde@example.com",
                UstIdNr = "DE123456789"
            },
            AbsenderSnapshot = new RechnungsAbsenderSnapshot
            {
                QuellId = 21,
                Name = "Synthetischer Absender GmbH",
                Ansprechpartner = "Firma Müller",
                Strasse = "Lieferantenallee 5",
                PLZ = "10117",
                Ort = "Berlin",
                Land = "DE",
                Email = "absender@example.com",
                Telefon = "+49 30 123456",
                UstIdNr = "DE987654321",
                IBAN = "DE12500105170648489890",
                BIC = "INGDDEFFXXX"
            },
            Positionen = [CreatePosition(2m, 100m, 19m, 1)]
        };
        RechnungCalculator.Berechnen(invoice);
        return invoice;
    }

    private static RechnungsPosition CreatePosition(
        decimal menge, decimal einzelpreisNetto, decimal steuersatz, int reihenfolge)
    {
        return new RechnungsPosition
        {
            Reihenfolge = reihenfolge,
            Menge = menge,
            EinzelpreisNetto = einzelpreisNetto,
            Steuersatz = steuersatz,
            Beschreibung = "Synthetische Dienstleistung",
            Einheit = "ST"
        };
    }
}