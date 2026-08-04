# E-Rechnung SD – Systemarchitektur

## Zielplattform
- Windows 10 und Windows 11
- **Keine zusätzlichen Installationen** für den Endbenutzer
- Einzelne `.exe` (Self-Contained Publish)

## Technologie-Stack

| Komponente         | Technologie                    | Gründe                                              |
|--------------------|--------------------------------|-----------------------------------------------------|
| Sprache            | C# (.NET 8)                   | Modern, kostenlos, Open Source                      |
| UI                 | WPF (XAML/C#)                 | Reife Desktop-GUI, MVVM-Pattern, Datenbindung      |
| Datenbank          | SQLite                        | Datei-basiert, getrennt vom Programm, einfache Backup |
| ORM                | Dapper                        | Leichtgewichtig, performant                         |
| XML-Parser         | `System.Xml.Linq`             | Eingebaut, für UBL 2.2 und Factur-X/CII in M5      |
| PDF-Generierung    | QuestPDF                      | PDF/A-3B, moderne Layout-API, Community-Lizenz     |
| E-Mail-Entwurf     | Outlook COM + RFC-6068-`mailto:` | Anhang in klassischem Outlook, sicherer Fallback |
| Updaten            | Eigenes Update-Service        | Prüft GitHub Release-API, lädt neue .exe herunter  |
| Build & Test       | dotnet CLI + xUnit            | Standard .NET-Ökosystem                             |
| IDE                | VS Code + C# Dev Kit          | Kostenlos, reicht für komplette Entwicklung         |
| Versionskontrolle  | Git + GitHub                  | Öffentliches Repository, CI/CD; keine Echtdaten     |

## Komponentenübersicht

```
┌─────────────────────────────────────────────────────────────┐
│                       WPF Frontend                          │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────────┐   │
│  │ Dashboard   │  │ Editor/      │  │ Kundenverwaltung  │   │
│  │             │  │ Formular     │  │                   │   │
│  └─────────────┘  └──────────────┘  └───────────────────┘   │
│                                                               │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────────┐   │
│  │ Vorlagen-   │  │ Backup/      │  │ Einstellungen &   │   │
│  │ Manager     │  │ Wiederherst. │  │ Update            │   │
│  └─────────────┘  └──────────────┘  └───────────────────┘   │
└──────────────────────────┬──────────────────────────────────┘
                           │ MVVM (DataBinding)
┌──────────────────────────▼──────────────────────────────────┐
│                     Businesslogik (Core)                    │
│  ┌────────────┐  ┌─────────────┐  ┌─────────────────────┐   │
│  │ Rechnung   │  │ Kunde       │  │ Vorlage             │   │
│  │ Service    │  │ Service     │  │ Service             │   │
│  └────────────┘  └─────────────┘  └─────────────────────┘   │
│                                                               │
│  ┌────────────┐  ┌─────────────┐  ┌─────────────────────┐   │
│  │ UBL 2.2    │  │ Factur-X    │  │ PDF Generator       │   │
│  │ Generator  │  │ Embedder    │  │                     │   │
│  └────────────┘  └─────────────┘  └─────────────────────┘   │
└──────────────────────────┬──────────────────────────────────┘
                           │ Dapper / LINQ
┌──────────────────────────▼──────────────────────────────────┐
│                      Datenspeicher                          │
│                                                              │
│  ┌────────────────────────────────────────────────────┐      │
│  │  SQLite (Datei: erechnung.db)                       │      │
│  │  - Rechnungen                                      │      │
│  │  - Kunden                                          │      │
│  │  - Vorlagen                                        │      │
│  │  - Einstellungen                                   │      │
│  └────────────────────────────────────────────────────┘      │
│                                                              │
│  ┌────────────────────────────────────────────────────┐      │
│  │  PDF-Ablage (%LOCALAPPDATA%/ERechnung-SD/pdf/)    │      │
│  │  - versionierte PDF/A-3B-Rechnungen                │      │
│  │  - relative Verknüpfungen in SQLite                │      │
│  └────────────────────────────────────────────────────┘      │
└──────────────────────────────────────────────────────────────┘
```

## Paket-Struktur (Solution)

```
E-Rechnung-SD.sln
├── src/
│   ├── ERechnung.App/            # WPF-Anwendung (Frontend)
│   │   ├── Views/                # XAML-Views
│   │   ├── ViewModels/           # MVVM ViewModels
│   │   ├── Controls/             # Benutzerdefinierte Steuerelemente
│   │   ├── Resources/            # XAML-Stile, Icons
│   │   └── Converters/           # WPF ValueConverters
│   │
│   ├── ERechnung.Core/           # Businesslogik
│   │   ├── Domain/               # Entitäten, Regeln
│   │   ├── Services/             # Services & Interfaces
│   │   ├── DTOs/                 # Data Transfer Objects
│   │   └── Exceptions/           # DomainExceptions
│   │
│   ├── ERechnung.Data/           # Datenbankzugriff
│   │   ├── Repositories/         # DAO / Repositories
│   │   ├── Migrations/           # Schema-Migrationen
│   │   ├── Backup/               # Backup/Restore-Logik
│   │   └── Settings/             | Config-Laden/Speichern
│   │
│   ├── ERechnung.XML/            | XML-Export/Import
│   │   ├── Generators/           # UBL 2.2 Generierung
│   │   ├── Parsers/              # UBL-Import
│   │   └── Schemas/              | XSD-Schemata
│   │
│   └── ERechnung.PDF/            # PDF-Generierung
│       └── Generators/           # QuestPDF-Layout und PDF/A-3B-Ausgabe
│
├── tests/
│   ├── ERechnung.Tests.Unit/     # reine Geschäftslogik (xUnit)
│   ├── ERechnung.Tests.Integration/ # SQLite-Integrationstests
│   └── ERechnung.Tests.App/      # WPF-ViewModel-Tests
│
├── build/                        | Build-Skripte
├── .github/                      # CI/CD, Templates
└── docs/                         # Diese Dokumentation
```

## Datenbankschema (Übersicht)

Siehe `DATABASE_SCHEMA.md` für Details.

## Kommunikationswege

| Vom ... | Zum ...                | Mechanismus                   |
|----------|------------------------|-------------------------------|
| View → ViewModel | Command Pattern (RelayCommand) |
| ViewModel → Service | Konstruktorinjektion im Composition Root |
| Service → Repository | Injizierte IRepositories |
| Repository → SQLite | Dapper + ConnectionString |
| ViewModel → PDF-Ablage | relative, geprüfte Pfade unter `%LOCALAPPDATA%` |
| ViewModel → klassisches Outlook | COM-Automation mit `Display(false)`, niemals `Send` |
| ViewModel → Standard-Mail-Client | RFC-6068-`mailto:` ohne Anhang; manuelles Anhängen |
| Update Service → GitHub | HttpClient auf releases/latest |

## Sicherheit

- SQLite-Datenbank in `%LOCALAPPDATA%\ERechnung-SD\data\` (benutzerprivat)
- Rechnungs-PDFs in `%LOCALAPPDATA%\ERechnung-SD\pdf\<Jahr>\`
- technische PDF-Dateinamen ohne Kundennamen und mit eindeutiger Versionskennung
- Pfadvalidierung gegen Traversal, absolute Pfade und Reparse-Points
- Backups in `%LOCALAPPDATA%\ERechnung-SD\backups\`
- Keine sensiblen Daten in Logs
- Verschlüsselung der DB optional per SQLCipher

## Performance-Anforderungen

| Metrik                  | Ziel          |
|-------------------------|---------------|
| Startzeit (kalter Cache)| < 2 Sekunden  |
| PDF-Generierung         | < 1 Sekunde   |
| Datenbank-Operation     | < 100 ms      |
| UI-FPS                  | 60 FPS        |
| Speicherverbrauch       | < 200 MB      |
| Ausgabegröße (.exe)     | < 100 MB      |