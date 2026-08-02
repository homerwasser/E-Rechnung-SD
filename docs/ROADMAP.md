# Roadmap – E-Rechnung SD

GitHub ist die maßgebliche Quelle für Issues und Meilensteine:
<https://github.com/homerwasser/E-Rechnung-SD/milestones>

## Statusübersicht

| Meilenstein | Inhalt | Status |
|---|---|---|
| M1 | Projekt-Setup und Grundgerüst | Implementiert; GitHub-Abschluss noch zu bereinigen |
| M2 | Datenbank und Kundenverwaltung | In Umsetzung / Abnahme |
| M3 | Rechnungserstellung (Kern) | Wartet auf M2-Abnahme |
| M4 | PDF-Generierung und E-Mail | Geplant |
| M5 | E-Rechnung: EN 16931, UBL/XRechnung und Factur-X/ZUGFeRD | Geplant |
| M6 | Vorlagen und Einstellungen | Geplant |
| M7 | Backup, Update und Veröffentlichung | Geplant |

## M1 – Projekt-Setup

- #1 Solution-Grundgerüst
- #2 CI/CD-Pipeline
- #3 Git-Branching-Strategie
- #4 Erste WPF-Anwendung
- #5 Test-Projekt

## M2 – Datenbank und Kundenverwaltung

- #6 SQLite-Integration mit Dapper
- #7 Versioniertes Migrationssystem
- #8 Kunden-Repository mit CRUD
- #9 Kunden-ViewModel mit Suche und Validierung
- #10 Kundenverwaltung mit Liste und Formular
- #11 Integrations- und Repository-Tests

### M2-Abnahmekriterien

- Datenbank liegt unter `%LOCALAPPDATA%\ERechnung-SD\data\erechnung.db`.
- Migrationen sind versioniert, transaktional und wiederholt ausführbar.
- Kunden können angelegt, angezeigt, gesucht, bearbeitet und gelöscht werden.
- Pflichtfelder und E-Mail-Adresse werden validiert.
- Repository und Migrationen sind mit temporären SQLite-Dateien getestet.
- Anwendung öffnet genau ein Hauptfenster.
- Build, Tests, manueller Test und GitHub-CI sind erfolgreich.

## M3 – Rechnungserstellung

Start erst nach bestätigter M2-Abnahme. Geplant sind insbesondere:

- Rechnung und Positionen transaktional speichern
- Firmenprofil und Kunde auswählen
- flexible Kostenpositionen
- automatische Rechnungsnummer
- Statusmodell und Statusfilter
- Berechnung und Validierung testen

## Spätere Verbesserungen

- #35 Automatischer Dunkel-/Hellmodus mit manueller Auswahl
- Forderungsanalyse im Dashboard
- Backup und Wiederherstellung
- automatische Updates über signierte GitHub-Releases

## Statuswerte für Rechnungen

- `erstellt`
- `versendet`
- `offen`
- `bezahlt`
- `inklaerung`
- `storniert`
