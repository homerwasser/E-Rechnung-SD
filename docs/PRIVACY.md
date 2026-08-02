# Datenschutz bei der Entwicklung

Dieses Repository ist öffentlich. Reale Rechnungen, Kundeninformationen und Firmenunterlagen dürfen deshalb niemals committed werden.

## Lokale Ablage für vertrauliche Vorlagen

Vertrauliche DOC-/DOCX-Dateien, Logos und Beispieldaten ausschließlich in einem der ignorierten Ordner ablegen:

```text
local-input/
private/
```

Diese Dateien können lokal zur Analyse verwendet werden, dürfen aber nicht in Commits, Issues, Pull Requests, Releases, Screenshots oder Testdaten erscheinen.

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
- keine echten Rechnungen oder Logos
- keine Tokens, Passwörter oder API-Schlüssel

## GitHub-Anmeldung

GitHub-Zugang ausschließlich lokal über `gh auth login` verwalten. Tokens niemals im Chat oder in Terminalbefehlen einfügen.
