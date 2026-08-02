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
- automatisierte Build-, Unit- und Integrationstests

### Geplant

- Rechnungserstellung mit flexiblen Kostenpositionen
- PDF-Ausgabe
- EN-16931-konformer XML-Export und -Import
- Factur-X/ZUGFeRD und XRechnung
- Outlook-Integration
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
| XML (E-Rechnung)  | UBL 2.2 / Factur-X |

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

# Veröffentlichen (Self-Contained, SingleFile)
dotnet publish -c Release -r win-x64 --self-contained
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