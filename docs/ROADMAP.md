# Roadmap – E-Rechnung SD

GitHub ist die maßgebliche Quelle für Issues und Meilensteine:
<https://github.com/homerwasser/E-Rechnung-SD/milestones>

## Statusübersicht

| Meilenstein | Inhalt | Status |
|---|---|---|
| M1 | Projekt-Setup und Grundgerüst | Implementiert; GitHub-Abschluss noch zu bereinigen |
| M2 | Datenbank und Kundenverwaltung | Abgeschlossen und abgenommen am 02.08.2026 |
| M3 | Rechnungserstellung (Kern) | Abgeschlossen und abgenommen am 03.08.2026 |
| M4 | PDF-Generierung und E-Mail | Implementiert; manuelle Abnahme und GitHub-Abschluss ausstehend |
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
- #37 Startfehler durch XAML-Ressource
- #38 Auswahlkontrast im dunklen Farbschema

**Abnahme:** Build, 19 automatisierte Tests, Start-Smoke-Test, Paketprüfung, GitHub-CI und manueller Bedienungstest erfolgreich.

### M2-Abnahmekriterien

- Datenbank liegt unter `%LOCALAPPDATA%\ERechnung-SD\data\erechnung.db`.
- Migrationen sind versioniert, transaktional und wiederholt ausführbar.
- Kunden können angelegt, angezeigt, gesucht, bearbeitet und gelöscht werden.
- Pflichtfelder und E-Mail-Adresse werden validiert.
- Repository und Migrationen sind mit temporären SQLite-Dateien getestet.
- Anwendung öffnet genau ein Hauptfenster.
- Build, Tests, manueller Test und GitHub-CI sind erfolgreich.

## M3 – Rechnungserstellung

Zugeordnete GitHub-Issues:

- #12 Rechnung-Repository (Master/Detail)
- #13 Automatische Rechnungsnummer
- #14 Rechnung-Validierung
- #15 Tests für Rechnungslogik und Persistenz
- #25 Firmenprofile verwalten und auswählen
- #31 Rechnungen nach Status filtern
- #39 Rechnungsübersicht und Rechnungseditor
- #40 Rechnungs-Service für Berechnung und Status

### Implementierter Stand

- Rechnungen und Positionen werden als vollständiges Aggregat transaktional gespeichert.
- Rechnungsnummern werden pro Jahr atomar im Format `JJJJ-NNN` vergeben.
- Empfänger- und Absenderdaten werden beim Speichern als historische Snapshots übernommen.
- Netto, Umsatzsteuer und Brutto werden aus den Positionen mit kaufmännischer Rundung und Steuergruppierung berechnet.
- Firmenprofile können angelegt, bearbeitet, ausgewählt und – sofern nicht verwendet – gelöscht werden.
- Der Rechnungseditor unterstützt flexible Positionen, Live-Summen, Kunden- und Profilauswahl sowie verständliche Validierungsfehler.
- Die Rechnungsübersicht unterstützt Bearbeitung, Löschen, Statusänderung und Filterung.
- Ungespeicherte Entwürfe werden vor Navigation, Abbruch und Fensterschließen geschützt.
- Optimistische Parallelitätsprüfung verhindert das lautlose Überschreiben einer zwischenzeitlich geänderten Rechnung.
- Kanonische Statuswerte verhindern abweichende Schreibweisen zwischen Core, SQLite und WPF.

### Automatische Validierung

- Release-Build: 0 Fehler, 0 Warnungen
- Unit-, SQLite- und ViewModel-Tests: 131/131 erfolgreich
- WPF-Start-Smoke-Test: erfolgreich
- bekannte verwundbare NuGet-Pakete: keine

### Manuelle Abnahme

Der vollständige Bedienungstest wurde am 03.08.2026 ohne festgestellte Fehler abgeschlossen. Umfang und Menge der Testaufgaben wurden vom Benutzer ausdrücklich bestätigt.

### M3-Abschluss

- Feature-PR #41 wurde nach erfolgreicher GitHub-CI per Squash gemergt.
- Alle acht zugeordneten M3-Issues sind geschlossen.
- Build, 131 automatisierte Tests, Start-Smoke-Test und Paketprüfung waren erfolgreich.
- Der vollständige manuelle Bedienungstest wurde ohne festgestellte Fehler abgenommen.

## M4 – PDF-Generierung und E-Mail

Zugeordnete GitHub-Issues:

- #16 QuestPDF-Integration
- #17 professionelles PDF-Layout
- #18 Leistungsdatum in Rechnung und PDF
- #19 PDF lokal speichern, verknüpfen und öffnen
- #20 interaktiver E-Mail-Entwurf mit sicherem Fallback
- #21 PDF-, Ablage- und E-Mail-Tests

### Implementierter Stand

- QuestPDF erzeugt komprimierte PDF/A-3B-Dokumente im deutschen Zahlen- und Datumsformat.
- Absender-, Empfänger- und Logo-Snapshots halten die historische Rechnungsdarstellung stabil.
- Ein- und mehrseitige Positionslisten wiederholen den Tabellenkopf und weisen Steuergruppen getrennt aus.
- PDFs werden mit technischen, eindeutigen Dateinamen unter `%LOCALAPPDATA%\ERechnung-SD\pdf\<Jahr>\` gespeichert; SQLite enthält nur relative Pfade und Zeitstempel.
- Die Übersicht unterscheidet `Kein PDF`, `Aktuell`, `Veraltet` und `Datei fehlt` und kann PDFs öffnen oder im Explorer anzeigen.
- Änderungen an einer Rechnung erhalten die alte PDF-Verknüpfung, markieren sie über den Rechnungszeitstempel aber als veraltet.
- Optimistische Prüfungen auf Rechnungsstand und erwartete Vorgänger-PDF verhindern verlorene Verknüpfungen bei paralleler Erzeugung oder Löschung.
- Klassisches Outlook öffnet einen Entwurf mit PDF-Anhang. Ist es nicht verfügbar, öffnet die Anwendung den Standard-Mail-Client über `mailto:` ohne Anhang, weist auf das manuelle Anhängen hin und zeigt die PDF im Explorer.
- Es wird weder automatisch versendet noch der Rechnungsstatus automatisch geändert.
- Beim Löschen einer Rechnung wird eine verknüpfte PDF mitgelöscht; scheitert die Dateilöschung, bleibt der Datenbankeintrag erhalten.

### Technische Validierung

- Release-Build: 0 Fehler, 0 Warnungen
- Unit-, SQLite- und ViewModel-Tests: 224/224 erfolgreich
- synthetische Einseiten-, Mischsteuer- und Sechsseiten-PDFs erfolgreich erzeugt und gerendert
- selbstenthaltende `win-x64`-PDF-Erzeugung einschließlich nativer QuestPDF-Komponenten erfolgreich
- vollständige WPF-Anwendung als selbstenthaltendes Single-File-Paket veröffentlicht und erfolgreich gestartet
- WPF-Start-Smoke-Test für Release-Build und veröffentlichtes Paket erfolgreich
- keine bekannten verwundbaren direkten oder transitiven NuGet-Pakete
- PDF/A-3B-Kennung in den XMP-Metadaten vorhanden; unabhängige Zertifizierung mit einem externen PDF/A-Validator steht noch aus

### Noch offen

- manueller Bedienungs- und Sichttest
- Pull Request, GitHub-CI, Merge sowie Abschluss der Issues und des Meilensteins

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
