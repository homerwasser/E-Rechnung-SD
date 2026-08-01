# Meilensteine & Issues

## M1 – Projekt-Setup & Grundgerüst

**Ziel:** Repository ist angelegt, Projektstruktur steht, Build läuft, erste Tests.

| Issue-ID | Titel | Beschreibung | Aufwand (h) |
|----------|-------|-------------|-------------|
| ISSUE-1 | Repository auf GitHub erstellen | `E-Rechnung-SD` (private) auf GitHub einrichten. README.md mit Projektbeschreibung. | 0.5 |
| ISSUE-2 | Solution-Grundgerüst erstellen | .NET 8 WPF Solution mit allen Projekten (App, Core, Data, XML, PDF). | 1 |
| ISSUE-3 | CI/CD Pipeline einrichten | GitHub Actions für Build + Test + Publish auf GitHub. | 1.5 |
| ISSUE-4 | Git-Branching-Strategie festlegen | `main` (Release), `develop` (Feature), `feature/*`. | 0.5 |
| ISSUE-5 | Erstes "Hello World" der App | WPF MainWindow öffnet, zeigt "E-Rechnung SD v0.0.0". | 1 |
| ISSUE-6 | Test-Projekt aufsetzen | xUnit + FluentAssertions für Unit-Tests. | 0.5 |

---

## M2 – Datenbank & Kundenverwaltung

**Ziel:** Benutzer kann Kunden anlegen, bearbeiten, löschen. Daten sind persistent.

| Issue-ID | Titel | Beschreibung | Aufwand (h) |
|----------|-------|-------------|-------------|
| ISSUE-7 | SQLite-Integration | Dapper + SQLite als Datenzugriff. Verbindungspooling, Transaktionen. | 2 |
| ISSUE-8 | Migrations-System | Automatisches Schema-Update (Migration 001). | 1.5 |
| ISSUE-9 | Kunden-Repository | CRUD für tbl_Kunde (Insert, Update, Delete, Get). | 2 |
| ISSUE-10 | Kunden-ViewModel | MVVM: Liste, Detail, Validierung, Suche. | 2 |
| ISSUE-11 | Kunden-View (UI) | WPF Grid/Liste, Formular, Suchfeld, Dialoge. | 3 |
| ISSUE-12 | Test: Kunden-Repo | Unit-Tests für Repository (Mock DB oder InMemory). | 1.5 |

---

## M3 – Rechnungserstellung (Kern)

**Ziel:** Benutzer kann eine Rechnung mit variablen Kostenzeilen erstellen.

| Issue-ID | Titel | Beschreibung | Aufwand (h) |
|----------|-------|-------------|-------------|
| ISSUE-13 | Rechnung-Repository | CRUD für tbl_Rechnung + tbl_Rechnungsposition (Master/Detail). | 3 |
| ISSUE-14 | Rechnungsformular (UI) | Eingabefeld: Rechnungsnummer, Datum, Anlass, Empfängerwahl. | 3 |
| ISSUE-15 | Flexible Kostentabelle | Zeilen hinzufügen/löschen, Beschreibung/Menge/Preis/Steuer berechnen. | 4 |
| ISSUE-16 | Automatische Rechnungsnr. | Auto-Inkrement: `2024-001`, `2024-002`… | 1 |
| ISSUE-17 | Rechnung-Validierung | Pflichtfelder: Nr., Datum, mindestens 1 Position. | 1.5 |
| ISSUE-18 | Test: Rechnungsservice | Berechnungstests, Validierungstests. | 2 |

---

## M4 – PDF-Generierung & E-Mail

**Ziel:** Rechnung wird als PDF generiert, per Outlook verschickbar.

| Issue-ID | Titel | Beschreibung | Aufwand (h) |
|----------|-------|-------------|-------------|
| ISSUE-19 | QuestPDF-Integration | Package hinzufügen, Basislayout mit Header/Footer. | 2 |
| ISSUE-20 | PDF-Layout erstellen | Firmenheader, Adressdaten, Positionstabelle, Summenblock. | 4 |
| ISSUE-21 | Rechnungsnr., Ort, Datum-Platzierung | Links im Header: Ereignis-Datum. Rechts: Rechnungs-Datum (über Nr.). | 2 |
| ISSUE-22 | PDF-Speichern | Ausgabe als Datei, in DB verknüpfen, automatisch öffnen optional. | 1.5 |
| ISSUE-23 | Outlook-Integration | `Process.Start("mailto:...")` mit Anhang. | 1.5 |
| ISSUE-24 | Test: PDF-Generierung | Snapshot-Tests, Ausgabe vergleichen. | 1 |

---

## M5 – E-Rechnung: UBL 2.2 & Factur-X

**Ziel:** Rechnungen sind EN 16931-konform (UBL-XML + Factur-X-PDF).

| Issue-ID | Titel | Beschreibung | Aufwand (h) |
|----------|-------|-------------|-------------|
| ISSUE-25 | UBL 2.2 Generator | Erzeuge gültiges Invoice.xml aus Rechnungsdaten. | 5 |
| ISSUE-26 | UBL-Schema-Validierung | XSD-Prüfung gegen EN 16931 BasicWL Profil. | 2 |
| ISSUE-27 | UBL-Parser (Import) | Lese UBL-XML → rekonstruiere Rechnung. | 4 |
| ISSUE-28 | Factur-X Embedding | UBL als PDF/A-3 Annex (Zugferd-Format, analog). | 3 |
| ISSUE-29 | Compliance-Tests | Validate gegen XRechnung/EN-16931 Testfälle. | 2 |

---

## M6 – Vorlagen & Einstellungen

**Ziel:** Benutzer kann eigene Vorlagen verwalten, Firmendaten eingeben.

| Issue-ID | Titel | Beschreibung | Aufwand (h) |
|----------|-------|-------------|-------------|
| ISSUE-30 | Einstellungsdialog | Firmenname, Adresse, IBAN, USt-IdNr. in DB speichern. | 2 |
| ISSUE-31 | Vorlagen-Manager (UI) | Liste, Erstellen, Bearbeiten, Löschen. | 3 |
| ISSUE-32 | DOCX-Import für Vorlagen | Import alter DOC-Rechnung als PDF-Basislayout. | 4 |
| ISSUE-33 | Platzhalter-System | `{Firma}`, `{Datum}`, `{Rechnungsnummer}`, `{Position}`… in Vorlagen. | 3 |
| ISSUE-34 | Test: Vorlagen | Rendering-Tests mit verschiedenen Daten. | 1.5 |

---

## M7 – Backup, Update, Fertigstellung

**Ziel:** Robustes System mit Backup, Update, polierte UX.

| Issue-ID | Titel | Beschreibung | Aufwand (h) |
|----------|-------|-------------|-------------|
| ISSUE-35 | Backup-Service | Automatisches tägliches Backup + manuelles Backup. | 2 |
| ISSUE-36 | Restore-Dialog | Backup auswählen → DB prüfen → ersetzen → Restart. | 2 |
| ISSUE-37 | Update-Service | Prüfe GitHub API auf neueste Version. | 2 |
| ISSUE-38 | Update-Installer | Herunterladen, alte .exe sichern, neue .exe ersetzen, Restart. | 2.5 |
| ISSUE-39 | Self-Contained Publish | SingleFile, win-x64, <100MB. | 1 |
| ISSUE-40 | UX-Polishing | Icons, Splash-Screen, About-Dialog, Spracheinstellung. | 3 |
| ISSUE-41 | README.md finalisieren | Setup, Screenshot, Features, Lizenz. | 1 |
| ISSUE-42 | CHANGELOG.md | Alle Changes dokumentieren. | 0.5 |
| ISSUE-43 | Erstes Release (v0.1.0-alpha) | Tag + GitHub Release mit Download-Link. | 0.5 |

---

## Zeitplanung

| Meilenstein | Issues | Gesamtaufwand | Zieltermin |
|-------------|--------|--------------|------------|
| M1: Setup | 1–6 | ~5h | Woche 1 |
| M2: Kunden | 7–12 | ~12h | Woche 2 |
| M3: Rechnung | 13–18 | ~15h | Woche 3–4 |
| M4: PDF/Mail | 19–24 | ~16h | Woche 5 |
| M5: UBL/Factur-X | 25–29 | ~16h | Woche 6–7 |
| M6: Vorlagen | 30–34 | ~14h | Woche 8 |
| M7: Fertig | 35–43 | ~17h | Woche 9 |

**Gesamtgeschätzter Aufwand: ~95 Stunden**

> *Hinweis: Diese Schätzung enthält nur reine Implementierungszeit. Review, Debugging und Anpassungen kommen hinzu.*