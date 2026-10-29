# Fenestra on Linux

English · [Português](README.pt-BR.md)

Fenestra for Linux is a single static Go binary. No runtime, no GUI, no root.

## Install

Download the binary for your architecture from the
[latest release](https://github.com/benedetleandro/fenestra/releases/latest):

- `fenestra-linux-amd64`
- `fenestra-linux-arm64`

```bash
chmod +x fenestra-linux-amd64
mkdir -p ~/.local/bin
mv fenestra-linux-amd64 ~/.local/bin/fenestra
```

Make sure `~/.local/bin` is on your `PATH`.

## Use

```bash
fenestra ~/projects/app/dist/index.html   # open a build
fenestra ~/projects/app/dist              # open a folder
fenestra --serve ~/index.html             # force the server
fenestra --port 8080 ~/index.html         # pick the port
fenestra --browser firefox ~/app/index.html
fenestra --browsers                       # what is installed
```

## Hardening

- Only loopback `Host` headers are accepted (`127.0.0.1`, `localhost`), which
  blocks DNS rebinding.
- Symlinks that resolve outside the served root are rejected.
- Responses carry `ETag` and `Last-Modified`, revalidate with `304`, and support
  `Range` for large media.

## Register the `.html` handler

```bash
fenestra --install
```

This writes `~/.local/share/applications/fenestra.desktop`, refreshes the desktop
database and calls:

```bash
xdg-mime default fenestra.desktop text/html
```

To undo:

```bash
fenestra --uninstall
```

If `xdg-mime` is missing, install `xdg-utils` or run the command above by hand.

## Browsers

Detection walks the `PATH`, in this order: `microsoft-edge`, `google-chrome`,
`chromium`, `firefox`, `brave-browser`, `vivaldi`, `opera`. The preference is
stored in `~/.config/fenestra/browser`; `--browser default` hands off to
`xdg-open`.

## Where it keeps state

| Path | What |
|---|---|
| `~/.config/fenestra/browser` | preferred browser |
| `~/.local/share/fenestra/fenestra.log` | server log |
| `~/.local/share/fenestra/` | one lock file per served folder (stable port) |

`XDG_CONFIG_HOME` and `XDG_DATA_HOME` are respected.

## Build from source

Go 1.21+, no cgo:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
go test ./...
```

Cross-compile for ARM:

```bash
GOOS=linux GOARCH=arm64 CGO_ENABLED=0 go build -o fenestra-arm64 .
```

## Differences from the other builds

- No registry and no Finder: the `.desktop` file plus `xdg-mime` is the
  association route.
- State lives under XDG paths, not `%LOCALAPPDATA%` or
  `~/Library/Application Support`.
- Same server, same flags, same SPA fallback as Windows and macOS.

## Troubleshooting

- **`could not find <browser> installed on this machine`** — run
  `fenestra --browsers` to see what was detected, or use `--browser default`.
- **`xdg-mime failed`** — install `xdg-utils`, or set the handler manually.
- **Nothing opens** — `--browser default` uses `xdg-open`; check that it works
  on its own.
- **No association after `--install`** — some desktops cache MIME handlers; log
  out, or run `update-desktop-database ~/.local/share/applications`.

## Tested in CI

`ubuntu-latest`: `go test`, build, and a real HTTP smoke test. ARM64 is
cross-compiled in the same pipeline; it is not executed.
