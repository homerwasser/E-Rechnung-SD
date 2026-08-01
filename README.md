# E-Rechnung SD

**Elektronische Rechnungen nach dem deutschen Standard (EN 16931) – erstellen, verwalten, verschicken.**

![Status](https://img.shields.io/badge/Status-Development-blue)
![License](https://img.shields.io/badge/License-Private-red)
![Platform](https://img.shields.io/badge/Plattform-Windows%2010%2F11-lightgrey)

---

## Was ist das?

Eine Desktop-Anwendung für Windows, mit der du:

- **Rechnungen erzeugen** kannst – als PDF, als UBL 2.2-XML und als Factur-X (hybrid PDF mit eingebettetem XML)
- **Rechnungen importieren** kannst – aus UBL/Factur-X zurück in die Datenbank
- **Kunden verwalten** kannst – wiederkehrende Empfänger in einem Kundenstamm
- **Vorlagen nutzen** kannst – verschiedene Rechnungstypen (Tagespauschalen, Projekte, etc.)
- **Rechnungen per E-Mail** verschicken kannst – direkt aus der App heraus via Outlook
- **Backups erstellen** kannst – automatische und manuelle Sicherung deiner Daten
- **Automatisch aktualisieren** kannst – das Programm überprüft selbständig nach neuen Versionen

---

## Schnellstart (Benutzer)

### Installation

1. Gehe zu [Releases](https://github.com/homerwasser/E-Rechnung-SD/releases)
2. Lade die neueste `ERechnung.exe` herunter
3. Doppelklick auf die `.exe` – fertig!

**Kein Installer. Keine Installation. Kein Admin-Recht.**

### Erste Rechnung erstellen

1. App starten
2. **„Mein Unternehmen"** in den Einstellungen eintragen
3. **„Neue Rechnung"** klicken
4. Kunde auswählen oder neuen anlegen
5. Kostenzeilen eingeben
6. **„Speichern"** → PDF wird generiert
7. **„Senden"** → Outlook öffnet sich mit dem PDF im Anhang

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
dotnet build

# Tests ausführen
dotnet test

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

## Lizenz

Private Software. Nicht für die Weitergabe gedacht.

© 2024 Homer Wasser