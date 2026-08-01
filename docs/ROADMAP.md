# ROADMAP – E-Rechnung SD

## Projekt-Übersicht

| Meilenstein | Issues | Status | Zieltermin |
|-------------|--------|--------|------------|
| M1: Projekt-Setup | 5 Issues | 🟢 Done | KW 31 |
| M2: Datenbank & Kunden | 7 Issues | 🔵 In Progress | KW 32 |
| M3: Rechnungserstellung | 7 Issues | ⏳ Waiting | KW 33-34 |
| M4: PDF & E-Mail | 6 Issues | ⏳ Waiting | KW 35 |
| M5: UBL/Factur-X | 5 Issues | ⏳ Waiting | KW 36-37 |
| M6: Vorlagen & Settings | 7 Issues | ⏳ Waiting | KW 38 |
| M7: Backup/Update | 7 Issues | ⏳ Waiting | KW 39 |

**Gesamtaufwand:** ~100h

---

## M1: Projekt-Setup (KW 31) – ✅ DONE

- [#5] SQLite-Integration (Dapper)
- [#6] Migration-System
- [#7] Kunden-Repository (CRUD)
- [#8] Kunden-ViewModel (MVVM)
- [#9] Kundenverwaltung UI
- [#10] Test: Kunden-Repository
- [#11] Firmenprofil: Mehrere Marken (Tanzschule SDA / Entertainer & Choreograph)

---

## M2: Datenbank & Kunden (KW 32) – 🔄 IN PROGRESS

- [#12] Rechnung-Repository (Master/Detail)
- [#13] Rechnungsformular UI
- [#14] Flexible Kostentabelle (Zeilen +/−)
- [#15] Automatische Rechnungsnr.
- [#16] Rechnungsstatus-System
- [#17] Rechnung-Validierung
- [#18] Test: Rechnung-Service
- [#35] Suchfilter nach Status

---

## M3: Rechnungserstellung (KW 33-34) – ⏳ WAITING

- [#19] QuestPDF-Integration
- [#20] PDF-Layout: Kopfdaten, Positionstabelle, Summen
- [#21] Rechnungsdaten-Platzierung (links Datum, rechts Datum)
- [#22] PDF-Speichern & Verknüpfung
- [#23] Outlook-Integration (mailto + Anhang)
- [#24] Test: PDF-Generierung

---

## M4: PDF & E-Mail (KW 35) – ⏳ WAITING

*(Siehe M3)*

---

## M5: UBL/Factur-X (KW 36-37) – ⏳ WAITING

- [#25] UBL 2.2 Generator (EN 16931 konform)
- [#26] UBL-Schema-Validierung (XSD)
- [#27] UBL-Parser (Import aus XML)
- [#28] Factur-X Embedding (PDF/A mit UBL)
- [#29] Compliance-Tests (EN 16931)

---

## M6: Vorlagen & Settings (KW 38) – ⏳ WAITING

- [#30] Firmeneinstellungen (IBAN, USt-IdNr, Logo)
- [#31] Vorlagen-Manager (UI)
- [#32] DOCX-Import: Alte Rechnungen als Vorlage
- [#33] Platzhalter-System
- [#34] Dashboard-Analyse: Forderungen über Zeit
- [#42] README.md finalisieren
- [#43] Erstes Release (v0.1.0-alpha)

---

## M7: Backup/Update (KW 39) – ⏳ WAITING

- [#35] Backup-Service (auto + manuell)
- [#36] Backup wiederherstellen
- [#37] Update-Service (GitHub API)
- [#38] Self-Contained Publish (SingleFile)
- [#41] UX-Polishing: Icons, Splashscreen, About-Dialog
- [#40] Dashboard: Forderungsanalyse (Diagramm)

---

## Labels

| Kategorie | Color |
|-----------|-------|
| enhancement | 🔵 Blue |
| bug | 🔴 Red |
| testing | 🟡 Yellow |
| pdf/export | 🩷 Pink |
| email | 💙 Light Blue |
| xml/ubl | 🧡 Orange |
| database | 🤎 Brown |
| backup/restore | ⚫ Gray |
| ui/ux | 💜 Purple |
| documentation | 🟦 Dark Blue |
| priority/high | 🔴 Red |
| priority/medium | 🟠 Orange |
| priority/low | 🟢 Green |

---

## Branching-Strategie

```
main          ← Produktionsbereit, immer stabil
release/vX.X.X ← Vorbereiten für Releases
develop       ← Hauptentwicklung
feature/*     ← Neue Funktionen (kurzlebig)
bugfix/*      ← Bugfixes (kurzlebig)
hotfix/*      ← Kritische Fixes (direkt auf main)
```

## Status-Semantik

- `erstellt` – Rechnung angelegt, noch nicht versendet
- `versendet` – Per E-Mail abgeschickt
- `offen` – Versendet, Zahlung noch offen
- `bezahlt` – Zahlung erhalten
- `inklarung` – In Diskussion / Klärungsbedarf
- `storniert` – Storniert, Gutschrift möglich

## Datenbank

Siehe `DATABASE_SCHEMA.md`

## UI-Mockups

Siehe `UI-MOCKUPS.md`