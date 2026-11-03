# Fenestra su Linux

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of linux/README.md @ 0.7.0 -->


Fenestra per Linux è un unico binario statico in Go. Nessun runtime, nessuna GUI, nessun root.

## Installazione

Scarica il binario per la tua architettura dalla
[ultima release](https://github.com/benedetleandro/fenestra/releases/latest):

- `fenestra-linux-amd64`
- `fenestra-linux-arm64`

```bash
chmod +x fenestra-linux-amd64
mkdir -p ~/.local/bin
mv fenestra-linux-amd64 ~/.local/bin/fenestra
```

Assicurati che `~/.local/bin` sia nel tuo `PATH`.

## Utilizzo

```bash
fenestra ~/projects/app/dist/index.html   # open a build
fenestra ~/projects/app/dist              # open a folder
fenestra --serve ~/index.html             # force the server
fenestra --port 8080 ~/index.html         # pick the port
fenestra --browser firefox ~/app/index.html
fenestra --browsers                       # what is installed
```

## Hardening

- Vengono accettati solo header `Host` di loopback (`127.0.0.1`, `localhost`), il che
  blocca il DNS rebinding.
- I symlink che risolvono fuori dalla radice servita vengono rifiutati.
- Le risposte portano `ETag` e `Last-Modified`, si rivalidano con `304` e supportano
  `Range` per i media di grandi dimensioni.

## Entry dei sorgenti

Se l'HTML fa riferimento a `/src/` (una entry di sviluppo Vite o simile), Fenestra non la
serve: cerca una build `dist/`, `build/` o `out/` accanto ad essa e serve quella
al suo posto. Quando non c'è alcuna build, si ferma con un messaggio che ti dice di
eseguire prima la build.

## Registrare l'handler per `.html`

```bash
fenestra --install
```

Questo scrive `~/.local/share/applications/fenestra.desktop`, aggiorna il database
del desktop e chiama:

```bash
xdg-mime default fenestra.desktop text/html
```

Per annullare:

```bash
fenestra --uninstall
```

Se `xdg-mime` manca, installa `xdg-utils` oppure esegui il comando qui sopra a mano.

## Browser

Il rilevamento percorre il `PATH`, in questo ordine: `microsoft-edge`, `google-chrome`,
`chromium`, `firefox`, `brave-browser`, `vivaldi`, `opera`. La preferenza è
salvata in `~/.config/fenestra/browser`; `--browser default` passa il compito a
`xdg-open`.

## Dove conserva lo stato

| Percorso | Cosa |
|---|---|
| `~/.config/fenestra/browser` | browser preferito |
| `~/.local/share/fenestra/fenestra.log` | log del server |
| `~/.local/share/fenestra/` | un file di lock per cartella servita (porta stabile) |

`XDG_CONFIG_HOME` e `XDG_DATA_HOME` vengono rispettate.

## Compilare dai sorgenti

Go 1.21+, senza cgo:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
go test ./...
```

Cross-compila per ARM:

```bash
GOOS=linux GOARCH=arm64 CGO_ENABLED=0 go build -o fenestra-arm64 .
```

## Differenze rispetto alle altre build

- Niente registro e niente Finder: il file `.desktop` più `xdg-mime` sono la
  via per l'associazione.
- Lo stato vive nei percorsi XDG, non in `%LOCALAPPDATA%` o
  `~/Library/Application Support`.
- Stesso server, stessi flag, stesso fallback SPA di Windows e macOS.

## Risoluzione dei problemi

- **`could not find <browser> installed on this machine`** — esegui
  `fenestra --browsers` per vedere cosa è stato rilevato, oppure usa `--browser default`.
- **`xdg-mime failed`** — installa `xdg-utils`, oppure imposta l'handler a mano.
- **Non si apre nulla** — `--browser default` usa `xdg-open`; verifica che funzioni
  da solo.
- **Nessuna associazione dopo `--install`** — alcuni desktop mettono in cache gli handler MIME; esci
  e rientra, oppure esegui `update-desktop-database ~/.local/share/applications`.

## Testato in CI

`ubuntu-latest`: `go test`, build e un vero smoke test HTTP. ARM64 viene
cross-compilato nella stessa pipeline; non viene eseguito.
