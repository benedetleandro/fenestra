# Fenestra op macOS

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of macos/README.md @ 0.7.0 -->


Deze map is de bouwkit. Je compileert op je eigen machine — er is geen vooraf
gebouwd binair bestand om te downloaden, en dat is bewust: een lokaal gebouwde app
komt nooit langs Gatekeeper.

## Vereiste: Go 1.21+

De officiële installer op <https://go.dev/dl/> is ondertekend en genotariseerd
door Google, dus **er verschijnt geen waarschuwing**. Het is het enige dat je
installeert.

## Pad A — geen git (nieuwe Mac, geen Command Line Tools)

Een vers gewiste Mac heeft geen `git`: de opdracht start de installatie van de
Command Line Tools (~1,5 GB). Dat kun je overslaan:

1. Download de ZIP: <https://github.com/benedetleandro/fenestra/archive/refs/heads/main.zip>
2. Pak hem uit
3. In Terminal:

```bash
cd ~/Downloads/fenestra-main/macos
bash build.command
```

`bash` wordt met het systeem meegeleverd, en het script wordt door de Finder
**gelezen**, niet uitgevoerd — dus Gatekeeper is er niet bij betrokken. `fenestra`
en `Fenestra.app` ontstaan lokaal, zonder quarantaine.

## Pad B — git clone (als je git al hebt)

```bash
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra/macos
./build.command
```

## Pad C — dubbelklik

Alleen als je Go al hebt en de Terminal wilt vermijden. macOS kan eenmalig om
toestemming vragen: **Systeeminstellingen → Privacy en beveiliging → Toch
openen**.

## Gebruik

```bash
./fenestra ~/projects/app/dist/index.html   # opens in the default browser
./fenestra --browsers                       # what is installed
./fenestra --serve ~/index.html             # force the server
./fenestra --port 8080 ~/index.html         # pick the port
./fenestra --browser firefox                # switch browsers
./fenestra --browser safari                 # Safari is in the list too
./fenestra --install                        # Finder instructions
```

Voor de Finder:

1. Sleep `Fenestra.app` naar `/Applications`.
2. Klik met de rechtermuisknop op een `index.html` → **Openen met** →
   **Fenestra**.
3. Vink **Altijd deze app gebruiken** aan.

Als de HTML naar `/src/` verwijst (een dev-ingang), gebruikt Fenestra in plaats
daarvan de build in de naburige `dist/`, `build/` of `out/`, en legt uit wat je
moet doen als die er niet is.

## Homebrew (alleen CLI)

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` is een eenmalige vereiste voor taps van derden. Dit installeert
alleen de commandoregelversie; `Fenestra.app` komt nog steeds uit
`./build.command`.

## Hoe de `.app` werkt

`Fenestra.app` is een AppleScript-*droplet* (gebouwd met `osacompile`, dat met
macOS wordt meegeleverd). Hij ontvangt de bestanden die de Finder hem geeft en
roept het Go-binair bestand in de bundel aan. Dat maakt "Openen met" mogelijk
zonder een Cocoa-app.

## Als er iets misgaat

Het script toont `FAILED at line N`. Kopieer die melding.

Het handmatige pad laat ondertussen zien waar het probleem zit:

```bash
cd fenestra/linux
go version
GOOS=darwin CGO_ENABLED=0 go build -o ../macos/fenestra .
../macos/fenestra ~/path/dist/index.html
```

Als `go build` werkt en `./fenestra` de pagina opent, zit het probleem alleen in
de verpakking van de `.app` — en is het programma al bruikbaar.

Als `osacompile` faalt (het kan om de Command Line Tools vragen), waarschuwt het
script en behoudt het binaire bestand. In dat geval heeft de Finder geen
koppeling, maar werkt de Terminal wel.

## Eerlijke beperkingen

- **Getest in CI, op een echte `macos-latest`**: bouwen, `codesign --verify`, de
  droplet die een bestand ontvangt via de Finder (`open -a`) en de server die
  HTTP beantwoordt.
- **Niet getest in een grafische sessie**: de klik in de Finder, het menu "Openen
  met" en de Gatekeeper-dialoog bestaan alleen op een scherm — de runner is
  headless.
- De `.app` wordt gegenereerd door `osacompile`; als jouw macOS dat hulpmiddel
  mist, waarschuwt het script en bouwt alleen het binaire bestand.
- Iedereen compileert zijn eigen versie. De `.app` via AirDrop naar een andere
  machine sturen voegt quarantaine toe en dan blokkeert Gatekeeper hem opnieuw.
- Geen ontwikkelaarssignatuur, geen notarisatie, geen US$ 99 per jaar.

## Ongedaan maken

Verwijder `Fenestra.app` en `fenestra` uit deze map. Kies voor de Finder een
andere app in **Openen met → Altijd**.
