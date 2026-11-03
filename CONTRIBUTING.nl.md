# Bijdragen

[English](CONTRIBUTING.md) · [Português](CONTRIBUTING.pt-BR.md) · [Español](CONTRIBUTING.es.md) · [Français](CONTRIBUTING.fr.md) · [Deutsch](CONTRIBUTING.de.md) · [Italiano](CONTRIBUTING.it.md) · [Nederlands](CONTRIBUTING.nl.md)

<!-- translation of CONTRIBUTING.md @ 0.7.0 -->


Bedankt dat je erover nadenkt. Dit is bewust een klein hulpmiddel — houd het klein.

## Bouwen

Vereisten: Windows 10/11 en het .NET Framework 4.x dat daarbij wordt geleverd.
Geen SDK, geen Visual Studio, geen internet.

```powershell
./build.ps1
```

Het binaire bestand komt in `bin/Fenestra.exe`.

## Testen

```powershell
./tests/run-tests.ps1
```

Het script bouwt niets; voer eerst `build.ps1` uit. Het start de server in
headless modus (`FENESTRA_NO_OPEN=1`), doet echte HTTP-verzoeken tegen een
tijdelijke fixture en meldt pass/fail met een niet-nul exitcode.

## Codestijl

- **Alleen C# 5.** De compiler is die van .NET Framework 4.8, dus geen
  stringinterpolatie, geen `?.`, geen expression-bodied members.
- **Nul afhankelijkheden.** Alleen de BCL. Als een wijziging NuGet nodig heeft,
  is eerst een discussie nodig.
- **Voorlopig één bestand.** `src/Fenestra.cs` is bewust één bestand; het opsplitsen
  staat op de roadmap, maar is geen vereiste voor PR's.
- Commentaar legt *waarom* uit, niet *wat*.

## Pull requests

- Eén ding per PR.
- Voeg een test toe als je HTTP-afhandeling, padresolutie of poortlogica raakt.
- Werk `README.md` bij als het gedrag verandert.
- Houd de diff leesbaar; geen terloopse herformattering.

## Goede eerste issues

- `--root <map>` om een andere map te serveren dan die van het bestand zelf.
- Ondersteuning voor Windows ARM64.
- Codeondertekening voor het release-binaire bestand (verwijdert de
  SmartScreen-waarschuwing).
- Draagbare modus (`fenestra.ini` naast de exe, geen `%LOCALAPPDATA%`).
- Meer tests: HEAD, ETag, range requests, UTF-8-paden.
