# GitHub-Repository – E-Rechnung SD

**URL:** `https://github.com/homerwasser/E-Rechnung-SD`
**Typ:** Private Repository

## Repository-Einrichtung

### 1. Grundsetup (manuell auf GitHub)

```bash
# Auf GitHub.com erstellen:
# - Name: E-Rechnung-SD
# - Visibility: Private
# - Initialisierung: Leer (kein README, keine .gitignore)
```

### 2. Lokaler Start

```bash
cd C:\Projekte\E-Rechnung-SD
git init
git remote add origin https://github.com/homerwasser/E-Rechnung-SD.git
```

### 3. Branching-Strategie

```
main          ← Produktionsbereit, immer stabil
release/vX.X.X ← Vorbereiten für Releases
develop       ← Hauptentwicklung
feature/*     ← Neue Funktionen (kurzlebig)
bugfix/*      ← Bugfixes (kurzlebig)
hotfix/*      ← Kritische Fixes (direkt auf main)
```

**Merge Strategy:**
- Feature → develop → release → main
- Pull Requests (PR) für jeden Merge
- Squash & Merge bei Features, rebase für Hotfixes

### 4. Labels (Issues/PRs)

| Label              | Farbe     | Verwendung                           |
|--------------------|-----------|--------------------------------------|
| `enhancement`      | Blue      | Neue Features                        |
| `bug`              | Red       | Fehler                               |
| `documentation`    | Green     | Doky, Readme, Wiki                   |
| `ui/ux`            | Purple    | Interface-Änderungen                 |
| `database`         | Brown     | DB-Changes, Schema                   |
| `testing`          | Yellow    | Tests hinzufügen/verbessern          |
| `xml/ubl`          | Orange    | UBL 2.2 / Factur-X Arbeit            |
| `pdf/export`       | Pink      | PDF-Generierung                     |
| `email`            | SkyBlue   | E-Mail-Funktion                      |
| `backup/restore`   | Gray      | Sicherung/FWiederherstellung         |
| `update`           | Indigo    | Update-Feature                       |
| `good first issue`| LightBlue | Für KI/Einsteiger gut geeignet       |
| `priority/high`    | Red       | Wichtig                              |
| `priority/medium`  | Orange    | Normal                               |
| `priority/low`     | Green     | Weniger wichtig                      |

### 5. Templates

#### Issue Template (`.github/ISSUE_TEMPLATE/feature_request.md`)

```markdown
---
name: Feature Request
about: Neues Feature oder Anforderung
title: ''
labels: 'enhancement'
assignees: ''
---

## Beschreibung
Beschreibe das Feature.

## Akzeptanzkriterien
- [ ] Kriterium 1
- [ ] Kriterium 2

## Abhängigkeiten
Ggf. Blocker oder Voraussetzungen.

## Bemerkungen
Zusätzliche Hinweise.
```

#### Bug Report (`.github/ISSUE_TEMPLATE/bug_report.md`)

```markdown
---
name: Bug Report
about: Melde einen Fehler
title: ''
labels: 'bug'
assignees: ''
---

## Beschreibung
Was ist der Fehler?

## Reproduktionsschritte
1. ...
2. ...
3. ...

## Erwartetes Verhalten
Was sollte stattdessen passieren?

## Umgebung
- OS: Windows 10/11
- Version: v0.1.0

## Logs/Screenshots
Falls vorhanden.
```

#### Pull Request Template (`.github/PULL_REQUEST_TEMPLATE.md`)

```markdown
## Zusammenfassung
Kurze Beschreibung der Änderungen.

## Art der Änderung
- [ ] Bugfix
- [ ] Neues Feature
- [ ] Refactoring
- [ ] Dokumentation
- [ ] Test

## Checkliste
- [ ] Code compiliert ohne Errors
- [ ] Tests geschrieben/passen
- [ ] Änderungen dokumentiert

## Screenshots
Für UI-Änderungen.

## Zusammengehörige Issues
Fixes #XXX
```

## Tags

| Format        | Beispiel      | Bedeutung                       |
|---------------|--------------|----------------------------------|
| `vX.Y.Z`      | `v0.1.0`    | Semantische Versionierung        |
| `dev-build`   | `dev-2024-001` | Entwicklungsbuilds          |

## Releases

| Typ     | Beschreibung                       | Trigger               |
|---------|-------------------------------------|-----------------------|
| Alpha   | Erste funktionierende Version       | Nach Milestone 2      |
| Beta    | Vollständig, noch Feinschliff       | Nach Milestone 4      |
| RC      | Release Candidate, stabil           | Nach Milestone 5      |
| Stable  | Produktionsfertige Version          | Nach Milestone 6      |

## CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/ci.yml

name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
      - run: dotnet build --configuration Release
      - run: dotnet test
      - run: dotnet publish -c Release -r win-x64 --self-contained
      - uses: actions/upload-artifact@v4
        with:
          name: erechnung-app
          path: '**/publish/'

  # Release-Job bei Tag-Push
  release:
    needs: build
    if: startsWith(github.ref, 'refs/tags/v')
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: softprops/action-gh-release@v1
        with:
          files: '**/publish/ERechnung.exe'
          generate_release_notes: true
```

## Changelog-Format

Siehe `CHANGELOG.md`.