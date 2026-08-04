# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-beta]

### Added
- Automatisierter WPF-Start-Smoke-Test
- Versioniertes und transaktionales SQLite-Migrationssystem
- Vollständige Kundenverwaltung mit Suche, Anlegen, Bearbeiten und Löschen
- Validierung für Firmenname, E-Mail, Land und Postleitzahl
- Unit- und SQLite-Integrationstests für M2
- Firmenprofilverwaltung mit vollständigem CRUD
- Rechnungsübersicht und Rechnungseditor mit flexiblen Positionen
- Live-Berechnung von Netto, Umsatzsteuer und Brutto
- automatische, transaktionale Rechnungsnummern pro Kalenderjahr
- historische Empfänger- und Absender-Snapshots
- Statusänderung und Filterung für alle definierten Rechnungsstatus
- ViewModel-, Geschäftslogik-, Migrations- und Master/Detail-Integrationstests für M3
- Datenschutzregeln für echte Rechnungen und lokale Vorlagendaten
- Leistungs-/Veranstaltungsdatum in Rechnung, Persistenz und PDF
- QuestPDF-basierte, komprimierte PDF/A-3B-Ausgabe mit Logo, Steuergruppen und Mehrseitenlayout
- versionierte lokale PDF-Ablage mit Aktualitätsstatus, Öffnen und Explorer-Integration
- interaktive E-Mail-Entwürfe mit Outlook-Anhang und sicherem `mailto:`-Fallback ohne Anhang
- synthetischer PDF-Smoke-Test einschließlich selbstenthaltender `win-x64`-Ausführung

### Changed
- Datenbank nach `%LOCALAPPDATA%\ERechnung-SD\data\` verschoben
- Kunden-Repository liefert erzeugte IDs zurück und prüft Schreiboperationen
- Roadmap und KI-Arbeitsablauf an verbindliche Abnahmekriterien angepasst
- Test- und SQLite-Abhängigkeiten auf sichere Patchstände aktualisiert
- Hauptnavigation auf MVVM-basierte Ansichten umgestellt
- Rechnungsschreibvorgänge als atomare Master/Detail-Transaktionen umgesetzt
- Optimistische Parallelitätsprüfung für Rechnungsänderungen ergänzt
- Ungespeicherte Rechnungsentwürfe vor Navigation und Fensterschließen geschützt
- historische Logo-Daten werden im Absender-Snapshot statt aus veränderlichen Stammdaten gelesen
- normale Rechnungsänderungen erhalten vorhandene PDF-Verknüpfungen und markieren sie als veraltet

### Fixed
- Unlesbare aktive und inaktive Kundenauswahl im dunklen Farbschema
- Startfehler durch zu spät deklarierte XAML-Ressource
- Doppeltes Öffnen des Hauptfensters beim Programmstart
- Nicht lesbare Tabellenüberschriften im dunklen Farbschema
- Funktionslose Kunden-Schaltflächen
- Unlesbare ComboBox-Auswahl im dunklen Rechnungseditor
- Verlustbehaftete Anzeige präziser Dezimalwerte im Rechnungseditor
- konkurrierende Rechnungsänderungen überschreiben keine neuere PDF-Verknüpfung
- E-Mail-Entwürfe prüfen unmittelbar vor dem Öffnen erneut PDF-Aktualität und Dateipfad
- jede PDF-Neuerzeugung verwendet einen eindeutigen Zielpfad, damit Fehler keine gültige Vorgängerversion löschen
- Windows-PowerShell-inkompatibles Beenden im WPF-Start-Smoke-Test

### Security
- Vertrauliche lokale Eingabeordner und PDF-Dateien werden von Git ignoriert
- PDF-Pfade werden gegen Traversal, absolute Pfade und Reparse-Points abgesichert
- Bekannte transitive Paket-Schwachstellen entfernt