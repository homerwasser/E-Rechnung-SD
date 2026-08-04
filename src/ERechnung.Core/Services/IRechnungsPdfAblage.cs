using ERechnung.Core.Models;

namespace ERechnung.Core.Services;

public interface IRechnungsPdfAblage
{
    Task<string> SpeichereAsync(
        Rechnung rechnung,
        ReadOnlyMemory<byte> pdfInhalt,
        CancellationToken cancellationToken);

    bool Existiert(string relativerPfad);
    string LoeseVollstaendigenPfadAuf(string relativerPfad);
    Task LoescheAsync(string relativerPfad, CancellationToken cancellationToken);
}
