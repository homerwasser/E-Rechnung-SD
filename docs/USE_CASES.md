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
6. Benutzer speichert die Rechnung; sie erscheint in der Rechnungsübersicht.
7. Benutzer erzeugt oder aktualisiert die PDF/A-3B-Rechnung.
8. EN-16931-XML und Factur-X/ZUGFeRD werden in M5 ergänzt.

---

## UC-2: E-Rechnung verschicken per E-Mail

**Akteure:** Buchhalter, Freiberufler  
**Ziel:** Die erstellte E-Rechnung per Outlook versenden.

### Ablauf
1. Benutzer öffnet eine gespeicherte Rechnung im Dashboard.
2. Benutzer wählt eine aktuelle, vorhandene PDF und klickt auf **„E-Mail-Entwurf öffnen"**.
3. Bei klassischem Outlook öffnet das System einen interaktiven Entwurf mit:
   - Empfängeradresse aus dem historischen Rechnungsempfänger-Snapshot,
   - vorbefülltem Betreff und Nachrichtentext,
   - der aktuellen PDF-Rechnung als Anhang.
4. Ist klassisches Outlook nicht verfügbar, öffnet das System den Standard-Mail-Client über `mailto:` mit Empfänger, Betreff und Text. RFC 6068 unterstützt keine Anhänge; deshalb weist die Anwendung auf das manuelle Anhängen hin und zeigt die PDF im Explorer.
5. Der Benutzer prüft und versendet selbst. Die Anwendung ruft niemals automatisch `Send` auf und ändert den Rechnungsstatus nicht automatisch.
6. Das Einbetten von Factur-X/CII wird in M5 ergänzt.

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