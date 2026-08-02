# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Automatisierter WPF-Start-Smoke-Test
- Versioniertes und transaktionales SQLite-Migrationssystem
- Vollständige Kundenverwaltung mit Suche, Anlegen, Bearbeiten und Löschen
- Validierung für Firmenname, E-Mail, Land und Postleitzahl
- Unit- und SQLite-Integrationstests für M2
- Datenschutzregeln für echte Rechnungen und lokale Vorlagendaten

### Changed
- Datenbank nach `%LOCALAPPDATA%\ERechnung-SD\data\` verschoben
- Kunden-Repository liefert erzeugte IDs zurück und prüft Schreiboperationen
- Roadmap und KI-Arbeitsablauf an verbindliche Abnahmekriterien angepasst
- Test- und SQLite-Abhängigkeiten auf sichere Patchstände aktualisiert

### Fixed
- Startfehler durch zu spät deklarierte XAML-Ressource
- Doppeltes Öffnen des Hauptfensters beim Programmstart
- Nicht lesbare Tabellenüberschriften im dunklen Farbschema
- Funktionslose Kunden-Schaltflächen

### Security
- Vertrauliche lokale Eingabeordner werden von Git ignoriert
- Bekannte transitive Paket-Schwachstellen entfernt