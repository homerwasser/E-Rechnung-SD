using System.Diagnostics;
using ERechnung.Core.Models;
using ERechnung.Data.Pdf;

namespace ERechnung.Tests.Integration;

public sealed class LocalRechnungsPdfAblageTests
{
    [Fact]
    public async Task SpeichereAsync_WithValidPdf_UsesTechnicalPathAndSupportsManagedLifecycle()
    {
        using var directory = new TemporaryDirectory();
        var ablage = new LocalRechnungsPdfAblage(directory.Path);
        var rechnung = CreateStoredInvoice();
        var ersteVersion = "%PDF-1.7\nSynthetische Version 1"u8.ToArray();
        var zweiteVersion = "%PDF-1.7\nSynthetische Version 2"u8.ToArray();

        var relativerPfad = await ablage.SpeichereAsync(
            rechnung,
            ersteVersion,
            CancellationToken.None);

        Assert.Matches(
            $"^2026/rechnung-42-2026-001-{rechnung.GeaendertAm.Ticks}-[0-9a-f]{{32}}\\.pdf$",
            relativerPfad);
        Assert.False(Path.IsPathRooted(relativerPfad));
        Assert.DoesNotContain("Kunde", relativerPfad, StringComparison.OrdinalIgnoreCase);
        Assert.True(ablage.Existiert(relativerPfad));
        var vollstaendigerPfad = ablage.LoeseVollstaendigenPfadAuf(relativerPfad);
        Assert.StartsWith(
            Path.GetFullPath(directory.Path),
            vollstaendigerPfad,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
        Assert.Equal(ersteVersion, await File.ReadAllBytesAsync(vollstaendigerPfad));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp", SearchOption.AllDirectories));
        Assert.Single(Directory.EnumerateFiles(directory.Path, "*.pdf", SearchOption.AllDirectories));

        var ersetzterPfad = await ablage.SpeichereAsync(
            rechnung,
            zweiteVersion,
            CancellationToken.None);

        Assert.NotEqual(relativerPfad, ersetzterPfad);
        Assert.Equal(ersteVersion, await File.ReadAllBytesAsync(vollstaendigerPfad));
        Assert.Equal(
            zweiteVersion,
            await File.ReadAllBytesAsync(ablage.LoeseVollstaendigenPfadAuf(ersetzterPfad)));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp", SearchOption.AllDirectories));
        Assert.Equal(2, Directory.EnumerateFiles(directory.Path, "*.pdf", SearchOption.AllDirectories).Count());

        await ablage.LoescheAsync(relativerPfad, CancellationToken.None);
        await ablage.LoescheAsync(ersetzterPfad, CancellationToken.None);
        Assert.False(ablage.Existiert(relativerPfad));
        Assert.False(ablage.Existiert(ersetzterPfad));
        await ablage.LoescheAsync(relativerPfad, CancellationToken.None);
    }

    [Fact]
    public async Task ManagedOperations_WithTraversalOrRootedPaths_RejectWithoutTouchingForeignFile()
    {
        using var directory = new TemporaryDirectory();
        var ablage = new LocalRechnungsPdfAblage(directory.Path);
        var fremderDateiname = $"fremd-{Guid.NewGuid():N}.pdf";
        var fremderPfad = Path.GetFullPath(
            Path.Combine(directory.Path, "..", fremderDateiname));
        await File.WriteAllBytesAsync(fremderPfad, "%PDF-fremd"u8.ToArray());

        try
        {
            var unzulaessigePfade = new[]
            {
                $"../{fremderDateiname}",
                $"2026/../../{fremderDateiname}",
                fremderPfad,
                Path.DirectorySeparatorChar + fremderDateiname
            };

            foreach (var pfad in unzulaessigePfade)
            {
                Assert.Throws<ArgumentException>(
                    () => ablage.LoeseVollstaendigenPfadAuf(pfad));
                Assert.Throws<ArgumentException>(() => ablage.Existiert(pfad));
                await Assert.ThrowsAsync<ArgumentException>(
                    () => ablage.LoescheAsync(pfad, CancellationToken.None));
            }

            Assert.True(File.Exists(fremderPfad));
            Assert.Equal("%PDF-fremd"u8.ToArray(), await File.ReadAllBytesAsync(fremderPfad));
        }
        finally
        {
            File.Delete(fremderPfad);
        }
    }

    [Fact]
    public async Task Constructor_WithJunctionInParentPath_RejectsRedirectedStorage()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var directory = new TemporaryDirectory();
        using var targetDirectory = new TemporaryDirectory();
        var junctionPath = Path.Combine(directory.Path, "weiterleitung");
        var startInfo = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add("mklink");
        startInfo.ArgumentList.Add("/J");
        startInfo.ArgumentList.Add(junctionPath);
        startInfo.ArgumentList.Add(targetDirectory.Path);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Der Junction-Testprozess konnte nicht gestartet werden.");
        await process.WaitForExitAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        Assert.True(
            process.ExitCode == 0,
            $"Die Test-Junction konnte nicht erstellt werden. Exitcode: {process.ExitCode}. {standardError}");

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => new LocalRechnungsPdfAblage(Path.Combine(junctionPath, "pdf")));
            Assert.Contains("Verzeichnisverknüpfungen", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(junctionPath))
            {
                Directory.Delete(junctionPath);
            }
        }
    }

    [Fact]
    public async Task SpeichereAsync_RequiresStoredInvoiceNumberAndPdfSignature()
    {
        using var directory = new TemporaryDirectory();
        var ablage = new LocalRechnungsPdfAblage(directory.Path);
        var rechnung = CreateStoredInvoice();
        var pdf = "%PDF-1.7\nSynthetisch"u8.ToArray();

        rechnung.Id = null;
        var idException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ablage.SpeichereAsync(rechnung, pdf, CancellationToken.None));
        Assert.Contains("ID", idException.Message, StringComparison.Ordinal);

        rechnung.Id = 42;
        rechnung.Nummer = "   ";
        var nummerException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ablage.SpeichereAsync(rechnung, pdf, CancellationToken.None));
        Assert.Contains("Rechnungsnummer", nummerException.Message, StringComparison.Ordinal);

        rechnung.Nummer = "2026-001";
        var signaturException = await Assert.ThrowsAsync<ArgumentException>(
            () => ablage.SpeichereAsync(
                rechnung,
                "Kein PDF"u8.ToArray(),
                CancellationToken.None));
        Assert.Contains("%PDF-", signaturException.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SpeichereAsync_WhenCancelled_LeavesNoTemporaryOrFinalFile()
    {
        using var directory = new TemporaryDirectory();
        var ablage = new LocalRechnungsPdfAblage(directory.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ablage.SpeichereAsync(
                CreateStoredInvoice(),
                "%PDF-1.7\nSynthetisch"u8.ToArray(),
                cancellation.Token));

        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void Constructor_ValidatesInjectedBaseDirectory()
    {
        Assert.Throws<ArgumentException>(() => new LocalRechnungsPdfAblage(string.Empty));

        using var directory = new TemporaryDirectory();
        var filePath = Path.Combine(directory.Path, "keine-ablage.txt");
        File.WriteAllText(filePath, "Synthetisch");

        var exception = Assert.Throws<ArgumentException>(
            () => new LocalRechnungsPdfAblage(filePath));
        Assert.Contains("Datei", exception.Message, StringComparison.Ordinal);
    }

    private static Rechnung CreateStoredInvoice()
    {
        return new Rechnung
        {
            Id = 42,
            Nummer = "2026 / 001",
            Rechnungsdatum = new DateTime(2026, 8, 3),
            GeaendertAm = new DateTime(2026, 8, 3, 10, 15, 0, DateTimeKind.Utc)
        };
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ERechnung.Tests.Integration.Pdf",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
