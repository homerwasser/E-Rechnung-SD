# E-Rechnung SD

**Elektronische Rechnungen nach dem deutschen Standard (EN 16931) – erstellen, verwalten, verschicken.**

![Status](https://img.shields.io/badge/Status-Development-blue)
![License](https://img.shields.io/badge/License-All%20rights%20reserved-red)
![Platform](https://img.shields.io/badge/Plattform-Windows%2010%2F11-lightgrey)

---

## Was ist das?

Eine Windows-Desktop-Anwendung zur Erstellung und Verwaltung elektronischer Rechnungen.

### Bereits umgesetzt

- WPF-Grundanwendung für Windows 10/11
- lokale SQLite-Datenbank außerhalb des Programmordners
- versionierte Datenbankmigrationen
- Kundenverwaltung mit Suche und CRUD-Funktionen
- Verwaltung mehrerer Firmen- und Absenderprofile
- Rechnungserstellung mit flexiblen Positionen und Live-Summen
- automatische jährliche Rechnungsnummern
- Rechnungsübersicht mit Statusänderung und Statusfilter
- historische Stammdaten-Snapshots pro Rechnung einschließlich Logo
- Leistungs-/Veranstaltungsdatum im Rechnungseditor
- PDF/A-3B-Ausgabe mit deutschem Rechnungsformat, Steuergruppen und Mehrseitenlayout
- versionierte lokale PDF-Ablage mit Statusanzeige, Öffnen und Anzeige im Explorer
- interaktive E-Mail-Entwürfe: mit PDF-Anhang in klassischem Outlook oder sicherer `mailto:`-Fallback ohne Anhang
- automatisierte Build-, Unit-, ViewModel-, SQLite-Integrations- und PDF-Smoke-Tests

### Geplant

- EN-16931-konformer XML-Export und -Import
- Factur-X/ZUGFeRD und XRechnung
- Vorlagen und Einstellungen
- Backup, Wiederherstellung und automatische Updates

---

## Schnellstart

Aktuell existiert noch kein Endbenutzer-Release. Entwickler können die Anwendung so starten:

```powershell
dotnet run --project src/ERechnung.App
```

Eine installationsfreie, selbstenthaltende Windows-Version wird in einem späteren Release bereitgestellt.

---

## Technologie

| Komponente        | Lösung              |
|-------------------|---------------------|
| Sprache           | C# (.NET 8)        |
| UI                | WPF (Desktop)      |
| Datenbank         | SQLite             |
| PDF               | QuestPDF           |
| XML (E-Rechnung)  | UBL 2.2 / CII (M5) |

---

## Screenshots

*(Wird mit ersten Builds aktualisiert)*

---

## Entwickler

### Voraussetzungen

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [VS Code](https://code.visualstudio.com/) + [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

### Build & Test

```bash
# Projekt wiederherstellen
dotnet restore

# Bauen
dotnet build E-Rechnung-SD.sln --configuration Release

# Tests ausführen
dotnet test E-Rechnung-SD.sln --configuration Release --no-build

# Synthetische M4-PDFs erzeugen (Ausgabe nur unter local-input/)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-M4PdfGeneration.ps1

# PDF-Erzeugung als Self-Contained win-x64 prüfen
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-M4PdfGeneration.ps1 -SelfContained

# Anwendung veröffentlichen (Self-Contained, SingleFile)
dotnet publish src/ERechnung.App/ERechnung.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Projektstruktur

Siehe [ARCHITECTURE.md](docs/ARCHITECTURE.md)

---

## Roadmap

Siehe [ROADMAP.md](docs/ROADMAP.md)

---

## Changelog

Siehe [CHANGELOG.md](CHANGELOG.md)

---

## Datenschutz und Lizenz

Das Repository ist öffentlich, enthält aber keine Freigabe zur Weiterverwendung. Sofern keine separate Lizenzdatei ergänzt wird, bleiben alle Rechte vorbehalten.

Reale Rechnungen, Kundendaten und Firmenlogos dürfen nicht committed werden. Siehe [Datenschutz bei der Entwicklung](docs/PRIVACY.md).

© 2026 Homer Wasser