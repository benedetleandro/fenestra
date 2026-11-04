# Fenestra

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of README.md @ 0.7.0 -->

**Doppio clic su un `dist/index.html` locale. Si apre nel tuo browser come un sito web — perché è Fenestra ad averlo servito.**

Niente terminale. Niente `npx serve`. Nessuna estensione da abilitare. Nessuna finestra di browser personalizzata.

![build](https://github.com/benedetleandro/fenestra/actions/workflows/build.yml/badge.svg)
![license](https://img.shields.io/badge/license-MIT-blue.svg)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)

[**Sito web →**](https://benedetleandro.github.io/fenestra/) · [Privacy](https://benedetleandro.github.io/fenestra/privacy.html)

> **Stato: 1.1.** Il server HTTP e l'integrazione con il browser funzionano.
> L'associazione dei file richiede una conferma una tantum da parte dell'utente:
> Windows non permette a un'app di diventare da sola il gestore predefinito.

## Il problema

Una build moderna (`Vite`, `React`, `Svelte`, qualsiasi cosa con i moduli ES) non
funziona da `file://`. Il browser rifiuta gli script dei moduli per via del CORS,
`/assets/...` punta alla radice del disco e i service worker non sono disponibili.
Serve un'origine HTTP.

Di solito questo significa aprire un terminale ed eseguire un server statico.
Fenestra elimina quel passaggio: è *lei stessa* il server statico, agganciato al
doppio clic.

## Cosa fa

Quando apri un file `.html`:

1. Fenestra legge il file e decide se ha bisogno di un'origine HTTP
   (moduli ES, percorsi assoluti dalla radice, `fetch`, service worker, web manifest).
2. **Se ne ha bisogno**, serve la cartella che lo contiene su `127.0.0.1`, su una
   porta stabile, e apre una normale scheda del browser a quell'URL.
3. **Se non ne ha bisogno**, apre il file direttamente via `file://`. Nessun server viene avviato.

Il server mantiene la stessa porta per ogni cartella, così `localStorage`, i cookie e
il service worker sopravvivono tra una sessione e l'altra.

## Installazione

Requisiti: Windows 10/11. Il .NET Framework 4.x ed Edge sono già presenti —
e funziona anche qualsiasi browser basato su Chromium o Firefox. Su Linux esiste una
build nativa separata, vedi [Linux](#linux).

```powershell
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Il binario è `bin\Fenestra.exe`. Nessun SDK, nessun NuGet, nessuna connessione a internet per compilare.

## Utilizzo

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

### Renderlo predefinito per `.html` (una volta sola)

1. Esegui `Fenestra.exe --install`.
2. Fai clic con il tasto destro su un `index.html` qualsiasi → **Apri con** → **Scegli un'altra app**.
3. Seleziona **Fenestra** e spunta **Usa sempre questa app**.

Windows permette esattamente un gestore per estensione, quindi questa conferma non
può essere automatizzata. Il gestore è invisibile; la finestra che si apre è il tuo browser.

### Browser

**Il browser di sistema è quello predefinito.** Chrome, Firefox, Brave, Vivaldi, Opera e
Chromium vengono rilevati automaticamente, e `--browser edge` fissa Edge. Su macOS
c'è anche **Safari** nella lista.

```powershell
Fenestra.exe --browsers              # what is installed
Fenestra.exe --browser chrome        # use Chrome from now on
Fenestra.exe --browser default       # let Windows decide
```

La scelta viene salvata in `%LOCALAPPDATA%\Fenestra\browser.txt` e vale per
ogni doppio clic, quindi non devi più passare il flag.

### Headless (test / CI)

Imposta `FENESTRA_NO_OPEN=1` e il server si avvia senza aprire un browser.
La porta viene scritta in `%LOCALAPPDATA%\Fenestra\sessions\<hash>.txt`.

## Linux

Una build nativa separata vive in [`linux/`](linux): un unico binario statico, nessun
runtime, niente Wine. Scritta in Go e cross-compilata per `linux/amd64` e
`linux/arm64`. Guida completa: [linux/README.it.md](linux/README.it.md).

```bash
chmod +x fenestra-linux-amd64
./fenestra-linux-amd64 ~/projects/app/dist/index.html
```

Stesso comportamento della build Windows: porta stabile per cartella, fallback SPA,
confinamento dei percorsi e scelta del browser.

```bash
./fenestra-linux-amd64 --browsers
./fenestra-linux-amd64 --browser firefox
./fenestra-linux-amd64 --install      # .desktop + xdg-mime
```

`--install` scrive `~/.local/share/applications/fenestra.desktop` e chiama
`xdg-mime default`. Richiede `xdg-utils`, che quasi tutti i desktop hanno già.

Compila dai sorgenti:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
```

## macOS

La **CLI** è disponibile come binario precompilato tramite Homebrew:

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` è una conferma una tantum che Homebrew richiede per i tap di terze parti.

L'**app per il Finder** (`Fenestra.app`) non ha un binario precompilato, e non per caso:
un bundle non firmato viene bloccato da Gatekeeper, e la notarizzazione costa 99 US$/anno.
[`macos/`](macos) è il kit di build: installa Go (installer ufficiale firmato, nessun
avviso), poi fai `git clone` ed esegui un solo comando. Clonare con git non aggiunge
l'attributo di quarantena, quindi macOS non chiede mai il permesso. Senza git, scarica
lo ZIP ed esegui `bash build.command`. Vedi [macos/README.it.md](macos/README.it.md).

Il kit viene compilato e collaudato su un vero runner `macos-latest` in CI: build,
`codesign --verify`, il droplet che riceve un file tramite `open -a` e il
server che risponde via HTTP. Solo l'interfaccia del Finder in sé non è testata (i
runner sono headless).

## Quando funziona

| Caso | Funziona? | Perché |
|---|---|---|
| Build statica (`dist`, `build`, `out`) | **Sì** | semplice HTML/CSS/JS |
| SPA con rotte lato client | **Sì** | il server ripiega su `index.html` per i percorsi senza estensione |
| HTML semplice in un unico file | **Sì** | aperto via `file://`, nessun server |
| Progetto sorgente (`/src/main.tsx`) | **No** | serve il dev server del progetto per compilare |
| App con backend/API | **No** | Fenestra serve file; non fa proxy né esegue servizi |
| Entry in una sottocartella che fa riferimento a `../` | **No** | la radice è la cartella del file; `..` è confinato per sicurezza |

Se il rilevamento sbaglia, `--serve` forza l'origine HTTP.

## Come funziona

Un solo file C#, nessuna dipendenza, compilato con il `csc.exe` fornito con Windows.

- **Server HTTP/1.1** su un `TcpListener`, in ascolto solo su `127.0.0.1`.
- **Risoluzione dei percorsi**: normalizza, rifiuta `..`, `:` e `::$DATA`, confina alla
  radice servita.
- **Assegnazione della porta**: hash FNV-1a della cartella → porta in `51000-63000`, salvata
  in un file di lock per cartella e riutilizzata all'avvio successivo.
- **Fallback SPA**: i 404 senza estensione restituiscono `index.html`.
- **Istanza singola**: un secondo doppio clic contatta il server in esecuzione e apre
  solo un'altra scheda del browser.
- **Durata**: il server termina quando termina il processo del browser che ha avviato, oppure
  dopo 4 ore di inattività se il browser era già in esecuzione.

## Sicurezza

Fenestra è uno strumento locale: solo loopback, niente TLS, niente autenticazione, niente
telemetria. Serve ogni file presente nella cartella aperta — non puntarlo su una
cartella con dei segreti. Vedi [SECURITY.it.md](SECURITY.it.md) per il modello di minaccia completo e la [pagina privacy](https://benedetleandro.github.io/fenestra/privacy.html) per sapere cosa memorizzano questo sito e l'app.

## Roadmap

- [ ] `--root <folder>` per le build la cui entry vive in una sottocartella
- [x] Linux: build nativa in Go in [`linux/`](linux), amd64 e arm64
- [x] macOS: CLI Homebrew e kit di build per il Finder in [`macos/`](macos)
- [ ] Firma del codice per le release, per eliminare l'avviso di SmartScreen
- [ ] Modalità portatile (senza `%LOCALAPPDATA%`)
- [ ] Più test: HEAD, ETag, richieste range, percorsi UTF-8
- [ ] Una registrazione demo di 15 secondi

## Privacy

Il sito web e l'app non usano cookie, analisi, pubblicità né telemetria.
Le uniche cose salvate in locale sono la lingua che scegli e l'avviso sulla privacy che
chiudi. Dettagli: [pagina privacy](https://benedetleandro.github.io/fenestra/privacy.html).

## Contribuire

Strumento piccolo, superficie piccola. Leggi [CONTRIBUTING.it.md](CONTRIBUTING.it.md); la
lista `good first issues` è lì.

## Licenza

[MIT](LICENSE) © Fenestra contributors
