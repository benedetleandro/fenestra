# Fenestra

**Double-click a local `dist/index.html`. It opens in your browser like a website — because Fenestra served it.**

No terminal. No `npx serve`. No extension to enable. No custom browser window.

![build](https://github.com/benedetleandro/fenestra/actions/workflows/build.yml/badge.svg)
![license](https://img.shields.io/badge/license-MIT-blue.svg)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)

[**Website →**](https://benedetleandro.github.io/fenestra/) · [Português](README.pt-BR.md)

> **Status: alpha.** The HTTP server and the browser integration work. The file
> association requires a one-time confirmation from the user — Windows does not
> allow an app to become the default handler on its own.

## The problem

A modern build (`Vite`, `React`, `Svelte`, anything with ES modules) does not
run from `file://`. The browser refuses module scripts by CORS, `/assets/...`
points at the root of the drive, and service workers are unavailable. You need
an HTTP origin.

Usually that means opening a terminal and running a static server. Fenestra
removes that step: it *is* the static server, attached to the double-click.

## What it does

When you open an `.html` file:

1. Fenestra reads the file and decides whether it needs an HTTP origin
   (ES modules, root-absolute paths, `fetch`, service worker, web manifest).
2. **If it needs one**, it serves the containing folder on `127.0.0.1`, on a
   stable port, and opens a normal tab in your browser at that URL.
3. **If it doesn't**, it opens the file directly via `file://`. No server is started.

The server keeps the same port per folder, so `localStorage`, cookies and the
service worker survive between sessions.

## Install

Requirements: Windows 10/11. The .NET Framework 4.x and Edge are already there —
and any Chromium-based browser or Firefox works too. On Linux there is a separate
native build, see [Linux](#linux).

```powershell
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The binary is `bin\Fenestra.exe`. No SDK, no NuGet, no internet needed to build.

## Usage

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

### Make it the default for `.html` (one time)

1. Run `Fenestra.exe --install`.
2. Right-click any `index.html` → **Open with** → **Choose another app**.
3. Select **Fenestra** and check **Always use this app**.

Windows allows exactly one handler per extension, so this confirmation cannot
be automated. The handler is invisible; the window that opens is your browser.

### Browsers

**Edge is the default.** Chrome, Firefox, Brave, Vivaldi, Opera and Chromium are
detected automatically. On macOS, **Safari** is in the list too.

```powershell
Fenestra.exe --browsers              # what is installed
Fenestra.exe --browser chrome        # use Chrome from now on
Fenestra.exe --browser default       # let Windows decide
```

The choice is stored in `%LOCALAPPDATA%\Fenestra\browser.txt` and applies to
every double-click, so you do not need to pass a flag again.

### Headless (tests / CI)

Set `FENESTRA_NO_OPEN=1` and the server starts without opening a browser.
The port is written to `%LOCALAPPDATA%\Fenestra\sessions\<hash>.txt`.

## Linux

A separate, native build lives in [`linux/`](linux): one static binary, no
runtime, no Wine. Written in Go and cross-compiled to `linux/amd64` and
`linux/arm64`.

```bash
chmod +x fenestra-linux-amd64
./fenestra-linux-amd64 ~/projects/app/dist/index.html
```

Same behavior as the Windows build: stable port per folder, SPA fallback, path
confinement and browser choice.

```bash
./fenestra-linux-amd64 --browsers
./fenestra-linux-amd64 --browser firefox
./fenestra-linux-amd64 --install      # .desktop + xdg-mime
```

`--install` writes `~/.local/share/applications/fenestra.desktop` and calls
`xdg-mime default`. It needs `xdg-utils`, which most desktops already have.

Build from source:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
```

## macOS

There is **no prebuilt macOS binary**, on purpose. Distributing one would require
Apple notarization (US$ 99/year); without it, the download is blocked by
Gatekeeper.

Instead, [`macos/`](macos) is a build kit: install Go (official signed installer,
no warning), then `git clone` and run one command. Cloning with git does not add
the quarantine attribute, so the macOS never asks for permission. Without git,
download the ZIP and run `bash build.command` — the shell reads the script, so
Gatekeeper is not involved either. See [macos/README.md](macos/README.md).

The kit is built and exercised on a real `macos-latest` runner in CI: build,
`codesign --verify`, the droplet receiving a file through `open -a`, and the
server answering HTTP. Only the Finder UI itself is untested (runners are
headless).

## When it works

| Case | Works? | Why |
|---|---|---|
| Static build (`dist`, `build`, `out`) | **Yes** | plain HTML/CSS/JS |
| SPA with client-side routes | **Yes** | server falls back to `index.html` for extensionless paths |
| Plain single-file HTML | **Yes** | opened via `file://`, no server |
| Source project (`/src/main.tsx`) | **No** | needs the project's dev server to compile |
| App with a backend/API | **No** | Fenestra serves files; it does not proxy or run services |
| Entry in a subfolder referencing `../` | **No** | root is the file's folder; `..` is confined for safety |

If detection gets it wrong, `--serve` forces the HTTP origin.

## How it works

One C# file, no dependencies, built with the `csc.exe` that ships with Windows.

- **HTTP/1.1 server** on a `TcpListener`, bound to `127.0.0.1` only.
- **Path resolution**: normalize, reject `..`, `:` and `::$DATA`, confine to the
  served root.
- **Port allocation**: FNV-1a hash of the folder → port in `51000-63000`, stored
  in a per-folder lock file and reused on the next run.
- **SPA fallback**: extensionless 404s return `index.html`.
- **Single instance**: a second double-click pings the running server and only
  opens another browser tab.
- **Lifetime**: the server exits when the browser process it started exits, or
  after 4 idle hours if the browser was already running.

## Security

Fenestra is a local tool: loopback only, no TLS, no authentication, no
telemetry. It serves every file under the opened folder — do not point it at a
directory with secrets. See [SECURITY.md](SECURITY.md) for the full threat model.

## Roadmap

- [ ] `--root <folder>` for builds whose entry lives in a subfolder
- [x] Linux: native Go build in [`linux/`](linux), amd64 and arm64
- [ ] macOS is **not** planned: it requires Apple Developer notarization
- [ ] Code signing for releases, to remove the SmartScreen warning
- [ ] Portable mode (no `%LOCALAPPDATA%`)
- [ ] More tests: HEAD, ETag, range requests, UTF-8 paths
- [ ] A 15-second demo recording

## Contributing

Small tool, small surface. Read [CONTRIBUTING.md](CONTRIBUTING.md); the
`good first issues` list is there.

## License

[MIT](LICENSE) © Fenestra contributors
