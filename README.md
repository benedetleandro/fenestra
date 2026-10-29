# Fenestra

**Double-click a local `dist/index.html`. It opens in Edge like a website — because Fenestra served it.**

No terminal. No `npx serve`. No extension to enable. No custom browser window.

![build](https://github.com/benedetleandro/fenestra/actions/workflows/build.yml/badge.svg)
![license](https://img.shields.io/badge/license-MIT-blue.svg)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)

[**Website →**](https://benedetleandro.github.io/fenestra/) · [Português](README.pt-BR.md)

> **Status: alpha.** The HTTP server and the Edge integration work. The file
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
   stable port, and opens a normal Edge tab at that URL.
3. **If it doesn't**, it opens the file directly in Edge via `file://`.
   No server is started.

The server keeps the same port per folder, so `localStorage`, cookies and the
service worker survive between sessions.

## Install

Requirements: Windows 10/11. The .NET Framework 4.x and Microsoft Edge are
already there.

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
Fenestra.exe --install                      # register in the "Open with" list
Fenestra.exe --uninstall                    # remove it
```

### Make it the default for `.html` (one time)

1. Run `Fenestra.exe --install`.
2. Right-click any `index.html` → **Open with** → **Choose another app**.
3. Select **Fenestra** and check **Always use this app**.

Windows allows exactly one handler per extension, so this confirmation cannot
be automated. The handler is invisible; the window that opens is Edge.

### Headless (tests / CI)

Set `FENESTRA_NO_OPEN=1` and the server starts without opening a browser.
The port is written to `%LOCALAPPDATA%\Fenestra\sessions\<hash>.txt`.

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
  opens another Edge tab.
- **Lifetime**: the server exits when the Edge process it started exits, or
  after 4 idle hours if Edge was already running.

## Security

Fenestra is a local tool: loopback only, no TLS, no authentication, no
telemetry. It serves every file under the opened folder — do not point it at a
directory with secrets. See [SECURITY.md](SECURITY.md) for the full threat model.

## Roadmap

- [ ] `--root <folder>` for builds whose entry lives in a subfolder
- [ ] Linux and macOS (the server is portable; the Edge/registry code is not)
- [ ] Code signing for releases, to remove the SmartScreen warning
- [ ] Portable mode (no `%LOCALAPPDATA%`)
- [ ] More tests: HEAD, ETag, range requests, UTF-8 paths
- [ ] A 15-second demo recording

## Contributing

Small tool, small surface. Read [CONTRIBUTING.md](CONTRIBUTING.md); the
`good first issues` list is there.

## License

[MIT](LICENSE) © Fenestra contributors
