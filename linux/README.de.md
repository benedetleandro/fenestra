# Fenestra unter Linux

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of linux/README.md @ 0.7.0 -->


Fenestra für Linux ist eine einzelne statische Go-Binärdatei. Keine
Laufzeitumgebung, keine GUI, kein Root.

## Installation

Laden Sie die Binärdatei für Ihre Architektur aus dem
[neuesten Release](https://github.com/benedetleandro/fenestra/releases/latest)
herunter:

- `fenestra-linux-amd64`
- `fenestra-linux-arm64`

```bash
chmod +x fenestra-linux-amd64
mkdir -p ~/.local/bin
mv fenestra-linux-amd64 ~/.local/bin/fenestra
```

Stellen Sie sicher, dass `~/.local/bin` in Ihrem `PATH` enthalten ist.

## Verwendung

```bash
fenestra ~/projects/app/dist/index.html   # open a build
fenestra ~/projects/app/dist              # open a folder
fenestra --serve ~/index.html             # force the server
fenestra --port 8080 ~/index.html         # pick the port
fenestra --browser firefox ~/app/index.html
fenestra --browsers                       # what is installed
```

## Härtung

- Es werden nur Loopback-`Host`-Header akzeptiert (`127.0.0.1`, `localhost`), was
  DNS-Rebinding blockiert.
- Symlinks, die außerhalb der ausgelieferten Wurzel auflösen, werden abgelehnt.
- Antworten tragen `ETag` und `Last-Modified`, werden mit `304` revalidiert und
  unterstützen `Range` für große Mediendateien.

## Quellcode-Einstiegspunkte

Wenn das HTML auf `/src/` verweist (ein Dev-Einstiegspunkt von Vite oder ähnlich),
liefert Fenestra es nicht aus: Es sucht daneben nach `dist/`, `build/` oder `out/`
und liefert stattdessen diesen Build aus. Gibt es keinen Build, bricht es mit einer
Meldung ab, die Sie auffordert, zuerst den Build auszuführen.

## Den `.html`-Handler registrieren

```bash
fenestra --install
```

Dies schreibt `~/.local/share/applications/fenestra.desktop`, aktualisiert die
Desktop-Datenbank und ruft auf:

```bash
xdg-mime default fenestra.desktop text/html
```

Zum Rückgängigmachen:

```bash
fenestra --uninstall
```

Wenn `xdg-mime` fehlt, installieren Sie `xdg-utils` oder führen Sie den obigen
Befehl von Hand aus.

## Browser

Die Erkennung durchsucht den `PATH` in dieser Reihenfolge: `microsoft-edge`,
`google-chrome`, `chromium`, `firefox`, `brave-browser`, `vivaldi`, `opera`. Die
Präferenz wird in `~/.config/fenestra/browser` gespeichert; `--browser default`
übergibt an `xdg-open`.

## Wo Statusdaten liegen

| Pfad | Was |
|---|---|
| `~/.config/fenestra/browser` | bevorzugter Browser |
| `~/.local/share/fenestra/fenestra.log` | Server-Log |
| `~/.local/share/fenestra/` | eine Lock-Datei pro ausgeliefertem Ordner (stabiler Port) |

`XDG_CONFIG_HOME` und `XDG_DATA_HOME` werden berücksichtigt.

## Aus dem Quellcode bauen

Go 1.21+, kein cgo:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
go test ./...
```

Für ARM cross-kompilieren:

```bash
GOOS=linux GOARCH=arm64 CGO_ENABLED=0 go build -o fenestra-arm64 .
```

## Unterschiede zu den anderen Builds

- Keine Registry und kein Finder: Die `.desktop`-Datei plus `xdg-mime` ist der Weg
  zur Dateizuordnung.
- Statusdaten liegen unter XDG-Pfaden, nicht unter `%LOCALAPPDATA%` oder
  `~/Library/Application Support`.
- Derselbe Server, dieselben Flags, derselbe SPA-Fallback wie unter Windows und
  macOS.

## Fehlerbehebung

- **`could not find <browser> installed on this machine`** – führen Sie
  `fenestra --browsers` aus, um die erkannten Browser zu sehen, oder verwenden Sie
  `--browser default`.
- **`xdg-mime failed`** – installieren Sie `xdg-utils`, oder legen Sie den Handler
  manuell fest.
- **Es öffnet sich nichts** – `--browser default` verwendet `xdg-open`; prüfen Sie,
  ob dieser Befehl allein funktioniert.
- **Keine Zuordnung nach `--install`** – manche Desktops cachen MIME-Handler;
  melden Sie sich ab oder führen Sie
  `update-desktop-database ~/.local/share/applications` aus.

## In der CI getestet

`ubuntu-latest`: `go test`, Build und ein echter HTTP-Smoke-Test. ARM64 wird in
derselben Pipeline cross-kompiliert; es wird nicht ausgeführt.
