using System.Globalization;
using System.Xml.Linq;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.XML.Generators;

/// <summary>
/// Erzeugt EN 16931-konformes UBL 2.2 CII XML aus einer <see cref="Rechnung"/>.
/// Profil: Basic WL (urn:cen.eu:en16931:2017).
/// </summary>
public sealed class UblGenerator : IUblGenerator
{
    // ── Namespaces ──────────────────────────────────────────────────────
    private static readonly XNamespace Rsm = "urn:oasis:names:specification:ubl:schema:xsd:CrossIndustryInvoice-1";
    private static readonly XNamespace Qdt = "urn:oasis:names:specification:ubl:schema:xsd:QualifiedDataTypes-2";
    private static readonly XNamespace Ram = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    private const string En16931GuidelineId = "urn:cen.eu:en16931:2017";
    private const string InvoiceTypeCode = "380";

    public string Generate(Rechnung rechnung)
    {
        ArgumentException.ThrowIfNullOrEmpty(rechnung.Nummer, nameof(rechnung.Nummer));

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(Rsm + "CrossIndustryInvoice",
                new XAttribute(XNamespace.Xmlns + "rsm", Rsm.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "qdt", Qdt.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "ram", Ram.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "cbc", Cbc.NamespaceName),
                GenerateExchangedDocumentContext(),
                GenerateExchangedDocument(rechnung),
                GenerateSupplyChainTradeTransaction(rechnung)));

        return doc.ToString(SaveOptions.None);
    }

    // ── ExchangedDocumentContext ──────────────────────────────────────────

    private static XElement GenerateExchangedDocumentContext()
    {
        return new XElement(Rsm + "ExchangedDocumentContext",
            new XElement(Ram + "GuidelineSpecifiedDocumentContextParameter",
                new XElement(Qdt + "ID",
                    new XAttribute("schemeAgencyID", "320"),
                    En16931GuidelineId)));
    }

    // ── ExchangedDocument ─────────────────────────────────────────────────

    private static XElement GenerateExchangedDocument(Rechnung r)
    {
        var children = new List<XElement>
        {
            new(Cbc + "ID", r.Nummer),
            new(Cbc + "TypeCode", InvoiceTypeCode),
            new(Cbc + "IssueDate", new XAttribute("format", "102"), r.Rechnungsdatum.ToString("yyyyMMdd")),
        };

        if (!string.IsNullOrWhiteSpace(r.Bemerkung))
        {
            children.Add(new XElement(Ram + "IncludedNote",
                new XElement(Cbc + "Content", r.Bemerkung)));
        }

        return new XElement(Rsm + "ExchangedDocument", children);
    }

    // ── SupplyChainTradeTransaction ───────────────────────────────────────

    private static XElement GenerateSupplyChainTradeTransaction(Rechnung r)
    {
        return new XElement(Rsm + "SupplyChainTradeTransaction",
            new XElement(Ram + "ApplicableHeaderTradeDelivery", GenerateDelivery(r)),
            new XElement(Ram + "ApplicableHeaderTradeAgreement", GenerateAgreement(r)),
            new XElement(Ram + "ApplicableHeaderTradeSettlement", GenerateSettlement(r)),
            GenerateLineItems(r));
    }

    // ── Delivery ──────────────────────────────────────────────────────────

    private static XElement[] GenerateDelivery(Rechnung r)
    {
        if (r.Leistungsdatum.HasValue)
        {
            return
            [
                new XElement(Ram + "ActualDeliverySupplyChainEvent",
                    new XElement(Cbc + "OccurrenceDateTime",
                        new XAttribute("format", "102"),
                        r.Leistungsdatum.Value.ToString("yyyyMMdd"))),
            ];
        }

        return Array.Empty<XElement>();
    }

    // ── Agreement ─────────────────────────────────────────────────────────

    private static XElement[] GenerateAgreement(Rechnung r)
    {
        var children = new List<XElement>();

        if (r.AbsenderSnapshot is not null)
            children.Add(GenerateSellerParty(r.AbsenderSnapshot));

        if (r.EmpfaengerSnapshot is not null)
            children.Add(GenerateBuyerParty(r.EmpfaengerSnapshot));

        return children.ToArray();
    }

    private static XElement GenerateSellerParty(RechnungsAbsenderSnapshot s)
    {
        var children = new List<XElement>
        {
            new XElement(Cbc + "Name", s.Name),
            PostalAddress(s.Strasse, s.PLZ, s.Ort, s.Land),
            CommunicationContact(s.Ansprechpartner, s.Telefon, s.Email)
        };

        if (!string.IsNullOrWhiteSpace(s.UstIdNr))
        {
            children.Add(SpecifiedTaxRegistration(s.UstIdNr));
        }

        return new XElement(Ram + "SellerTradeParty", children);
    }

    private static XElement GenerateBuyerParty(RechnungsEmpfaengerSnapshot s)
    {
        var children = new List<XElement>
        {
            new XElement(Cbc + "Name", s.Name),
            PostalAddress(s.Strasse, s.PLZ, s.Ort, s.Land),
            CommunicationContact(s.Ansprechpartner, string.Empty, s.Email)
        };

        if (!string.IsNullOrWhiteSpace(s.UstIdNr))
        {
            children.Add(SpecifiedTaxRegistration(s.UstIdNr));
        }

        return new XElement(Ram + "BuyerTradeParty", children);
    }

    // ── Settlement ────────────────────────────────────────────────────────

    private static XElement[] GenerateSettlement(Rechnung r)
    {
        var children = new List<XElement>
        {
            new XElement(Cbc + "InvoiceCurrencyCode", r.Waehrung),
            new XElement(Cbc + "PayableCurrencyCode", r.Waehrung),
            HeaderTradeTax(r),
            PaymentTerms(r),
            MonetarySummation(r),
        };

        if (r.AbsenderSnapshot is not null)
        {
            var payeeChildren = new List<XElement> { new XElement(Cbc + "Name", r.AbsenderSnapshot.Name) };
            if (!string.IsNullOrWhiteSpace(r.AbsenderSnapshot.UstIdNr))
            {
                payeeChildren.Add(SpecifiedTaxRegistration(r.AbsenderSnapshot.UstIdNr));
            }

            children.Insert(0, new XElement(Ram + "PayeeTradeParty", payeeChildren));

            // SEPA Zahlungsart hinzufügen (wenn IBAN vorhanden)
            if (!string.IsNullOrWhiteSpace(r.AbsenderSnapshot.IBAN))
            {
                children.Insert(1,
                    new XElement(Ram + "ApplicableTradeSettlementPaymentMeans",
                        new XElement(Cbc + "InstructionID", "1"),
                        new XElement(Cbc + "PaymentMeansCode", new XAttribute("name", "Credit transfer"), "58"),
                        new XElement(Ram + "PayeeSpecifiedCreditorFinancialAccount",
                            new XElement(Cbc + "IBANID", r.AbsenderSnapshot.IBAN))));
            }
        }

        return children.ToArray();
    }

    // ── Header Trade Tax ──────────────────────────────────────────────────

    private static XElement HeaderTradeTax(Rechnung r)
    {
        var children = new List<XElement>();

        foreach (var stg in RechnungCalculator.BerechneSteuergruppen(r.Positionen))
        {
            children.Add(ApplicableTradeTaxEntry(stg, r.Waehrung));
        }

        children.Add(new XElement(Cbc + "TaxTotalAmount",
            new XAttribute("currencyID", r.Waehrung),
            FormatDecimal(r.UmsatzsteuerBetrag)));

        return new XElement(Ram + "ApplicableHeaderTradeTax", children);
    }

    private static XElement ApplicableTradeTaxEntry(RechnungsSteuergruppe stg, string währung)
    {
        return new XElement(Ram + "ApplicableTradeTax",
            new XElement(Cbc + "CalculatedAmount",
                new XAttribute("currencyID", währung),
                FormatDecimal(stg.Steuerbetrag)),
            new XElement(Cbc + "TypeCode", "VAT"),
            new XElement(Cbc + "BasisAmount",
                new XAttribute("currencyID", währung),
                FormatDecimal(stg.Nettobetrag)),
            new XElement(Ram + "TaxCategory",
                new XElement(Cbc + "ID", VatCategoryCode(stg)),
                new XElement(Cbc + "Percent", FormatDecimal(stg.Steuersatz))),
            new XElement(Cbc + "RateApplicablePercent",
                FormatDecimal(stg.Steuersatz)));
    }

    private static string VatCategoryCode(RechnungsSteuergruppe stg)
    {
        if (stg.Steuersatz == 0m)
            return stg.Nettobetrag > 0m ? "Z" : "E";
        return "S";
    }

    // ── Payment Terms ─────────────────────────────────────────────────────

    private static XElement PaymentTerms(Rechnung r)
    {
        if (r.Faeligkeitsdatum.HasValue)
            return new XElement(Ram + "SpecifiedTradePaymentTerms",
                new XElement(Cbc + "DescriptionDueDate",
                    new XAttribute("format", "102"),
                    r.Faeligkeitsdatum.Value.ToString("yyyyMMdd")));

        return new XElement(Ram + "SpecifiedTradePaymentTerms");
    }

    // ── Monetary Summation ────────────────────────────────────────────────

    private static XElement MonetarySummation(Rechnung r)
    {
        return new XElement(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation",
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", r.Waehrung),
                FormatDecimal(r.GesamtbetragNetto)),
            new XElement(Cbc + "TaxBasisTotalAmount",
                new XAttribute("currencyID", r.Waehrung),
                FormatDecimal(r.GesamtbetragNetto)),
            new XElement(Cbc + "TaxTotalAmount",
                new XAttribute("currencyID", r.Waehrung),
                FormatDecimal(r.UmsatzsteuerBetrag)),
            new XElement(Cbc + "GrandTotalAmount",
                new XAttribute("currencyID", r.Waehrung),
                FormatDecimal(r.GesamtbetragBrutto)),
            new XElement(Cbc + "TotalAllowanceChargeAmount",
                new XAttribute("currencyID", r.Waehrung),
                FormatDecimal(0m)),
            new XElement(Cbc + "ChargeTotalAmount",
                new XAttribute("currencyID", r.Waehrung),
                FormatDecimal(r.GesamtbetragBrutto)),
            new XElement(Cbc + "PrepaidAmount",
                new XAttribute("currencyID", r.Waehrung),
                FormatDecimal(0m)),
            new XElement(Cbc + "PayableAmount",
                new XAttribute("currencyID", r.Waehrung),
                FormatDecimal(r.GesamtbetragBrutto)));
    }

    // ── Line Items ────────────────────────────────────────────────────────

    private static XElement[] GenerateLineItems(Rechnung r)
    {
        return r.Positionen
            .OfType<RechnungsPosition>()
            .Select(p => LineItem(p, r.Waehrung))
            .ToArray();
    }

    private static XElement LineItem(RechnungsPosition p, string waehrung)
    {
        return new XElement(Ram + "IncludedSupplyChainTradeLineItem",
            new XElement(Cbc + "AssociatedDocumentLineReference",
                new XElement(Cbc + "LineID", p.Reihenfolge)),
            new XElement(Ram + "SpecifiedTradeProduct",
                new XElement(Cbc + "Name", p.Beschreibung)),
            new XElement(Ram + "SpecifiedLineTradeDelivery",
                new XElement(Cbc + "BilledQuantity",
                    new XAttribute("unitCode", p.Einheit),
                    FormatDecimal(p.Menge))),
            new XElement(Ram + "SpecifiedLineTradePricing",
                new XElement(Ram + "ItemPriceAmount",
                    new XAttribute("currencyID", waehrung),
                    FormatDecimal(p.EinzelpreisNetto)),
                new XElement(Cbc + "LineExtensionAmount",
                    new XAttribute("currencyID", waehrung),
                    FormatDecimal(p.GesamtpreisNetto))),
            new XElement(Ram + "SpecifiedLineTradeTax",
                new XElement(Cbc + "CalculatedAmount",
                    new XAttribute("currencyID", waehrung),
                    FormatDecimal(decimal.Round(p.GesamtpreisNetto * p.Steuersatz / 100m, 2))),
                new XElement(Cbc + "TypeCode", "VAT"),
                new XElement(Cbc + "BasisAmount",
                    new XAttribute("currencyID", waehrung),
                    FormatDecimal(p.GesamtpreisNetto)),
                new XElement(Ram + "TaxCategory",
                    new XElement(Cbc + "ID", LineVatCategoryCode(p)),
                    new XElement(Cbc + "Percent", FormatDecimal(p.Steuersatz))),
                new XElement(Cbc + "RateApplicablePercent",
                    FormatDecimal(p.Steuersatz))));
    }

    private static string LineVatCategoryCode(RechnungsPosition p)
    {
        if (p.Steuersatz == 0m)
            return p.GesamtpreisNetto > 0m ? "Z" : "E";
        return "S";
    }

    // ── Shared helpers ────────────────────────────────────────────────────

    private static XElement PostalAddress(string strasse, string plz, string ort, string land)
    {
        return new XElement(Ram + "PostalTradeAddress",
            new XElement(Cbc + "StreetName", strasse),
            new XElement(Cbc + "PostcodeCode", plz),
            new XElement(Cbc + "CityName", ort),
            new XElement(Ram + "CountryID", land),
            new XElement(Ram + "CountryName", CountryName(land)));
    }

    private static XElement CommunicationContact(string person, string phone, string email)
    {
        var children = new List<XElement>();

        if (!string.IsNullOrWhiteSpace(person))
            children.Add(new XElement(Cbc + "PersonName", person));

        if (!string.IsNullOrWhiteSpace(phone))
            children.Add(new XElement(Cbc + "TelephoneNumber", phone));

        if (!string.IsNullOrWhiteSpace(email))
            children.Add(new XElement(Ram + "URIUniversalCommunication",
                new XElement(Cbc + "URIID", new XAttribute("schemeID", "EM"), email)));

        return new XElement(Ram + "SpecifiedCommunication", children);
    }

    private static string FormatDecimal(decimal value) =>
        value.ToString("0.00#", CultureInfo.InvariantCulture);

    private static string CountryName(string isoCode) => isoCode switch
    {
        "DE" => "Deutschland",
        "AT" => "Österreich",
        "CH" => "Schweiz",
        "NL" => "Niederlande",
        "BE" => "Belgien",
        "FR" => "Frankreich",
        "IT" => "Italien",
        "ES" => "Spanien",
        "PL" => "Polen",
        "CZ" => "Tschechien",
        "UK" => "Vereinigtes Königreich",
        "IE" => "Irland",
        _ => isoCode,
    };

    private static XElement SpecifiedTaxRegistration(string ustId)
    {
        return new XElement(Ram + "SpecifiedTaxRegistration",
            new XElement(Cbc + "ID", new XAttribute("schemeID", "VA"), ustId));
    }
}