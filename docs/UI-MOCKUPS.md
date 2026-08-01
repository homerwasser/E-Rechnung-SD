# UI-Mockups – E-Rechnung SD

## Fensterstruktur

Die App folgt einem klassischen **Single-Window**-Ansatz mit Navigation links.

```
┌──────────────────────────────────────────────────────────────────────┐
│  E-Rechnung SD v0.1.0              [⚙ Einstellungen] [❓ Hilfe] [✕] │
├────────────────┬────────────────────────────────────────────────────┤
│                │                                                    │
│  📊 Dashboard  │  DASHBOARD                                         │
│  ➕ Neue Rechnung │                                              │
│  👥 Kunden     │  Übersicht aller Rechnungen                       │
│  📁 Vorlagen    │                                                      │
│                │  ┌─────────────────────────────────────────────┐   │
│  📊 Berichte   │  │ Suche: [___________________] 🔍 [Filter ▼] │   │
│                │  ├─────────────────────────────────────────────┤   │
│                │  │ # Datum     | Kunde   | Betrag | Status     │   │
│                │  │─────────────┼─────────┼────────┼────────────│   │
│                │  │ 2024-001    │ Müller  │ 1.250€ │ Gesendet   │   │
│                │  │ 2024-002    │ Schmidt │   875€ │ Entwurf    │   │
│                │  │ 2024-003    │ Weber   │   500€ │ Bezahlt    │   │
│                │  │ ...         │ ...     │   ...  │ ...        │   │
│                │  └─────────────────────────────────────────────┘   │
│                │                                                    │
│                │  [🗑️ Selektiert löschen] [📤 Alle exportieren]     │
└────────────────┴────────────────────────────────────────────────────┘
```

---

## Rechnungsformular (Neue / Bearbeiten)

```
┌──────────────────────────────────────────────────────────────────────┐
│  E-Rechnung SD              [💾 Speichern] [👁️ Vorschau] [✉️ Senden]│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─ RECHNUNGSDATEN ───────────────────────────────────────────────┐  │
│  │  Rechnungsnr.:  [2024-004 ▼]    Datum: [▢ ▢ ▢ ▢/▢ ▢ ▢ ▢]    │  │
│  │  Anlass/Ort:    [__________________________]                    │  │
│  │  Veranstaltung: [▢ ▢ ▢ ▢/▢ ▢ ▢ ▢]                              │  │
│  │  Kunde:         [🔍 Kunden auswählen ... ▼]                   │  │
│  │                                                                      │
│  ┌─ KOSTENAUFSTELLUNG ──────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  [+ Vorlage wählen ▼]    [+ Freie Tabelle]                   │  │
│  │                                                                │  │
│  │  ┌─────┬──────────────┬─────┬──────┬──────┬──────┬──────────┐ │  │
│  │  │ #   │ Beschreibung │Menge│Einheit│Preis │USt % │Gesamt   │ │  │
│  │  ├─────┼──────────────┼─────┼──────┼──────┼──────┼──────────┤ │  │
│  │  │ 1   │ [Tagespauschale]│ 2   │ [Tage ▼] │ 125  │ [19 ▼] │ 312,50 │ │  │
│  │  │ 2   │ [Anfahrt       ]│ 1   │ [ST  ▼] │ 75   │ [19 ▼] │ 89,25  │ │  │
│  │  │ 3   │ [Miete         ]│ 2   │ [Tage ▼] │ 150  │ [19 ▼] │ 357,00 │ │  │
│  │  │ [+] │              │     │      │      │      │        │ │  │
│  │  └─────┴──────────────┴─────┴──────┴──────┴──────┴──────────┘ │  │
│  │                                                                │  │
│  │  [+ Zeile hinzufügen]  [- Zeile entfernen]                     │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─ ZUSAMMENFASSUNG ────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  Netto:          750,00 €                                     │  │
│  │  + USt (19%):    142,50 €                                     │  │
│  │  = Brutto:        892,50 €                                     │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Kundenverwaltung

```
┌──────────────────────────────────────────────────────────────────────┐
│  E-Rechnung SD                                       [➕ Neuer Kunde]│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  Suche: [___________________] 🔍                             │    │
│  ├─────────────────────────────────────────────────────────────┤    │
│  │  Firma      │ Email            │ USt-IdNr │ Ort            │    │
│  │─────────────┼──────────────────┼──────────┼────────────────│    │
│  │ Müller GmbH │ mail@mueller.de  │ DE123... │ Berlin         │    │
│  │ Schmidt KG  │ info@schmidt.de  │ DE456... │ München        │    │
│  │ Dr. Weber   │ weber@dr.de      │ -        │ Hamburg        │    │
│  │ ...         │ ...              │ ...      │ ...            │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                      │
│  ┌─ Detailansicht (wenn ausgewählt) ─────────────────────────────┐  │
│  │  Firma:          [Müller GmbH                                 ]  │  │
│  │  Ansprechpartner: [Max Müller                                ]  │  │
│  │  Straße:         [Musterstraße 1                             ]  │  │
│  │  PLZ/Ort:        [12345 Berlin                               ]  │  │
│  │  E-Mail:         [mail@mueller.de                             ]  │  │
│  │  Telefon:        [030-123456                                  ]  │  │
│  │  USt-IdNr:       [DE123456789                                 ]  │  │
│  │  Bemerkungen:    [                                            ]  │  │
│  │                   [___________________________________________]  │  │
│  │                                                                  │  │
│  │  [💾 Speichern]  [🗑️ Löschen]  [✉️ E-Mail senden]               │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Vorlagen-Manager

```
┌──────────────────────────────────────────────────────────────────────┐
│  E-Rechnung SD              [➕ Neue Vorlage] [📥 DOCX importieren]  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─ Vorlagen ─────────────────────────────────────────────────────┐  │
│  │                                                                  │  │
│  │  📄 Standard-Tagespauschale       [✏️] [🗑️] [📋 Als Standard] │  │
│  │  ──────────────────────────────────────────────────────────────────│  │
│  │  ├── Tagespauschale (2 x Tage x 125€)                            │  │
│  │  ├── Anfahrt (1 x ST x 75€)                                     │  │
│  │  └── Miete (falls angegeben)                                     │  │
│  │                                                                  │  │
│  │  📄 Projektarbeit                         [✏️] [🗑️] [📋 Als Std]│  │
│  │  ──────────────────────────────────────────────────────────────────│  │
│  │  ├── Reisekosten (Pauschal)                                     │  │
│  │  ├── Stundenlohn (variiert)                                     │  │
│  │  └── Materialkosten                                             │  │
│  │                                                                  │  │
│  │  📄 Freie Vorlage                        [✏️] [🗑️] [📋 Als Std]│  │
│  │  ──────────────────────────────────────────────────────────────────│  │
│  │  (Benutzer fügt beliebige Zeilen hinzu)                          │  │
│  │                                                                  │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─ Platzhalter (für DOCX-Import) ────────────────────────────────┐  │
│  │                                                                  │  │
│  │  {{Firma.Name}}, {{Firma.Strasse}}, {{Firma.PLZ}}, {{Firma.Ort}}│  │
│  │  {{Firma.Email}}, {{Firma.UStIdNr}}                             │  │
│  │  {{Rechnung.Nummer}}, {{Rechnung.Datum}}, {{Rechnung.Anlass}}   │  │
│  │  {{Position.Beschreibung}}, {{Position.Menge}}                  │  │
│  │  {{Gesamt.Netto}}, {{Gesamt.Brutto}}                            │  │
│  │                                                                  │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Einstellungen & Backup

```
┌──────────────────────────────────────────────────────────────────────┐
│  E-Rechnung SD – Einstellungen                                       │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─ Mein Unternehmen ─────────────────────────────────────────────┐  │
│  │  Firmenname:   [Meine Firma GmbH                               ]  │  │
│  │  Ansprechender:[Max Mustermann                                  ]  │  │
│  │  Straße:       [Musterstraße 42                                ]  │  │
│  │  PLZ/Ort:      [12345 Musterstadt                              ]  │  │
│  │  Email:        [kontakt@meine-firma.de                         ]  │  │
│  │  Telefon:      [0123-456789                                   ]  │  │
│  │  IBAN:         [DE12 3456 7890 1234 5678 90                    ]  │  │
│  │  BIC:          [DEUTDEFFXXX                                   ]  │  │
│  │  USt-IdNr:     [DE123456789                                   ]  │  │
│  │  Logo:         [📎 Logo auswählen]                              │  │
│  │                                                                  │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─ Datenbank & Backup ──────────────────────────────────────────┐  │
│  │                                                                  │  │
│  │  Datenbankspeicherort: C:\Users\xxx\AppData\Local\ERechnung\   │  │
│  │                                                                  │  │
│  │  [💾 Jetzt Backup erstellen]      Letzte Sicherung: vor 2 Std. │  │
│  │  [📂 Backups durchsuchen]        12 Backups vorhanden          │  │
│  │  [🔄 Aus Backup wiederherstellen]                                │  │
│  │                                                                  │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─ Update ───────────────────────────────────────────────────────┐  │
│  │                                                                  │  │
│  │  Akt. Version: v0.1.0-beta                                       │  │
│  │  Letzte Prüfung: vor 1 Std.                                     │  │
│  │  [🔄 Jetzt auf Update prüfen]                                   │  │
│  │  ☑ Auto-Update (beim Start prüfen)                              │  │
│  │                                                                  │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  [💾 Einstellungen speichern] [↩ Zurück]                           │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```