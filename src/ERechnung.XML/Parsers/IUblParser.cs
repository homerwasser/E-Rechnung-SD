using ERechnung.Core.Models;

namespace ERechnung.XML.Parsers;

/// <summary>
/// Parst UBL 2.2 CII XML aus einer E-Rechnung zurück in ein <see cref="Rechnung"/>-Objekt.
/// Stützt sich auf die vom <see cref="Generators.IUblGenerator"/> erzeugte Struktur (Basic WL).
/// </summary>
public interface IUblParser
{
    /// <summary>
    /// Parst den UBL-XML-Text und gibt ein neues <see cref="Rechnung"/>-Objekt zurück.
    /// Das Objekt besitzt keine Datenbank-ID – es muss zuerst mit <c>RechnungService.SpeichernAsync()</c> persistiert werden.
    /// </summary>
    Rechnung Parse(string xmlText);
}