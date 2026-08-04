using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

/// <summary>
/// Generiert EN 16931 konformes UBL 2.2 XML aus einer Rechnung.
/// </summary>
public interface IUblGenerator
{
    /// <summary>
    /// Generiert den UBL 2.2 XML als Zeichenkette.
    /// </summary>
    string Generate(Rechnung rechnung);
}