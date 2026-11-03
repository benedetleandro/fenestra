# Fenestra su macOS

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of macos/README.md @ 0.7.0 -->


Questa cartella è il kit di build. Compili sulla tua macchina — non c'è un binario
precompilato da scaricare, e questo è voluto: un'app compilata in locale non passa mai
da Gatekeeper.

## Requisito: Go 1.21+

L'installer ufficiale su <https://go.dev/dl/> è firmato e notarizzato da Google,
quindi **non appare nessun avviso**. È l'unica cosa che devi installare.

## Percorso A — senza git (Mac nuovo, senza Command Line Tools)

Un Mac appena azzerato non ha `git`: il comando fa scattare l'installazione dei Command Line Tools
(~1,5 GB). Puoi evitarlo:

1. Scarica lo ZIP: <https://github.com/benedetleandro/fenestra/archive/refs/heads/main.zip>
2. Estrailo
3. Nel Terminale:

```bash
cd ~/Downloads/fenestra-main/macos
bash build.command
```

`bash` è incluso nel sistema, e lo script è **letto**, non eseguito dal
Finder — quindi Gatekeeper non c'entra. `fenestra` e `Fenestra.app` nascono
in locale, senza quarantena.

## Percorso B — git clone (se hai già git)

```bash
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra/macos
./build.command
```

## Percorso C — doppio clic

Solo se hai già Go e vuoi evitare il Terminale. macOS potrebbe chiederti il
permesso una volta: **Impostazioni di Sistema → Privacy e sicurezza → Apri comunque**.

## Utilizzo

```bash
./fenestra ~/projects/app/dist/index.html   # opens in the default browser
./fenestra --browsers                       # what is installed
./fenestra --serve ~/index.html             # force the server
./fenestra --port 8080 ~/index.html         # pick the port
./fenestra --browser firefox                # switch browsers
./fenestra --browser safari                 # Safari is in the list too
./fenestra --install                        # Finder instructions
```

Per il Finder:

1. Trascina `Fenestra.app` in `/Applications`.
2. Fai clic con il tasto destro su un `index.html` → **Apri con** → **Fenestra**.
3. Spunta **Usa sempre questa app**.

Se l'HTML fa riferimento a `/src/` (una entry di sviluppo), Fenestra usa invece la build
`dist/`, `build/` o `out/` accanto ad essa, e spiega cosa fare quando non ce n'è nessuna.

## Homebrew (solo CLI)

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` è un requisito una tantum per i tap di terze parti. Questo installa solo la
riga di comando; `Fenestra.app` arriva ancora da `./build.command`.

## Come funziona la `.app`

`Fenestra.app` è un *droplet* AppleScript (creato con `osacompile`, che è incluso
in macOS). Riceve i file che il Finder gli passa e chiama il binario Go
dentro il bundle. È questo che fa funzionare "Apri con" senza un'app Cocoa.

## Se qualcosa fallisce

Lo script stampa `FAILED at line N`. Copia quel messaggio.

Intanto, il percorso manuale mostra dov'è il problema:

```bash
cd fenestra/linux
go version
GOOS=darwin CGO_ENABLED=0 go build -o ../macos/fenestra .
../macos/fenestra ~/path/dist/index.html
```

Se `go build` funziona e `./fenestra` apre la pagina, il problema è solo nel
packaging della `.app` — e il programma è già utilizzabile.

Se `osacompile` fallisce (potrebbe chiedere i Command Line Tools), lo script avvisa
e mantiene il binario. In quel caso il Finder non ha alcuna associazione, ma il
Terminale funziona.

## Limiti reali

- **Testato in CI, su un vero `macos-latest`**: build, `codesign --verify`, il
  droplet che riceve un file tramite il Finder (`open -a`) e il server
  che risponde via HTTP.
- **Non testato in una sessione grafica**: il clic nel Finder, il menu "Apri con"
  e la finestra di Gatekeeper esistono solo su uno schermo — il runner è headless.
- La `.app` è generata da `osacompile`; se il tuo macOS non ha lo strumento, lo
  script avvisa e compila solo il binario.
- Ognuno si compila la propria. Inviare la `.app` via AirDrop a un'altra macchina
  aggiunge la quarantena e Gatekeeper la blocca di nuovo.
- Niente firma dello sviluppatore, niente notarizzazione, niente 99 US$ all'anno.

## Annullare

Elimina `Fenestra.app` e `fenestra` da questa cartella. Per il Finder, scegli
un'altra app in **Apri con → Sempre**.
