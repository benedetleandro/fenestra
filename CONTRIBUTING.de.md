# Mitwirken

[English](CONTRIBUTING.md) · [Português](CONTRIBUTING.pt-BR.md) · [Español](CONTRIBUTING.es.md) · [Français](CONTRIBUTING.fr.md) · [Deutsch](CONTRIBUTING.de.md) · [Italiano](CONTRIBUTING.it.md) · [Nederlands](CONTRIBUTING.nl.md)

<!-- translation of CONTRIBUTING.md @ 0.7.0 -->


Vielen Dank, dass Sie mitwirken möchten. Dieses Werkzeug ist bewusst klein – halten
Sie es klein.

## Bauen

Voraussetzungen: Windows 10/11 und das mitgelieferte .NET Framework 4.x.
Kein SDK, kein Visual Studio, kein Internet.

```powershell
./build.ps1
```

Die Binärdatei landet in `bin/Fenestra.exe`.

## Tests

```powershell
./tests/run-tests.ps1
```

Das Skript baut nichts; führen Sie zuerst `build.ps1` aus. Es startet den Server im
Headless-Modus (`FENESTRA_NO_OPEN=1`), stellt echte HTTP-Anfragen an ein temporäres
Fixture und meldet Bestanden/Nicht bestanden mit einem Exit-Code ungleich null.

## Codestil

- **Nur C# 5.** Der Compiler ist der aus .NET Framework 4.8, daher keine
  Interpolation, kein `?.`, keine ausdrucksbasierten Mitglieder.
- **Keine Abhängigkeiten.** Nur die BCL. Wenn eine Änderung NuGet erfordert, ist
  zuerst eine Diskussion nötig.
- **Vorerst eine Datei.** `src/Fenestra.cs` ist absichtlich eine einzige Datei; das
  Aufteilen steht auf der Roadmap, ist aber keine Voraussetzung für PRs.
- Kommentare erklären das *Warum*, nicht das *Was*.

## Pull Requests

- Eine Sache pro PR.
- Fügen Sie einen Test hinzu, wenn Sie die HTTP-Verarbeitung, die Pfadauflösung oder
  die Portlogik ändern.
- Aktualisieren Sie `README.md`, wenn sich das Verhalten ändert.
- Halten Sie den Diff lesbar; keine beiläufigen Neuformatierungen.

## Gute erste Issues

- `--root <folder>`, um einen anderen Ordner als den der Datei auszuliefern.
- Unterstützung für Windows ARM64.
- Codesignierung für die Release-Binärdatei (beseitigt die SmartScreen-Warnung).
- Portabler Modus (`fenestra.ini` neben der exe, ohne `%LOCALAPPDATA%`).
- Mehr Tests: HEAD, ETag, Range-Requests, UTF-8-Pfade.
