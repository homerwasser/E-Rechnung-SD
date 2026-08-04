using System.IO;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.App.Services;

public sealed class LocalLogoSnapshotLoader : ILogoSnapshotLoader
{
    public const int MaximaleDateigroesse = 2 * 1024 * 1024;

    public LogoSnapshotDaten? Lade(string logoPfad)
    {
        if (string.IsNullOrWhiteSpace(logoPfad))
        {
            return null;
        }

        try
        {
            if (!Path.IsPathFullyQualified(logoPfad))
            {
                return null;
            }

            var vollstaendigerPfad = Path.GetFullPath(logoPfad);
            if (IstNetzwerkpfad(vollstaendigerPfad))
            {
                return null;
            }

            var datei = new FileInfo(vollstaendigerPfad);
            if (!datei.Exists
                || datei.Length is <= 0 or > MaximaleDateigroesse
                || (datei.Attributes & (FileAttributes.Directory | FileAttributes.Device | FileAttributes.ReparsePoint)) != 0)
            {
                return null;
            }

            using var stream = new FileStream(
                vollstaendigerPfad,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.SequentialScan);

            if (stream.Length is <= 0 or > MaximaleDateigroesse)
            {
                return null;
            }

            var inhalt = new byte[checked((int)stream.Length)];
            stream.ReadExactly(inhalt);
            var medientyp = ErmittleMedientyp(inhalt);
            return medientyp is null ? null : new LogoSnapshotDaten(inhalt, medientyp);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IstNetzwerkpfad(string pfad)
    {
        if (pfad.StartsWith("\\\\", StringComparison.Ordinal)
            || pfad.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        var wurzel = Path.GetPathRoot(pfad);
        if (string.IsNullOrWhiteSpace(wurzel))
        {
            return true;
        }

        try
        {
            return new DriveInfo(wurzel).DriveType == DriveType.Network;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static string? ErmittleMedientyp(ReadOnlySpan<byte> inhalt)
    {
        if (inhalt.Length >= 8
            && inhalt[0] == 0x89
            && inhalt[1] == 0x50
            && inhalt[2] == 0x4E
            && inhalt[3] == 0x47
            && inhalt[4] == 0x0D
            && inhalt[5] == 0x0A
            && inhalt[6] == 0x1A
            && inhalt[7] == 0x0A)
        {
            return "image/png";
        }

        if (inhalt.Length >= 3
            && inhalt[0] == 0xFF
            && inhalt[1] == 0xD8
            && inhalt[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (inhalt.Length >= 12
            && inhalt[..4].SequenceEqual("RIFF"u8)
            && inhalt.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        if (inhalt.StartsWith("BM"u8))
        {
            return "image/bmp";
        }

        return null;
    }
}
