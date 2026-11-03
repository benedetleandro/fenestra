# Fenestra unter macOS

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of macos/README.md @ 0.7.0 -->


Dieser Ordner ist der Baukasten. Sie kompilieren auf Ihrem Rechner – es gibt keine
vorgefertigte Binärdatei zum Herunterladen, und das ist Absicht: Eine lokal gebaute
App durchläuft nie Gatekeeper.

## Voraussetzung: Go 1.21+

Der offizielle Installer unter <https://go.dev/dl/> ist von Google signiert und
notarisiert, daher **erscheint keine Warnung**. Er ist das Einzige, was Sie
installieren.

## Weg A – ohne git (neuer Mac, keine Command Line Tools)

Ein frisch zurückgesetzter Mac hat kein `git`: Der Befehl löst die Installation der
Command Line Tools aus (~1,5 GB). Das können Sie überspringen:

1. Laden Sie das ZIP herunter: <https://github.com/benedetleandro/fenestra/archive/refs/heads/main.zip>
2. Entpacken Sie es
3. Im Terminal:

```bash
cd ~/Downloads/fenestra-main/macos
bash build.command
```

`bash` ist Teil des Systems, und das Skript wird **gelesen**, nicht vom Finder
ausgeführt – Gatekeeper ist also nicht beteiligt. `fenestra` und `Fenestra.app`
entstehen lokal, ohne Quarantäne.

## Weg B – git clone (wenn Sie bereits git haben)

```bash
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra/macos
./build.command
```

## Weg C – Doppelklick

Nur wenn Sie bereits Go haben und das Terminal vermeiden möchten. macOS fragt
möglicherweise einmal nach einer Berechtigung:
**Systemeinstellungen → Datenschutz & Sicherheit → Dennoch öffnen**.

## Verwendung

```bash
./fenestra ~/projects/app/dist/index.html   # opens in the default browser
./fenestra --browsers                       # what is installed
./fenestra --serve ~/index.html             # force the server
./fenestra --port 8080 ~/index.html         # pick the port
./fenestra --browser firefox                # switch browsers
./fenestra --browser safari                 # Safari is in the list too
./fenestra --install                        # Finder instructions
```

Für den Finder:

1. Ziehen Sie `Fenestra.app` nach `/Applications`.
2. Klicken Sie mit der rechten Maustaste auf eine `index.html` → **Öffnen mit** → **Fenestra**.
3. Setzen Sie ein Häkchen bei **Immer diese App verwenden**.

Wenn das HTML auf `/src/` verweist (ein Dev-Einstiegspunkt), nutzt Fenestra
stattdessen den danebenliegenden Build aus `dist/`, `build/` oder `out/` und erklärt,
was zu tun ist, wenn keiner existiert.

## Homebrew (nur CLI)

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` ist eine einmalige Voraussetzung für Taps von Drittanbietern. Dies
installiert nur die Kommandozeile; `Fenestra.app` entsteht weiterhin über
`./build.command`.

## Wie die `.app` funktioniert

`Fenestra.app` ist ein AppleScript-*Droplet* (gebaut mit `osacompile`, das macOS
mitbringt). Es empfängt die Dateien, die der Finder ihm übergibt, und ruft die
Go-Binärdatei im Bundle auf. Das ist der Grund, warum "Öffnen mit" ohne Cocoa-App
funktioniert.

## Wenn etwas fehlschlägt

Das Skript gibt `FAILED at line N` aus. Kopieren Sie diese Meldung.

Der manuelle Weg zeigt unterdessen, wo das Problem liegt:

```bash
cd fenestra/linux
go version
GOOS=darwin CGO_ENABLED=0 go build -o ../macos/fenestra .
../macos/fenestra ~/path/dist/index.html
```

Wenn `go build` funktioniert und `./fenestra` die Seite öffnet, liegt das Problem nur
in der Verpackung der `.app` – und das Programm ist bereits nutzbar.

Wenn `osacompile` fehlschlägt (es könnte die Command Line Tools anfordern), warnt das
Skript und behält die Binärdatei. In diesem Fall gibt es keine Zuordnung im Finder,
aber das Terminal funktioniert.

## Ehrliche Einschränkungen

- **In der CI getestet, auf einem echten `macos-latest`**: Build,
  `codesign --verify`, das Droplet, das eine Datei über den Finder (`open -a`) erhält,
  und der Server, der per HTTP antwortet.
- **Nicht in einer grafischen Sitzung getestet**: Der Finder-Klick, das Menü
  "Öffnen mit" und der Gatekeeper-Dialog existieren nur auf einem Bildschirm – der
  Runner ist headless.
- Die `.app` wird von `osacompile` erzeugt; fehlt das Werkzeug auf Ihrem macOS, warnt
  das Skript und baut nur die Binärdatei.
- Jeder kompiliert seine eigene. Wer die `.app` per AirDrop auf einen anderen Rechner
  schickt, fügt Quarantäne hinzu, und Gatekeeper blockiert sie erneut.
- Keine Entwicklersignatur, keine Notarisierung, keine 99 US-Dollar pro Jahr.

## Rückgängig machen

Löschen Sie `Fenestra.app` und `fenestra` aus diesem Ordner. Wählen Sie für den
Finder im Menü **Öffnen mit → Immer** eine andere App.
