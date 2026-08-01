# Use-Cases – E-Rechnung SD

## UC-1: Rechnungserstellung (B2B)

**Akteure:** Buchhalter, Freiberufler  
**Ziel:** Eine gültige E-Rechnung (EN 16931) aus einer Vorlage erstellen.

### Ablauf
1. Benutzer wählt **Vorlage** (fest oder frei definierbar).
2. Benutzer füllt **Pflichtfelder**: Firmenname, Adresse, Logo, IBAN/BIC, USt-ID.
3. Benutzer fügt **Empfängerdaten** ein (Neuanlage oder aus Kundenstamm).
4. Benutzer wählt **Kostenaufstellung**:
   - **Modus A**: Feste Kostenzeile (z.B. „Tagespauschale", „Reisekosten").
   - **Modus B**: Flexible Tabelle – beliebige Anzahl an Zeilen, freier Text, Mengen, Preise.
5. System berechnet **Gesamtbetrag, Steuern (USt.), Zwischensummen**.
6. Vorschau der PDF-Rechnung angezeigt.
7. Benutzer klickt **„Speichern"** → Rechnung wird als PDF + UBL-XML (E-Rechnung) gespeichert.
8. Rechnung erscheint im **Dashboard** als gespeicherte Rechnung.

---

## UC-2: E-Rechnung verschicken per E-Mail

**Akteure:** Buchhalter, Freiberufler  
**Ziel:** Die erstellte E-Rechnung per Outlook versenden.

### Ablauf
1. Benutzer öffnet eine gespeicherte Rechnung im Dashboard.
2. Klick auf **„Per E-Mail senden"**.
3. System startet Outlook mit:
   - Empfängeradresse vorbefüllt (aus Kundendaten).
   - Betreff: `E-Rechnung [Nr.] – [Firmenname]`.
   - Anhang: Die PDF-Rechnung (inkl. eingebettetem UBL-Factur-X).
4. Benutzer prüft, bestätigt, versendet.

---

## UC-3: Rechnung bearbeiten & stornieren

**Akteure:** Buchhalter  
**Ziel:** Eine bereits gespeicherte Rechnung ändern oder als storniert markieren.

### Ablauf
1. Benutzer öffnet eine Rechnung aus dem Dashboard.
2. Änderungen am Inhalt, Betrag, Empfänger.
3. **„Aktualisiertes PDF neu erstellen"**.
4. Alternativ: **„Stornieren"** → Rechnung erhält Status "storniert", neue Gutschrift möglich.

---

## UC-4: Rechnungsverwaltung (Dashboard)

**Akteure:** Buchhalter  
**Ziel:** Übersicht aller Rechnungen mit Filter- und Suchfunktion.

### Features
- Tabelle: Rechnungsnummer, Datum, Empfänger, Betrag, Status.
- Filtern nach Zeitraum, Kunde, Status.
- Sortieren nach Datum, Betrag, Name.
- Export nach CSV/Excel.

---

## UC-5: Kundendaten-Verwaltung

**Akteure:** Buchhalter  
**Ziel:** Stammdaten von Kunden pflegen und wieder verwenden.

### Features
- CRUD (Anlegen, Bearbeiten, Löschen) von Kontakten/Firmen.
- Pflichtfelder: Firma, Kontakt, Email, Adresse, USt-IdNr. (optional).
- Suche im Kundenstamm.
- Einbettung in Rechnungsformular.

---

## UC-6: Vorlagen-Management

**Akteure:** Administrator / Benutzer  
**Ziel:** Eigene PDF-Rechnungsvorlagen erstellen und verwalten.

### Ablauf
1. Importieren von DOCX-Vorlagen (alter Rechnungsvorlagen).
2. System wandelt Vorlage ins interne Layout um.
3. Platzhalter definieren: `{Firma}`, `{Datum}`, `{Rechnungsnummer}`, etc.
4. Vorlage speichern und beim Erstellen auswählen.
5. Mehrere Vorlagen parallel pflegbar (z.B. „Standard", „Projekt", „Tagessatz").

---

## UC-7: Datenbank-Backup & Wiederherstellung

**Akteure:** System (automatisch) / Benutzer (manuell)  
**Ziel:** Sichere Sicherung aller Daten für Neuinstallationen.

### Features
- Automatisch: Tägliches Backup der `erechnung.db` nach `%APPDATA%`.
- Manuelles Backup: Button im Menü.
- Wiederherstellung: Beim Start oder über Menü → „Datenbank wiederherstellen".
- Verschiebbare Datenbank-Datei – komplett getrennt von der `.exe`.

---

## UC-8: Update-Überprüfung & -Installation

**Akteure:** System (automatisch) / Benutzer (manuell)  
**Ziel:** Neue Programmversion erkennen und installieren.

### Ablauf
1. Beim Start: Hintergrundabfrage der GitHub Release-API.
2. Wenn neuer Release → Benachrichtigung.
3. Benutzer bestätigt → Download & Installieren.
4. Programm wird neu gestartet (selbstständig).