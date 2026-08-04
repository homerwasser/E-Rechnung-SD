using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

public interface IRechnungsPdfGenerator
{
    byte[] Erzeuge(Rechnung rechnung, DateTimeOffset erzeugtAm);
}
