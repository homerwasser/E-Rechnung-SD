# KI-Agent Workflows

Dieses Dokument definiert den verbindlichen Prozess für die KI-gestützte Entwicklung im Projekt `E-Rechnung-SD`.

## Regelwerk für KI-Agenten

Bei jeder Arbeitseinheit (Issue oder Milestone) **MUSS** folgender Ablauf eingehalten werden:

### 1. Implementierung
- Code schreiben, Dateien erstellen/bearbeiten.
- Architektur-Richtlinien (`docs/ARCHITECTURE.md`) beachten.

### 2. Test-Phase (PFLICHT)
Bevor ein Meilenstein als "Done" gilt:
1.  **Build:** `dotnet build E-Rechnung-SD.sln --configuration Release` MUSS fehlerfrei durchlaufen (Exit Code 0).
2.  **Test:** `dotnet test --configuration Release` MUSS alle Tests bestehen.
    -   Sind noch keine Tests vorhanden, MUSS der KI-Agent mindestens einen Dummy-Test oder eine grundlegende Unit-Test-Klasse schreiben, um die Pipeline zu validieren.

### 3. Status-Report (Output)
Nach erfolgreicher Testphase MUSS dem Benutzer folgende Information gegeben werden:
1.  **Milestone-Übersicht:** Kurze Zusammenfassung des Erledigten.
2.  **Abfrage:** "Willst du zum nächsten Meilenstein wechseln?"

## Status-Report Format

```markdown
✅ MILESTONE [X]: [NAME] – ABGESCHLOSSEN!

| Issue | Titel | Status |
|-------|-------|--------|
| #XY   | Name  | ✅ Done |

🧪 TEST-Ergebnis:
- Build: 0 Fehler, 0 Warnungen.
- Tests: Alle Bestanden.

🚀 NÄCHSTE SCHRITTE:
Soll ich mit [MILESTONE X+1] fortfahren? (Befehl: LOS)
```