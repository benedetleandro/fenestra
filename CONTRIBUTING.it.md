# Contribuire

[English](CONTRIBUTING.md) · [Português](CONTRIBUTING.pt-BR.md) · [Español](CONTRIBUTING.es.md) · [Français](CONTRIBUTING.fr.md) · [Deutsch](CONTRIBUTING.de.md) · [Italiano](CONTRIBUTING.it.md) · [Nederlands](CONTRIBUTING.nl.md)

<!-- translation of CONTRIBUTING.md @ 0.7.0 -->


Grazie per averci pensato. Questo è uno strumento piccolo di proposito — tienilo piccolo.

## Build

Requisiti: Windows 10/11 e il .NET Framework 4.x che Windows include già.
Nessun SDK, niente Visual Studio, niente internet.

```powershell
./build.ps1
```

Il binario finisce in `bin/Fenestra.exe`.

## Test

```powershell
./tests/run-tests.ps1
```

Lo script non compila nulla; esegui prima `build.ps1`. Avvia il server in
modalità headless (`FENESTRA_NO_OPEN=1`), fa vere richieste HTTP contro un
fixture temporaneo e riporta pass/fail con un codice di uscita diverso da zero.

## Stile del codice

- **Solo C# 5.** Il compilatore è quello di .NET Framework 4.8, quindi niente
  interpolazione, niente `?.`, niente membri con corpo a espressione.
- **Zero dipendenze.** Solo la BCL. Se una modifica richiede NuGet, prima serve
  una discussione.
- **Un solo file per ora.** `src/Fenestra.cs` è di proposito un unico file;
  dividerlo è in roadmap ma non è un requisito per le PR.
- I commenti spiegano *perché*, non *cosa*.

## Pull request

- Una sola cosa per PR.
- Includi un test se tocchi la gestione HTTP, la risoluzione dei percorsi o la logica delle porte.
- Aggiorna `README.md` se il comportamento cambia.
- Mantieni il diff leggibile; niente riformattazioni estranee.

## Good first issues

- `--root <folder>` per servire una cartella diversa da quella del file stesso.
- Supporto a Windows ARM64.
- Firma del codice per il binario di release (elimina l'avviso di SmartScreen).
- Modalità portatile (`fenestra.ini` accanto all'exe, senza `%LOCALAPPDATA%`).
- Più test: HEAD, ETag, richieste range, percorsi UTF-8.
