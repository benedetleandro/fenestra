# Fenestra en macOS

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of macos/README.md @ 0.7.0 -->


Esta carpeta es el kit de compilación. Compilas en tu propia máquina — no hay
ningún binario precompilado para descargar, y es a propósito: una aplicación
compilada localmente nunca pasa por Gatekeeper.

## Requisito: Go 1.21+

El instalador oficial de <https://go.dev/dl/> está firmado y notarizado por Google,
así que **no aparece ninguna advertencia**. Es lo único que instalas.

## Opción A — sin git (Mac nuevo, sin Command Line Tools)

Un Mac recién instalado no tiene `git`: el comando lanza la instalación de las
Command Line Tools (~1,5 GB). Puedes saltártela:

1. Descarga el ZIP: <https://github.com/benedetleandro/fenestra/archive/refs/heads/main.zip>
2. Extráelo
3. En la Terminal:

```bash
cd ~/Downloads/fenestra-main/macos
bash build.command
```

`bash` viene con el sistema, y el Finder **lee** el script en lugar de
ejecutarlo — así que Gatekeeper no interviene. `fenestra` y `Fenestra.app` nacen
en local, sin cuarentena.

## Opción B — git clone (si ya tienes git)

```bash
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra/macos
./build.command
```

## Opción C — doble clic

Solo si ya tienes Go y quieres evitar la Terminal. macOS puede pedir permiso una
vez: **Ajustes del Sistema → Privacidad y Seguridad → Abrir de todos modos**.

## Uso

```bash
./fenestra ~/projects/app/dist/index.html   # opens in the default browser
./fenestra --browsers                       # what is installed
./fenestra --serve ~/index.html             # force the server
./fenestra --port 8080 ~/index.html         # pick the port
./fenestra --browser firefox                # switch browsers
./fenestra --browser safari                 # Safari is in the list too
./fenestra --install                        # Finder instructions
```

Para el Finder:

1. Arrastra `Fenestra.app` a `/Applications`.
2. Haz clic derecho en un `index.html` → **Abrir con** → **Fenestra**.
3. Marca **Usar siempre esta aplicación**.

Si el HTML hace referencia a `/src/` (una entrada de desarrollo), Fenestra usa en
su lugar la compilación hermana `dist/`, `build/` u `out/`, y explica qué hacer
cuando no hay ninguna.

## Homebrew (solo CLI)

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` es un requisito único para los taps de terceros. Esto instala solo la
línea de comandos; `Fenestra.app` sigue viniendo de `./build.command`.

## Cómo funciona la `.app`

`Fenestra.app` es un *droplet* de AppleScript (creado con `osacompile`, que viene
con macOS). Recibe los archivos que le pasa el Finder y llama al binario de Go que
hay dentro del paquete. Eso es lo que hace que «Abrir con» funcione sin una
aplicación Cocoa.

## Si algo falla

El script imprime `FAILED at line N`. Copia ese mensaje.

Mientras tanto, la vía manual muestra dónde está el problema:

```bash
cd fenestra/linux
go version
GOOS=darwin CGO_ENABLED=0 go build -o ../macos/fenestra .
../macos/fenestra ~/path/dist/index.html
```

Si `go build` funciona y `./fenestra` abre la página, el problema está solo en el
empaquetado de la `.app` — y el programa ya es usable.

Si `osacompile` falla (puede pedir las Command Line Tools), el script avisa y
conserva el binario. En ese caso el Finder no tiene asociación, pero la Terminal
funciona.

## Limitaciones honestas

- **Probado en CI, en un `macos-latest` real**: compilación, `codesign --verify`,
  el droplet recibiendo un archivo a través del Finder (`open -a`) y el servidor
  respondiendo por HTTP.
- **Sin probar en una sesión gráfica**: el clic en el Finder, el menú «Abrir con» y
  el diálogo de Gatekeeper solo existen en una pantalla — el runner funciona sin
  interfaz gráfica.
- La `.app` la genera `osacompile`; si tu macOS no tiene la herramienta, el script
  avisa y compila solo el binario.
- Cada persona compila la suya. Enviar la `.app` por AirDrop a otra máquina le
  añade cuarentena y Gatekeeper la vuelve a bloquear.
- Sin firma de desarrollador, sin notarización, sin US$ 99 al año.

## Deshacer

Borra `Fenestra.app` y `fenestra` de esta carpeta. Para el Finder, elige otra
aplicación en **Abrir con → Siempre**.
