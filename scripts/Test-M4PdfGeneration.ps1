param(
    [string]$OutputDirectory = '',
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot 'local-input\m4-pdf-smoke'
}

$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
$workPath = Join-Path $outputPath '.work'
$projectPath = Join-Path $workPath 'M4PdfSmoke.csproj'
$programPath = Join-Path $workPath 'Program.cs'
$coreProject = Join-Path $repositoryRoot 'src\ERechnung.Core\ERechnung.Core.csproj'
$pdfProject = Join-Path $repositoryRoot 'src\ERechnung.PDF\ERechnung.PDF.csproj'

New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
if (Test-Path $workPath) {
    Remove-Item $workPath -Recurse -Force
}
New-Item -ItemType Directory -Path $workPath -Force | Out-Null

$projectContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$coreProject" />
    <ProjectReference Include="$pdfProject" />
  </ItemGroup>
</Project>
"@
Set-Content -Path $projectPath -Value $projectContent -Encoding UTF8

$programContent = @'
using ERechnung.Core.Models;
using ERechnung.PDF.Generators;

if (args.Length != 1)
{
    throw new ArgumentException("Das Ausgabeverzeichnis fehlt.");
}

var outputPath = Path.GetFullPath(args[0]);
Directory.CreateDirectory(outputPath);
var generator = new QuestPdfRechnungsPdfGenerator();
var erzeugtAm = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

Erzeuge("01-einseitig-ohne-logo.pdf", ErstelleRechnung(1, gemischteSteuer: false, mitLogo: false));
Erzeuge("02-gemischte-steuer-mit-logo.pdf", ErstelleRechnung(2, gemischteSteuer: true, mitLogo: true));
Erzeuge("03-mehrseitig-mit-logo.pdf", ErstelleRechnung(90, gemischteSteuer: true, mitLogo: true));

void Erzeuge(string dateiname, Rechnung rechnung)
{
    var zielpfad = Path.Combine(outputPath, dateiname);
    var pdf = generator.Erzeuge(rechnung, erzeugtAm);
    File.WriteAllBytes(zielpfad, pdf);
    Console.WriteLine($"{dateiname}: {pdf.Length:N0} Bytes");
}

static Rechnung ErstelleRechnung(int anzahlPositionen, bool gemischteSteuer, bool mitLogo)
{
    var rechnung = new Rechnung
    {
        Id = anzahlPositionen,
        Nummer = $"TEST-2026-{anzahlPositionen:000}",
        Titel = "Synthetische Testrechnung",
        Rechnungsdatum = new DateTime(2026, 8, 3),
        Leistungsdatum = new DateTime(2026, 8, 1),
        Faeligkeitsdatum = new DateTime(2026, 8, 17),
        Waehrung = "EUR",
        Bemerkung = "Diese PDF enthaelt ausschliesslich frei erfundene Testdaten.",
        GeaendertAm = new DateTime(2026, 8, 3, 11, 30, 0, DateTimeKind.Utc),
        AbsenderSnapshot = new RechnungsAbsenderSnapshot
        {
            QuellId = 1,
            Name = "Synthetische Beispiel GmbH",
            Ansprechpartner = "Erika Beispiel",
            Strasse = "Testweg 1",
            PLZ = "10115",
            Ort = "Berlin",
            Land = "DE",
            Email = "rechnung@example.invalid",
            Telefon = "+49 30 000000",
            UstIdNr = "DE123456789",
            IBAN = "DE02120300000000202051",
            BIC = "BYLADEM1001",
            LogoInhalt = mitLogo
                ? Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=")
                : null,
            LogoMedientyp = mitLogo ? "image/png" : null
        },
        EmpfaengerSnapshot = new RechnungsEmpfaengerSnapshot
        {
            QuellId = 2,
            Name = "Synthetischer Testkunde AG",
            Ansprechpartner = "Max Muster",
            Strasse = "Pruefstrasse 2",
            PLZ = "20095",
            Ort = "Hamburg",
            Land = "DE",
            Email = "empfang@example.invalid",
            UstIdNr = "DE987654321"
        }
    };

    rechnung.Positionen = Enumerable.Range(1, anzahlPositionen)
        .Select(index => new RechnungsPosition
        {
            Reihenfolge = index,
            Beschreibung = anzahlPositionen > 10
                ? $"Synthetische Leistungsposition {index}: ausfuehrliche Beschreibung fuer den Mehrseitentest"
                : $"Synthetische Leistungsposition {index}",
            Menge = index % 3 == 0 ? 2.5m : 1m,
            Einheit = "Std.",
            EinzelpreisNetto = 25m + index,
            Steuersatz = gemischteSteuer && index % 2 == 0 ? 7m : 19m
        })
        .ToList();

    return rechnung;
}
'@
Set-Content -Path $programPath -Value $programContent -Encoding UTF8

$expectedFiles = @(
    '01-einseitig-ohne-logo.pdf',
    '02-gemischte-steuer-mit-logo.pdf',
    '03-mehrseitig-mit-logo.pdf'
)
foreach ($fileName in $expectedFiles) {
    $filePath = Join-Path $outputPath $fileName
    if (Test-Path $filePath) {
        Remove-Item $filePath -Force
    }
}

if ($SelfContained) {
    $publishPath = Join-Path $workPath 'publish'
    & dotnet publish $projectPath --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output $publishPath
    if ($LASTEXITCODE -ne 0) {
        throw "Der selbstenthaltende PDF-Smoke-Build ist mit Exitcode $LASTEXITCODE fehlgeschlagen."
    }

    $executablePath = Join-Path $publishPath 'M4PdfSmoke.exe'
    & $executablePath $outputPath
}
else {
    & dotnet run --project $projectPath --configuration Release -- $outputPath
}

if ($LASTEXITCODE -ne 0) {
    throw "Die PDF-Erzeugung ist mit Exitcode $LASTEXITCODE fehlgeschlagen."
}

foreach ($fileName in $expectedFiles) {
    $filePath = Join-Path $outputPath $fileName
    if (-not (Test-Path $filePath)) {
        throw "Die erwartete PDF wurde nicht erzeugt: $filePath"
    }

    $bytes = [System.IO.File]::ReadAllBytes($filePath)
    if ($bytes.Length -lt 5000) {
        throw "Die erzeugte PDF ist unerwartet klein: $filePath ($($bytes.Length) Bytes)"
    }

    $signature = [System.Text.Encoding]::ASCII.GetString($bytes, 0, 5)
    if ($signature -ne '%PDF-') {
        throw "Die erzeugte Datei besitzt keine PDF-Signatur: $filePath"
    }
}

Write-Output "M4-PDF-Smoke-Test erfolgreich. Synthetische Pruefdateien:"
$expectedFiles | ForEach-Object { Write-Output "- $(Join-Path $outputPath $_)" }
