# Fenestra op Linux

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of linux/README.md @ 0.7.0 -->


Fenestra voor Linux is één statisch Go-binair bestand. Geen runtime, geen GUI,
geen root.

## Installeren

Download het binaire bestand voor je architectuur uit de
[nieuwste release](https://github.com/benedetleandro/fenestra/releases/latest):

- `fenestra-linux-amd64`
- `fenestra-linux-arm64`

```bash
chmod +x fenestra-linux-amd64
mkdir -p ~/.local/bin
mv fenestra-linux-amd64 ~/.local/bin/fenestra
```

Zorg dat `~/.local/bin` in je `PATH` staat.

## Gebruik

```bash
fenestra ~/projects/app/dist/index.html   # open a build
fenestra ~/projects/app/dist              # open a folder
fenestra --serve ~/index.html             # force the server
fenestra --port 8080 ~/index.html         # pick the port
fenestra --browser firefox ~/app/index.html
fenestra --browsers                       # what is installed
```

## Hardening

- Alleen loopback-`Host`-headers worden geaccepteerd (`127.0.0.1`, `localhost`),
  wat DNS rebinding blokkeert.
- Symlinks die buiten de geserveerde root uitkomen, worden geweigerd.
- Antwoorden bevatten `ETag` en `Last-Modified`, worden opnieuw gevalideerd met
  `304` en ondersteunen `Range` voor grote mediabestanden.

## Ingangen in de broncode

Als de HTML naar `/src/` verwijst (een dev-ingang van Vite of iets dergelijks),
serveert Fenestra die niet: hij zoekt ernaast naar `dist/`, `build/` of `out/` en
serveert die build in plaats daarvan. Is er geen build, dan stopt hij met een
melding dat je eerst de build moet uitvoeren.

## De `.html`-handler registreren

```bash
fenestra --install
```

Dit schrijft `~/.local/share/applications/fenestra.desktop`, ververst de
desktopdatabase en roept aan:

```bash
xdg-mime default fenestra.desktop text/html
```

Ongedaan maken:

```bash
fenestra --uninstall
```

Als `xdg-mime` ontbreekt, installeer dan `xdg-utils` of voer de opdracht hierboven
handmatig uit.

## Browsers

De detectie doorloopt de `PATH`, in deze volgorde: `microsoft-edge`,
`google-chrome`, `chromium`, `firefox`, `brave-browser`, `vivaldi`, `opera`. De
voorkeur wordt opgeslagen in `~/.config/fenestra/browser`; `--browser default`
draagt het over aan `xdg-open`.

## Waar hij zijn status bewaart

| Pad | Wat |
|---|---|
| `~/.config/fenestra/browser` | voorkeursbrowser |
| `~/.local/share/fenestra/fenestra.log` | serverlog |
| `~/.local/share/fenestra/` | één lockbestand per geserveerde map (vaste poort) |

`XDG_CONFIG_HOME` en `XDG_DATA_HOME` worden gerespecteerd.

## Bouwen vanuit de broncode

Go 1.21+, geen cgo:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
go test ./...
```

Cross-compileren voor ARM:

```bash
GOOS=linux GOARCH=arm64 CGO_ENABLED=0 go build -o fenestra-arm64 .
```

## Verschillen met de andere builds

- Geen register en geen Finder: het `.desktop`-bestand plus `xdg-mime` is de route
  voor de koppeling.
- Status staat onder XDG-paden, niet onder `%LOCALAPPDATA%` of
  `~/Library/Application Support`.
- Dezelfde server, dezelfde flags, dezelfde SPA-fallback als Windows en macOS.

## Problemen oplossen

- **`could not find <browser> installed on this machine`** — voer
  `fenestra --browsers` uit om te zien wat er is gedetecteerd, of gebruik
  `--browser default`.
- **`xdg-mime failed`** — installeer `xdg-utils`, of stel de handler handmatig in.
- **Er opent niets** — `--browser default` gebruikt `xdg-open`; controleer of dat
  op zichzelf werkt.
- **Geen koppeling na `--install`** — sommige desktops cachen MIME-handlers; log
  uit, of voer `update-desktop-database ~/.local/share/applications` uit.

## Getest in CI

`ubuntu-latest`: `go test`, bouwen en een echte HTTP-smoketest. ARM64 wordt in
dezelfde pipeline ge-crosscompileerd; het wordt niet uitgevoerd.
