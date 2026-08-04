# Datenschutz bei der Entwicklung

Dieses Repository ist öffentlich. Reale Rechnungen, Kundeninformationen und Firmenunterlagen dürfen deshalb niemals committed werden.

## Lokale Ablage für vertrauliche Vorlagen

Vertrauliche DOC-/DOCX-Dateien, Logos, PDFs und Beispieldaten ausschließlich in einem der ignorierten Ordner ablegen:

```text
local-input/
private/
```

Diese Dateien können lokal zur Analyse verwendet werden, dürfen aber nicht in Commits, Issues, Pull Requests, Releases, Screenshots oder Testdaten erscheinen. Zusätzlich ignoriert Git vorsorglich alle `*.pdf`-Dateien.

## Anwendungsdaten

Die Anwendung speichert Rechnungs-PDFs außerhalb des Repositorys unter `%LOCALAPPDATA%\ERechnung-SD\pdf\<Jahr>\`. In SQLite werden nur relative Pfade gespeichert; technische Dateinamen enthalten keine Kundennamen. Beim Löschen einer Rechnung wird die verknüpfte PDF ebenfalls gelöscht. Scheitert die Dateilöschung, bleibt der Datenbankeintrag erhalten und der Benutzer erhält eine Fehlermeldung.

## Testdaten

Automatisierte Tests verwenden ausschließlich erfundene Firmen und Adressen. Namen realer Kunden, Rechnungsnummern, Bankverbindungen und Steuerdaten sind verboten.

## Vor jedem Commit

```powershell
git status --short
git diff --cached
```

Zusätzlich prüfen:

- keine Datenbanken (`*.db`, `*.sqlite`)
- keine Backups (`*.bak`)
- keine PDFs, echten Rechnungen oder Logos
- keine Tokens, Passwörter oder API-Schlüssel

## GitHub-Anmeldung

GitHub-Zugang ausschließlich lokal über `gh auth login` verwalten. Tokens niemals im Chat oder in Terminalbefehlen einfügen.
