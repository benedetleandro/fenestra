# Fenestra

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of README.md @ 0.7.0 -->

**Doppelklicken Sie auf eine lokale `dist/index.html`. Sie öffnet sich im Browser wie eine Website – weil Fenestra sie ausgeliefert hat.**

Kein Terminal. Kein `npx serve`. Keine Erweiterung, die aktiviert werden muss. Kein
eigenes Browserfenster.

![build](https://github.com/benedetleandro/fenestra/actions/workflows/build.yml/badge.svg)
![license](https://img.shields.io/badge/license-MIT-blue.svg)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)

[**Website →**](https://benedetleandro.github.io/fenestra/) · [Datenschutz](https://benedetleandro.github.io/fenestra/privacy.html)

> **Status: 1.1.** Der HTTP-Server und die Browser-Integration funktionieren. Die
> Dateizuordnung erfordert eine einmalige Bestätigung durch Sie – Windows erlaubt es
> einer Anwendung nicht, von sich aus zum Standardhandler zu werden.

## Das Problem

Ein moderner Build (`Vite`, `React`, `Svelte`, alles mit ES-Modulen) läuft nicht
über `file://`. Der Browser lehnt Modulskripte per CORS ab, `/assets/...` zeigt auf
die Wurzel des Laufwerks, und Service Worker stehen nicht zur Verfügung. Sie
benötigen einen HTTP-Origin.

Normalerweise bedeutet das, ein Terminal zu öffnen und einen statischen Server zu
starten. Fenestra erspart Ihnen diesen Schritt: Es *ist* der statische Server,
verbunden mit dem Doppelklick.

## Was es tut

Wenn Sie eine `.html`-Datei öffnen:

1. Fenestra liest die Datei und entscheidet, ob sie einen HTTP-Origin benötigt
   (ES-Module, wurzelabsolute Pfade, `fetch`, Service Worker, Web-Manifest).
2. **Wenn ja**, liefert es den enthaltenden Ordner unter `127.0.0.1` auf einem
   stabilen Port aus und öffnet einen normalen Tab in Ihrem Browser unter dieser URL.
3. **Wenn nicht**, öffnet es die Datei direkt über `file://`. Es wird kein Server gestartet.

Der Server behält pro Ordner denselben Port, sodass `localStorage`, Cookies und der
Service Worker Sitzungen überdauern.

## Installation

Voraussetzungen: Windows 10/11. Das .NET Framework 4.x und Edge sind bereits
vorhanden – und jeder Chromium-basierte Browser oder Firefox funktioniert ebenfalls.
Unter Linux gibt es einen separaten nativen Build, siehe [Linux](#linux).

```powershell
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Die Binärdatei ist `bin\Fenestra.exe`. Kein SDK, kein NuGet, kein Internet zum Bauen
erforderlich.

## Verwendung

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

### Zum Standard für `.html` machen (einmalig)

1. Führen Sie `Fenestra.exe --install` aus.
2. Klicken Sie mit der rechten Maustaste auf eine beliebige `index.html` → **Öffnen mit** → **Andere App auswählen**.
3. Wählen Sie **Fenestra** und setzen Sie ein Häkchen bei **Immer diese App verwenden**.

Windows erlaubt genau einen Handler pro Dateiendung, daher lässt sich diese
Bestätigung nicht automatisieren. Der Handler ist unsichtbar; das Fenster, das sich
öffnet, ist Ihr Browser.

### Browser

**Der Systembrowser ist der Standard.** Chrome, Firefox, Brave, Vivaldi, Opera und
Chromium werden automatisch erkannt, und `--browser edge` legt Edge fest. Unter
macOS steht auch **Safari** in der Liste.

```powershell
Fenestra.exe --browsers              # what is installed
Fenestra.exe --browser chrome        # use Chrome from now on
Fenestra.exe --browser default       # let Windows decide
```

Die Auswahl wird in `%LOCALAPPDATA%\Fenestra\browser.txt` gespeichert und gilt für
jeden Doppelklick, sodass Sie das Flag nicht erneut angeben müssen.

### Headless (Tests / CI)

Mit `FENESTRA_NO_OPEN=1` startet der Server, ohne einen Browser zu öffnen.
Der Port wird in `%LOCALAPPDATA%\Fenestra\sessions\<hash>.txt` geschrieben.

## Linux

Ein separater, nativer Build liegt in [`linux/`](linux): eine statische Binärdatei,
keine Laufzeitumgebung, kein Wine. In Go geschrieben und für `linux/amd64` und
`linux/arm64` cross-kompiliert. Vollständige Anleitung:
[linux/README.de.md](linux/README.de.md).

```bash
chmod +x fenestra-linux-amd64
./fenestra-linux-amd64 ~/projects/app/dist/index.html
```

Gleiches Verhalten wie beim Windows-Build: stabiler Port pro Ordner, SPA-Fallback,
Pfadbegrenzung und Browser-Auswahl.

```bash
./fenestra-linux-amd64 --browsers
./fenestra-linux-amd64 --browser firefox
./fenestra-linux-amd64 --install      # .desktop + xdg-mime
```

`--install` schreibt `~/.local/share/applications/fenestra.desktop` und ruft
`xdg-mime default` auf. Dafür wird `xdg-utils` benötigt, das die meisten Desktops
bereits mitbringen.

Aus dem Quellcode bauen:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
```

## macOS

Die **CLI** ist als vorgefertigte Binärdatei über Homebrew verfügbar:

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` ist eine einmalige Bestätigung, die Homebrew für Taps von
Drittanbietern verlangt.

Die **Finder-App** (`Fenestra.app`) hat bewusst keine vorgefertigte Binärdatei: Ein
nicht signiertes Bundle wird von Gatekeeper blockiert, und die Notarisierung kostet
99 US-Dollar pro Jahr. [`macos/`](macos) ist der Baukasten: Go installieren
(offizieller signierter Installer, keine Warnung), dann `git clone` und einen Befehl
ausführen. Ein Klon mit git setzt kein Quarantäne-Attribut, daher fragt macOS nie
nach einer Berechtigung. Ohne git laden Sie das ZIP herunter und führen
`bash build.command` aus. Siehe [macos/README.de.md](macos/README.de.md).

Der Baukasten wird in CI auf einem echten `macos-latest`-Runner gebaut und getestet:
Build, `codesign --verify`, das Droplet, das eine Datei über `open -a` erhält, und
der Server, der per HTTP antwortet. Nur die Finder-Oberfläche selbst ist ungetestet
(Runner sind headless).

## Wann es funktioniert

| Fall | Funktioniert? | Warum |
|---|---|---|
| Statischer Build (`dist`, `build`, `out`) | **Ja** | reines HTML/CSS/JS |
| SPA mit clientseitigen Routen | **Ja** | Server greift bei Pfaden ohne Dateiendung auf `index.html` zurück |
| Einfaches HTML in einer Datei | **Ja** | über `file://` geöffnet, kein Server |
| Quellprojekt (`/src/main.tsx`) | **Nein** | benötigt den Dev-Server des Projekts zum Kompilieren |
| App mit Backend/API | **Nein** | Fenestra liefert Dateien aus; es fungiert nicht als Proxy und startet keine Dienste |
| Einstiegspunkt in einem Unterordner mit `../` | **Nein** | Wurzel ist der Ordner der Datei; `..` ist aus Sicherheitsgründen begrenzt |

Wenn die Erkennung danebenliegt, erzwingt `--serve` den HTTP-Origin.

## Wie es funktioniert

Eine einzige C#-Datei, keine Abhängigkeiten, gebaut mit dem `csc.exe`, das Windows
mitbringt.

- **HTTP/1.1-Server** auf einem `TcpListener`, ausschließlich an `127.0.0.1` gebunden.
- **Pfadauflösung**: normalisieren, `..`, `:` und `::$DATA` ablehnen, auf die
  ausgelieferte Wurzel begrenzen.
- **Portzuweisung**: FNV-1a-Hash des Ordners → Port in `51000-63000`, in einer
  Lock-Datei pro Ordner gespeichert und beim nächsten Lauf wiederverwendet.
- **SPA-Fallback**: 404-Antworten ohne Dateiendung liefern `index.html`.
- **Einzelinstanz**: Ein zweiter Doppelklick fragt den laufenden Server an und
  öffnet nur einen weiteren Browser-Tab.
- **Lebensdauer**: Der Server beendet sich, wenn der von ihm gestartete
  Browserprozess endet, oder nach 4 Stunden Inaktivität, wenn der Browser bereits
  lief.

## Sicherheit

Fenestra ist ein lokales Werkzeug: nur Loopback, kein TLS, keine Authentifizierung,
keine Telemetrie. Es liefert jede Datei im geöffneten Ordner aus – richten Sie es
nicht auf ein Verzeichnis mit Geheimnissen. Siehe [SECURITY.de.md](SECURITY.de.md)
für das vollständige Bedrohungsmodell und die
[Datenschutzseite](https://benedetleandro.github.io/fenestra/privacy.html) dafür,
was diese Website und die App speichern.

## Roadmap

- [ ] `--root <folder>` für Builds, deren Einstiegspunkt in einem Unterordner liegt
- [x] Linux: nativer Go-Build in [`linux/`](linux), amd64 und arm64
- [x] macOS: Homebrew-CLI und der Finder-Baukasten in [`macos/`](macos)
- [ ] Codesignierung für Releases, um die SmartScreen-Warnung zu beseitigen
- [ ] Portabler Modus (ohne `%LOCALAPPDATA%`)
- [ ] Mehr Tests: HEAD, ETag, Range-Requests, UTF-8-Pfade
- [ ] Eine 15-sekündige Demo-Aufnahme

## Datenschutz

Die Website und die App verwenden keine Cookies, keine Analyse, keine Werbung und
keine Telemetrie. Die einzige lokale Speicherung sind die von Ihnen gewählte Sprache
und der von Ihnen weggeklickte Datenschutzhinweis. Details:
[Datenschutzseite](https://benedetleandro.github.io/fenestra/privacy.html).

## Mitwirken

Kleines Werkzeug, kleine Angriffsfläche. Lesen Sie
[CONTRIBUTING.de.md](CONTRIBUTING.de.md); die Liste `good first issues` finden Sie
dort.

## Lizenz

[MIT](LICENSE) © Fenestra-Mitwirkende
