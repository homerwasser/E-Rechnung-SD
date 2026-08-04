using System.Diagnostics;

namespace ERechnung.App.Services;

public interface IProzessStarter
{
    void Starte(ProcessStartInfo startInfo);
}

public sealed class SystemProzessStarter : IProzessStarter
{
    public void Starte(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        Process.Start(startInfo);
    }
}
