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
| XML-Parser         | `System.Xml.Linq`             | Eingebaut, ideal für UBL 2.2 / Factur-X            |
| PDF-Generierung    | QuestPDF                      | Open-Source, moderne API, Factur-X Embedding       |
| E-Mail-Sendung     | `System.Diagnostics.Process`  | Öffnet Outlook (Standard-Mail-Client)              |
| Updaten            | Eigenes Update-Service        | Prüft GitHub Release-API, lädt neue .exe herunter  |
| Build & Test       | dotnet CLI + xUnit            | Standard .NET-Ökosystem                             |
| IDE                | VS Code + C# Dev Kit          | Kostenlos, reicht für komplette Entwicklung         |
| Versionskontrolle  | Git + GitHub                  | Private Repository, CI/CD                           |

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
│  │  Backup-Ordner (%APPDATA%/ERechnung/backups/)     │      │
│  │  - Tägliches Backup (.db.bak)                      │      │
│  │  - Manuelle Sicherungen                            │      │
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
│       ├── FacturXEmbedder/      # Factur-X in PDF einbetten
│       ├── VorlagenRenderer/     # Vorlage-basierter Renderer
│       └── Layout/               | PDF-Layouts
│
├── tests/
│   ├── ERechnung.Tests.Unit/     # Unit Tests (xUnit)
│   └── ERechnung.Tests.Integration/ # Integration Tests
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
| ViewModel → Service | Injizierte Interfaces (DI-Container) |
| Service → Repository | Injizierte IRepositories |
| Repository → SQLite | Dapper + ConnectionString |
| PDF Generator → Outlook | Process.Start(mailto: ...) |
| Update Service → GitHub | HttpClient auf releases/latest |

## Sicherheit

- SQLite-Datenbank in `%LOCALAPPDATA%\ERechnung-SD\data\` (benutzerprivat)
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