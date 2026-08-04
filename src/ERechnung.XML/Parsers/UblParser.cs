using System.Globalization;
using System.Xml.Linq;
using ERechnung.Core.Models;

namespace ERechnung.XML.Parsers;

/// <summary>
/// Parst UBL 2.2 CrossIndustryInvoice XML zurück in ein <see cref="Rechnung"/>-Objekt.
/// </summary>
public sealed class UblParser : IUblParser
{
    private static readonly XNamespace Rsm = "urn:oasis:names:specification:ubl:schema:xsd:CrossIndustryInvoice-1";
    private static readonly XNamespace Qdt = "urn:oasis:names:specification:ubl:schema:xsd:QualifiedDataTypes-2";
    private static readonly XNamespace Ram = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    public Rechnung Parse(string xmlText)
    {
        if (string.IsNullOrWhiteSpace(xmlText))
        {
            throw new ArgumentException("Der XML-Text darf nicht leer sein.", nameof(xmlText));
        }

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xmlText);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Die XML-Zeichenkette konnte nicht geparst werden.", ex);
        }

        var root = doc.Root ?? throw new InvalidOperationException("Das XML-Dokument hat kein Wurzelelement.");

        if (root.Name.LocalName != "CrossIndustryInvoice")
        {
            throw new InvalidOperationException(
                $"Unerwartetes Wurzelelement '{root.Name.LocalName}'. Erwartet wurde 'CrossIndustryInvoice'.");
        }

        var exchangedDoc = root.Element(Rsm + "ExchangedDocument")
            ?? throw new InvalidOperationException("Das Element 'ExchangedDocument' fehlt im XML-Dokument.");

        var transaction = root.Element(Rsm + "SupplyChainTradeTransaction")
            ?? throw new InvalidOperationException("Das Element 'SupplyChainTradeTransaction' fehlt im XML-Dokument.");

        var invoice = new Rechnung
        {
            Nummer = LesElementWert(exchangedDoc, Cbc + "ID"),
            Rechnungsdatum = LiesDatum(exchangedDoc, Cbc + "IssueDate"),
            Bemerkung = LiesNotenInhalt(exchangedDoc),
        };

        // Lieferdaten
        LiesLieferdaten(transaction, invoice);

        // Vertragsdaten – Absender & Empfänger
        LiesVertragsdaten(transaction, invoice);

        // Zahlungsbedingungen
        LiesZahlungsbedingungen(transaction, invoice);

        // Abrechnungsdaten – Währung, Steuern, Summen
        LiesAbrechnungsdaten(transaction, invoice);

        // Positionsdaten
        LiesPositionen(transaction, invoice);

        return invoice;
    }

    private static void LiesLieferdaten(XElement transaction, Rechnung invoice)
    {
        var delivery = transaction.Element(Ram + "ApplicableHeaderTradeDelivery");
        if (delivery is null)
        {
            return;
        }

        var deliveryEvent = delivery.Element(Ram + "ActualDeliverySupplyChainEvent");
        if (deliveryEvent is not null)
        {
            var dateTimeElement = deliveryEvent.Element(Cbc + "OccurrenceDateTime");
            if (dateTimeElement is not null)
            {
                invoice.Leistungsdatum = dateTimeElement.Value.ToDateTimeYyyyMMdd();
            }
        }
    }

    private static void LiesVertragsdaten(XElement transaction, Rechnung invoice)
    {
        var agreement = transaction.Element(Ram + "ApplicableHeaderTradeAgreement");
        if (agreement is null)
        {
            return;
        }

        // Verkäufer (Absender)
        var seller = agreement.Element(Ram + "SellerTradeParty");
        if (seller is not null)
        {
            invoice.AbsenderSnapshot = ParsTradePartyAsAbsender(seller);
        }

        // Käufer (Empfänger)
        var buyer = agreement.Element(Ram + "BuyerTradeParty");
        if (buyer is not null)
        {
            invoice.EmpfaengerSnapshot = ParsTradePartyAsEmpfaenger(buyer);
        }
    }

    private static RechnungsAbsenderSnapshot ParsTradePartyAsAbsender(XElement party)
    {
        var contact = party.Element(Ram + "Contact");

        return new RechnungsAbsenderSnapshot
        {
            Name = LesElementWert(party, Cbc + "Name"),
            Strasse = LiesAdresseStraße(party),
            PLZ = LiesAdressePostleitzahl(party),
            Ort = LiesAdresseOrt(party),
            Land = LiesAdresseLand(party),
            Ansprechpartner = contact is not null
                ? LesElementWert(contact, Cbc + "PersonName")
                : string.Empty,
            Telefon = contact is not null
                ? LesElementWert(contact, Cbc + "TelephoneUniversalCommunication", Cbc + "CompleteNumber")
                : string.Empty,
            Email = contact is not null
                ? LesElementWert(contact, Cbc + "EmailURIUniversalCommunication", Cbc + "URIIdentifier")
                : string.Empty,
            UstIdNr = LesVatId(party),
        };
    }

    private static RechnungsEmpfaengerSnapshot ParsTradePartyAsEmpfaenger(XElement party)
    {
        var contact = party.Element(Ram + "Contact");

        return new RechnungsEmpfaengerSnapshot
        {
            Name = LesElementWert(party, Cbc + "Name"),
            Strasse = LiesAdresseStraße(party),
            PLZ = LiesAdressePostleitzahl(party),
            Ort = LiesAdresseOrt(party),
            Land = LiesAdresseLand(party),
            Ansprechpartner = contact is not null
                ? LesElementWert(contact, Cbc + "PersonName")
                : string.Empty,
            Email = contact is not null
                ? LesElementWert(contact, Cbc + "EmailURIUniversalCommunication", Cbc + "URIIdentifier")
                : string.Empty,
            UstIdNr = LesVatId(party),
        };
    }

    private static void LiesZahlungsbedingungen(XElement transaction, Rechnung invoice)
    {
        var settlement = transaction.Element(Ram + "ApplicableHeaderTradeSettlement");
        if (settlement is null)
        {
            return;
        }

        var paymentTerms = settlement.Element(Ram + "SpecifiedTradePaymentTerms");
        if (paymentTerms is not null)
        {
            var dueDate = paymentTerms.Element(Cbc + "DescriptionDueDate");
            if (dueDate is not null)
            {
                invoice.Faeligkeitsdatum = dueDate.Value.ToDateTimeYyyyMMdd();
            }
        }
    }

    private static void LiesAbrechnungsdaten(XElement transaction, Rechnung invoice)
    {
        var settlement = transaction.Element(Ram + "ApplicableHeaderTradeSettlement");
        if (settlement is null)
        {
            return;
        }

        // Währung
        invoice.Waehrung = LesElementWert(settlement, Cbc + "InvoiceCurrencyCode")
            ?? "EUR";

        // Gesamtsummen
        var summation = settlement.Element(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation");
        if (summation is not null)
        {
            invoice.GesamtbetragNetto = LesDecimal(summation, Cbc + "LineExtensionAmount")
                                ?? LesDecimal(summation, Cbc + "TaxBasisTotalAmount")
                                ?? 0m;

            invoice.UmsatzsteuerBetrag = LesDecimal(summation, Cbc + "TaxTotalAmount") ?? 0m;

            invoice.GesamtbetragBrutto = LesDecimal(summation, Cbc + "GrandTotalAmount") ?? 0m;
        }
    }

    private static void LiesPositionen(XElement transaction, Rechnung invoice)
    {
        invoice.Positionen = transaction
            .Elements(Ram + "IncludedSupplyChainTradeLineItem")
            .Select((lineItem, index) => new RechnungsPosition
            {
                Reihenfolge = index + 1,
                Beschreibung = ParsProduktbeschreibung(lineItem),
                Menge = ParsMenge(lineItem) ?? 1m,
                EinzelpreisNetto = ParsEinzelpreis(lineItem) ?? 0m,
                Steuersatz = ParsSteuersatz(lineItem) ?? 0m,
                Einheit = ParsEinheit(lineItem),
            })
            .ToList();
    }

    private static string ParsProduktbeschreibung(XElement lineItem)
    {
        var product = lineItem.Element(Ram + "SpecifiedTradeProduct");
        return product is not null
            ? LesElementWert(product, Cbc + "Name")
            : string.Empty;
    }

    private static decimal? ParsMenge(XElement lineItem)
    {
        var delivery = lineItem.Element(Ram + "SpecifiedLineTradeDelivery");
        if (delivery is null)
        {
            return null;
        }

        var quantity = delivery.Element(Cbc + "BilledQuantity");
        return quantity is not null ? LesDecimal(quantity) : null;
    }

    private static decimal? ParsEinzelpreis(XElement lineItem)
    {
        var pricing = lineItem.Element(Ram + "SpecifiedLineTradePricing");
        if (pricing is null)
        {
            return null;
        }

        var itemPrice = pricing.Element(Ram + "ItemPriceAmount");
        return itemPrice is not null ? LesDecimal(itemPrice) : null;
    }

    private static decimal? ParsGesamtpreis(XElement lineItem)
    {
        var pricing = lineItem.Element(Ram + "SpecifiedLineTradePricing");
        if (pricing is null)
        {
            return null;
        }

        return LesDecimal(pricing, Cbc + "LineExtensionAmount");
    }

    private static decimal? ParsSteuersatz(XElement lineItem)
    {
        var lineTax = lineItem.Element(Ram + "SpecifiedLineTradeTax");
        if (lineTax is null)
        {
            return null;
        }

        var taxCategory = lineTax.Element(Ram + "TaxCategory");
        if (taxCategory is not null)
        {
            return LesDecimal(taxCategory, Cbc + "Percent");
        }

        return LesDecimal(lineTax, Cbc + "RateApplicablePercent");
    }

    private static string ParsEinheit(XElement lineItem)
    {
        var delivery = lineItem.Element(Ram + "SpecifiedLineTradeDelivery");
        if (delivery is null)
        {
            return "ST";
        }

        var quantity = delivery.Element(Cbc + "BilledQuantity");
        return quantity?.Attribute("unitCode")?.Value ?? "ST";
    }

    // ── Hilfsfunktionen ──────────────────────────────────────────────────────

    private static string LesElementWert(XElement parent, XName name)
    {
        var element = parent.Element(name);
        return element?.Value ?? string.Empty;
    }

    private static string LesElementWert(XElement parent, XName container, XName child)
    {
        var element = parent.Element(container)?.Element(child);
        return element?.Value ?? string.Empty;
    }

    private static DateTime LiesDatum(XElement parent, XName name)
    {
        var element = parent.Element(name);
        if (element is null || string.IsNullOrEmpty(element.Value))
        {
            return DateTime.Today;
        }

        return element.Value.ToDateTimeYyyyMMdd();
    }

    private static string LiesNotenInhalt(XElement exchangedDocument)
    {
        var note = exchangedDocument.Element(Ram + "IncludedNote");
        if (note is null)
        {
            return string.Empty;
        }

        return LesElementWert(note, Cbc + "Content");
    }

    private static string LiesAdresseStraße(XElement party)
    {
        var address = party.Element(Ram + "PostalTradeAddress");
        if (address is null)
        {
            return string.Empty;
        }

        // Basic WL verwendet StreetName, CII nutzt LineOne/LineTwo
        var streetName = address.Element(Cbc + "StreetName");
        if (streetName is not null)
        {
            return streetName.Value;
        }

        return LesElementWert(address, Cbc + "LineOne");
    }

    private static string LiesAdressePostleitzahl(XElement party)
    {
        var address = party.Element(Ram + "PostalTradeAddress");
        return address is not null
            ? LesElementWert(address, Cbc + "PostcodeCode")
            : string.Empty;
    }

    private static string LiesAdresseOrt(XElement party)
    {
        var address = party.Element(Ram + "PostalTradeAddress");
        return address is not null
            ? LesElementWert(address, Cbc + "CityName")
            : string.Empty;
    }

    private static string LiesAdresseLand(XElement party)
    {
        var address = party.Element(Ram + "PostalTradeAddress");
        if (address is null)
        {
            return string.Empty;
        }

        var country = address.Element(Ram + "CountryID");
        return country?.Value ?? string.Empty;
    }

    private static string LesVatId(XElement party)
    {
        var reg = party.Element(Ram + "SpecifiedTaxRegistration");
        if (reg is null)
        {
            return string.Empty;
        }

        var id = reg.Element(Cbc + "ID");
        if (id is null)
        {
            return string.Empty;
        }

        var scheme = id.Attribute("schemeID")?.Value;
        if (scheme != "VA")
        {
            return string.Empty;
        }

        return id.Value;
    }

    private static decimal? LesDecimal(XElement element)
    {
        if (string.IsNullOrEmpty(element.Value))
        {
            return null;
        }

        if (decimal.TryParse(element.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static decimal? LesDecimal(XElement parent, XName name)
    {
        var element = parent.Element(name);
        if (element is null)
        {
            return null;
        }

        return LesDecimal(element);
    }
}

internal static class DateTimeExtensions
{
    internal static DateTime ToDateTimeYyyyMMdd(this string value)
    {
        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }

        throw new FormatException($"Das Datum '{value}' konnte nicht im Format yyyyMMdd geparst werden.");
    }
}