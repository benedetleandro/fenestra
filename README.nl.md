# Fenestra

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of README.md @ 0.7.0 -->

**Dubbelklik op een lokale `dist/index.html`. Hij opent in je browser als een website — omdat Fenestra hem heeft geserveerd.**

Geen terminal. Geen `npx serve`. Geen extensie om in te schakelen. Geen eigen browservenster.

![build](https://github.com/benedetleandro/fenestra/actions/workflows/build.yml/badge.svg)
![license](https://img.shields.io/badge/license-MIT-blue.svg)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)

[**Website →**](https://benedetleandro.github.io/fenestra/) · [Privacy](https://benedetleandro.github.io/fenestra/privacy.html)

> **Status: alfa.** De HTTP-server en de browserintegratie werken. De
> bestandskoppeling vereist een eenmalige bevestiging van de gebruiker — Windows
> staat niet toe dat een app zelf de standaardhandler wordt.

## Het probleem

Een moderne build (`Vite`, `React`, `Svelte`, alles met ES-modules) draait niet
vanaf `file://`. De browser weigert modulescripts vanwege CORS, `/assets/...`
wijst naar de hoofdmap van de schijf en service workers zijn niet beschikbaar. Je
hebt een HTTP-origin nodig.

Meestal betekent dat een terminal openen en een statische server starten.
Fenestra schrapt die stap: hij *is* de statische server, gekoppeld aan de
dubbelklik.

## Wat het doet

Wanneer je een `.html`-bestand opent:

1. Fenestra leest het bestand en bepaalt of het een HTTP-origin nodig heeft
   (ES-modules, root-absolute paden, `fetch`, service worker, webmanifest).
2. **Als dat zo is**, serveert hij de map waarin het bestand staat op
   `127.0.0.1`, op een vaste poort, en opent hij een gewoon tabblad in je browser
   op die URL.
3. **Als dat niet zo is**, opent hij het bestand rechtstreeks via `file://`. Er
   wordt geen server gestart.

De server houdt dezelfde poort per map, zodat `localStorage`, cookies en de
service worker bewaard blijven tussen sessies.

## Installeren

Vereisten: Windows 10/11. Het .NET Framework 4.x en Edge zijn al aanwezig — en
elke op Chromium gebaseerde browser of Firefox werkt ook. Op Linux is er een
aparte native build, zie [Linux](#linux).

```powershell
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Het binaire bestand is `bin\Fenestra.exe`. Geen SDK, geen NuGet, geen internet
nodig om te bouwen.

## Gebruik

```powershell
Fenestra.exe "C:\projects\my-app\dist\index.html"
Fenestra.exe "C:\projects\my-app\dist"
Fenestra.exe --serve "C:\path\index.html"   # force the server, skip detection
Fenestra.exe --browser chrome "C:\path\index.html"
Fenestra.exe --browser firefox              # set the preferred browser
Fenestra.exe --browsers                     # list detected browsers
Fenestra.exe --install                      # register in the "Open with" list
Fenestra.exe --uninstall                    # remove it
```

### Maak hem de standaard voor `.html` (eenmalig)

1. Voer `Fenestra.exe --install` uit.
2. Klik met de rechtermuisknop op een willekeurig `index.html` → **Openen met** →
   **Een andere app kiezen**.
3. Selecteer **Fenestra** en vink **Altijd deze app gebruiken** aan.

Windows staat precies één handler per extensie toe, dus deze bevestiging kan niet
geautomatiseerd worden. De handler is onzichtbaar; het venster dat opent is je
browser.

### Browsers

**De systeembrowser is de standaard.** Chrome, Firefox, Brave, Vivaldi, Opera en
Chromium worden automatisch gedetecteerd, en `--browser edge` legt Edge vast. Op
macOS staat **Safari** ook in de lijst.

```powershell
Fenestra.exe --browsers              # what is installed
Fenestra.exe --browser chrome        # use Chrome from now on
Fenestra.exe --browser default       # let Windows decide
```

De keuze wordt opgeslagen in `%LOCALAPPDATA%\Fenestra\browser.txt` en geldt voor
elke dubbelklik, dus je hoeft niet opnieuw een flag mee te geven.

### Headless (tests / CI)

Stel `FENESTRA_NO_OPEN=1` in en de server start zonder een browser te openen. De
poort wordt weggeschreven naar `%LOCALAPPDATA%\Fenestra\sessions\<hash>.txt`.

## Linux

Een aparte, native build staat in [`linux/`](linux): één statisch binair bestand,
geen runtime, geen Wine. Geschreven in Go en gecompileerd voor `linux/amd64` en
`linux/arm64`. Volledige handleiding: [linux/README.nl.md](linux/README.nl.md).

```bash
chmod +x fenestra-linux-amd64
./fenestra-linux-amd64 ~/projects/app/dist/index.html
```

Hetzelfde gedrag als de Windows-build: vaste poort per map, SPA-fallback,
padinperking en browserkeuze.

```bash
./fenestra-linux-amd64 --browsers
./fenestra-linux-amd64 --browser firefox
./fenestra-linux-amd64 --install      # .desktop + xdg-mime
```

`--install` schrijft `~/.local/share/applications/fenestra.desktop` en roept
`xdg-mime default` aan. Het vereist `xdg-utils`, dat de meeste desktops al hebben.

Bouwen vanuit de broncode:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
```

## macOS

De **CLI** is beschikbaar als vooraf gebouwd binair bestand via Homebrew:

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` is een eenmalige bevestiging die Homebrew vereist voor taps van
derden.

De **Finder-app** (`Fenestra.app`) heeft bewust geen vooraf gebouwd binair
bestand: een niet-ondertekende bundle wordt door Gatekeeper geblokkeerd, en
notarisatie kost US$ 99 per jaar. [`macos/`](macos) is de bouwkit: installeer Go
(officiële ondertekende installer, geen waarschuwing), doe `git clone` en voer
één opdracht uit. Klonen met git voegt het quarantaineattribuut niet toe, dus
macOS vraagt nooit om toestemming. Zonder git download je de ZIP en voer je
`bash build.command` uit. Zie [macos/README.nl.md](macos/README.nl.md).

De kit wordt gebouwd en getest op een echte `macos-latest`-runner in CI: bouwen,
`codesign --verify`, de droplet die een bestand ontvangt via `open -a`, en de
server die HTTP beantwoordt. Alleen de Finder-interface zelf is ongetest (runners
zijn headless).

## Wanneer het werkt

| Geval | Werkt? | Waarom |
|---|---|---|
| Statische build (`dist`, `build`, `out`) | **Ja** | gewone HTML/CSS/JS |
| SPA met clientside-routes | **Ja** | de server valt terug op `index.html` voor paden zonder extensie |
| Eenvoudige HTML in één bestand | **Ja** | geopend via `file://`, geen server |
| Bronproject (`/src/main.tsx`) | **Nee** | heeft de devserver van het project nodig om te compileren |
| App met een backend/API | **Nee** | Fenestra serveert bestanden; hij proxyt niet en draait geen services |
| Entry in een submap die naar `../` verwijst | **Nee** | de root is de map van het bestand; `..` is ingeperkt voor de veiligheid |

Als de detectie het mis heeft, forceert `--serve` de HTTP-origin.

## Hoe het werkt

Eén C#-bestand, geen afhankelijkheden, gebouwd met de `csc.exe` die bij Windows
wordt geleverd.

- **HTTP/1.1-server** op een `TcpListener`, uitsluitend gebonden aan `127.0.0.1`.
- **Padresolutie**: normaliseren, `..`, `:` en `::$DATA` weigeren, inperken tot de
  geserveerde root.
- **Poorttoewijzing**: FNV-1a-hash van de map → poort in `51000-63000`, opgeslagen
  in een lockbestand per map en hergebruikt bij de volgende start.
- **SPA-fallback**: 404's zonder extensie geven `index.html` terug.
- **Eén instantie**: een tweede dubbelklik pingt de draaiende server en opent
  alleen een extra browsertabblad.
- **Levensduur**: de server stopt wanneer het browserproces dat hij startte
  stopt, of na 4 uur inactiviteit als de browser al draaide.

## Beveiliging

Fenestra is een lokaal hulpmiddel: alleen loopback, geen TLS, geen
authenticatie, geen telemetrie. Hij serveert elk bestand onder de geopende map —
wijs hem niet naar een map met geheimen. Zie [SECURITY.nl.md](SECURITY.nl.md)
voor het volledige dreigingsmodel en de
[privacypagina](https://benedetleandro.github.io/fenestra/privacy.html) voor wat
deze site en de app opslaan.

## Roadmap

- [ ] `--root <map>` voor builds waarvan de entry in een submap staat
- [x] Linux: native Go-build in [`linux/`](linux), amd64 en arm64
- [x] macOS: Homebrew-CLI en de Finder-bouwkit in [`macos/`](macos)
- [ ] Codeondertekening voor releases, om de SmartScreen-waarschuwing te verwijderen
- [ ] Draagbare modus (geen `%LOCALAPPDATA%`)
- [ ] Meer tests: HEAD, ETag, range requests, UTF-8-paden
- [ ] Een demoopname van 15 seconden

## Privacy

De website en de app gebruiken geen cookies, analytics, advertenties of
telemetrie. De enige lokale opslag is de taal die je kiest en de privacyverklaring
die je wegklikt. Details:
[privacypagina](https://benedetleandro.github.io/fenestra/privacy.html).

## Bijdragen

Klein hulpmiddel, klein oppervlak. Lees [CONTRIBUTING.nl.md](CONTRIBUTING.nl.md);
de lijst met `good first issues` staat daar.

## Licentie

[MIT](LICENSE) © Fenestra contributors
