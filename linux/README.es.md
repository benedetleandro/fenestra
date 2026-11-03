# Fenestra en Linux

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of linux/README.md @ 0.7.0 -->


Fenestra para Linux es un único binario estático de Go. Sin runtime, sin interfaz
gráfica y sin root.

## Instalación

Descarga el binario para tu arquitectura desde la
[última versión](https://github.com/benedetleandro/fenestra/releases/latest):

- `fenestra-linux-amd64`
- `fenestra-linux-arm64`

```bash
chmod +x fenestra-linux-amd64
mkdir -p ~/.local/bin
mv fenestra-linux-amd64 ~/.local/bin/fenestra
```

Asegúrate de que `~/.local/bin` esté en tu `PATH`.

## Uso

```bash
fenestra ~/projects/app/dist/index.html   # open a build
fenestra ~/projects/app/dist              # open a folder
fenestra --serve ~/index.html             # force the server
fenestra --port 8080 ~/index.html         # pick the port
fenestra --browser firefox ~/app/index.html
fenestra --browsers                       # what is installed
```

## Refuerzo de seguridad

- Solo se aceptan cabeceras `Host` de loopback (`127.0.0.1`, `localhost`), lo que
  bloquea el DNS rebinding.
- Se rechazan los enlaces simbólicos que resuelven fuera de la raíz servida.
- Las respuestas llevan `ETag` y `Last-Modified`, se revalidan con `304` y admiten
  `Range` para medios grandes.

## Entradas de código fuente

Si el HTML hace referencia a `/src/` (una entrada de desarrollo de Vite o similar),
Fenestra no la sirve: busca `dist/`, `build/` u `out/` junto a ella y sirve esa
compilación en su lugar. Cuando no hay compilación, se detiene con un mensaje que
te dice que ejecutes la compilación primero.

## Registrar el controlador de `.html`

```bash
fenestra --install
```

Esto escribe `~/.local/share/applications/fenestra.desktop`, actualiza la base de
datos del escritorio y llama a:

```bash
xdg-mime default fenestra.desktop text/html
```

Para deshacerlo:

```bash
fenestra --uninstall
```

Si no tienes `xdg-mime`, instala `xdg-utils` o ejecuta el comando de arriba a mano.

## Navegadores

La detección recorre el `PATH` en este orden: `microsoft-edge`, `google-chrome`,
`chromium`, `firefox`, `brave-browser`, `vivaldi`, `opera`. La preferencia se
guarda en `~/.config/fenestra/browser`; `--browser default` delega en `xdg-open`.

## Dónde guarda el estado

| Ruta | Qué |
|---|---|
| `~/.config/fenestra/browser` | navegador preferido |
| `~/.local/share/fenestra/fenestra.log` | registro del servidor |
| `~/.local/share/fenestra/` | un archivo de bloqueo por carpeta servida (puerto estable) |

Se respetan `XDG_CONFIG_HOME` y `XDG_DATA_HOME`.

## Compilar desde el código fuente

Go 1.21 o superior, sin cgo:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
go test ./...
```

Compilación cruzada para ARM:

```bash
GOOS=linux GOARCH=arm64 CGO_ENABLED=0 go build -o fenestra-arm64 .
```

## Diferencias con las otras compilaciones

- Sin registro y sin Finder: el archivo `.desktop` más `xdg-mime` es la vía de
  asociación.
- El estado vive en rutas XDG, no en `%LOCALAPPDATA%` ni en
  `~/Library/Application Support`.
- El mismo servidor, las mismas opciones y el mismo fallback de SPA que en Windows
  y macOS.

## Solución de problemas

- **`could not find <browser> installed on this machine`** — ejecuta
  `fenestra --browsers` para ver qué se detectó, o usa `--browser default`.
- **`xdg-mime failed`** — instala `xdg-utils` o configura el controlador a mano.
- **No se abre nada** — `--browser default` usa `xdg-open`; comprueba que funcione
  por sí solo.
- **Sin asociación después de `--install`** — algunos escritorios cachean los
  controladores MIME; cierra la sesión o ejecuta
  `update-desktop-database ~/.local/share/applications`.

## Probado en CI

`ubuntu-latest`: `go test`, compilación y una prueba de humo HTTP real. ARM64 se
compila de forma cruzada en el mismo pipeline; no se ejecuta.
