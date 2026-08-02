# Verbindlicher KI-Entwicklungsworkflow

Dieses Dokument gilt für jede KI-gestützte Änderung an `E-Rechnung-SD`.
Ein Issue oder Meilenstein darf erst als abgeschlossen gelten, wenn alle nachfolgenden Schritte erfüllt sind.

## 1. Vor Beginn

1. Anforderungen und Akzeptanzkriterien des GitHub-Issues lesen.
2. Abhängigkeiten und betroffene Dateien bestimmen.
3. Einen eigenen Branch verwenden (`feature/*`, `fix/*`, `docs/*`).
4. `git status` prüfen und fremde oder nicht zugehörige Änderungen nicht überschreiben.
5. Niemals Zugangstoken, Passwörter, echte Rechnungen, Kundendaten oder Firmenlogos committen.

## 2. Implementierung

- Änderungen klein, nachvollziehbar und passend zur Architektur halten.
- Ursache statt Symptom beheben.
- Keine funktionslosen Schaltflächen als fertige Funktion ausgeben.
- Keine Platzhalter oder TODOs als abgeschlossene Akzeptanzkriterien werten.
- Datenbankänderungen ausschließlich über nummerierte, transaktionale Migrationen ausführen.
- Personen- und Rechnungsdaten außerhalb des Repositorys speichern.

## 3. Tests pro wichtigem Issue

Für Geschäftslogik, Datenbankzugriff und Fehlerkorrekturen müssen passende Tests ergänzt werden.
Leere Tests und Dummy-Tests sind nicht zulässig.

Mindestens ausführen:

```powershell
dotnet build E-Rechnung-SD.sln --configuration Release
dotnet test E-Rechnung-SD.sln --configuration Release --no-build --verbosity normal
dotnet list E-Rechnung-SD.sln package --vulnerable --include-transitive
```

Je nach Änderung zusätzlich:

- Datenbank: Integrationstest mit temporärer SQLite-Datei
- Migration: Erstlauf und wiederholter Lauf (Idempotenz)
- UI-Start: `powershell -ExecutionPolicy Bypass -File scripts/Test-AppStartup.ps1`
- UI-Bedienung: manueller Smoke-Test der betroffenen Abläufe
- Release: Self-Contained-Publish auf `win-x64`

## 4. Meilenstein-Abnahme

Ein Meilenstein ist nur abgeschlossen, wenn:

- alle zugeordneten Issues ihre Akzeptanzkriterien erfüllen,
- Build und alle automatisierten Tests erfolgreich sind,
- wichtige UI-Abläufe manuell geprüft wurden,
- keine unbeabsichtigten Dateien im Git-Arbeitsbaum liegen,
- die Änderungen committed und gepusht sind,
- die GitHub-CI erfolgreich ist,
- die zugehörigen GitHub-Issues geschlossen sind,
- der Benutzer die Meilenstein-Abnahme bestätigt hat.

## 5. Pflichtbericht an den Benutzer

```markdown
## Meilenstein Mx: Name

Status: Bereit zur Abnahme / Abgeschlossen / Blockiert

### Erledigt
- Issue und Ergebnis

### Automatische Validierung
- Build-Befehl und Ergebnis
- Test-Befehl und Anzahl der Tests
- GitHub-CI-Status

### Manueller Test
1. Programm starten mit: `dotnet run --project src/ERechnung.App`
2. Konkrete Bedienabläufe testen
3. Erwartetes Ergebnis nennen

### Offene Punkte
- verbleibende Arbeiten oder Risiken

### Nächster Schritt
- erst nach Benutzerfreigabe den nächsten Meilenstein starten
```

## 6. GitHub und Zugangsdaten

Für GitHub-Automatisierung ausschließlich die lokale Anmeldung verwenden:

```powershell
gh auth login
gh auth status
```

Zugangstoken dürfen niemals in Chatnachrichten, Befehlen, Quellcode, Dokumentation oder Git-Historie erscheinen.
