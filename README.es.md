# Fenestra

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of README.md @ 0.7.0 -->

**Haz doble clic en un `dist/index.html` local. Se abre en tu navegador como un sitio web — porque Fenestra lo sirvió.**

Sin terminal. Sin `npx serve`. Sin extensión que activar. Sin ventana de navegador personalizada.

![build](https://github.com/benedetleandro/fenestra/actions/workflows/build.yml/badge.svg)
![license](https://img.shields.io/badge/license-MIT-blue.svg)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)

[**Sitio web →**](https://benedetleandro.github.io/fenestra/) · [Privacidad](https://benedetleandro.github.io/fenestra/privacy.html)

> **Estado: 1.1.** El servidor HTTP y la integración con el navegador funcionan. La
> asociación de archivos requiere una confirmación única del usuario — Windows no
> permite que una aplicación se convierta por sí sola en el controlador predeterminado.

## El problema

Una compilación moderna (`Vite`, `React`, `Svelte`, cualquier cosa con módulos ES)
no se ejecuta desde `file://`. El navegador rechaza los scripts de módulo por CORS,
`/assets/...` apunta a la raíz de la unidad y los service workers no están
disponibles. Necesitas un origen HTTP.

Normalmente eso significa abrir un terminal e iniciar un servidor estático.
Fenestra elimina ese paso: *es* el servidor estático, unido al doble clic.

## Qué hace

Cuando abres un archivo `.html`:

1. Fenestra lee el archivo y decide si necesita un origen HTTP
   (módulos ES, rutas absolutas a la raíz, `fetch`, service worker, manifiesto web).
2. **Si lo necesita**, sirve la carpeta que lo contiene en `127.0.0.1`, en un
   puerto estable, y abre una pestaña normal en tu navegador en esa URL.
3. **Si no lo necesita**, abre el archivo directamente mediante `file://`. No se
   inicia ningún servidor.

El servidor mantiene el mismo puerto por carpeta, así que `localStorage`, las
cookies y el service worker sobreviven entre sesiones.

## Instalación

Requisitos: Windows 10/11. .NET Framework 4.x y Edge ya vienen incluidos — y
cualquier navegador basado en Chromium o Firefox también funciona. En Linux hay
una compilación nativa aparte; consulta [Linux](#linux).

```powershell
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

El binario es `bin\Fenestra.exe`. Sin SDK, sin NuGet, sin internet para compilar.

## Uso

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

### Convertirlo en el predeterminado para `.html` (una sola vez)

1. Ejecuta `Fenestra.exe --install`.
2. Haz clic derecho en cualquier `index.html` → **Abrir con** → **Elegir otra aplicación**.
3. Selecciona **Fenestra** y marca **Usar siempre esta aplicación**.

Windows permite exactamente un controlador por extensión, así que esta
confirmación no se puede automatizar. El controlador es invisible; la ventana que
se abre es tu navegador.

### Navegadores

**El navegador del sistema es el predeterminado.** Chrome, Firefox, Brave,
Vivaldi, Opera y Chromium se detectan automáticamente, y `--browser edge` fija
Edge. En macOS, **Safari** también está en la lista.

```powershell
Fenestra.exe --browsers              # what is installed
Fenestra.exe --browser chrome        # use Chrome from now on
Fenestra.exe --browser default       # let Windows decide
```

La elección se guarda en `%LOCALAPPDATA%\Fenestra\browser.txt` y se aplica a cada
doble clic, así que no necesitas volver a pasar la opción.

### Modo headless (pruebas / CI)

Define `FENESTRA_NO_OPEN=1` y el servidor arranca sin abrir un navegador. El
puerto se escribe en `%LOCALAPPDATA%\Fenestra\sessions\<hash>.txt`.

## Linux

Existe una compilación nativa aparte en [`linux/`](linux): un único binario
estático, sin runtime y sin Wine. Escrita en Go y compilada de forma cruzada para
`linux/amd64` y `linux/arm64`. Guía completa: [linux/README.es.md](linux/README.es.md).

```bash
chmod +x fenestra-linux-amd64
./fenestra-linux-amd64 ~/projects/app/dist/index.html
```

El mismo comportamiento que la compilación para Windows: puerto estable por
carpeta, fallback de SPA, confinamiento de rutas y elección de navegador.

```bash
./fenestra-linux-amd64 --browsers
./fenestra-linux-amd64 --browser firefox
./fenestra-linux-amd64 --install      # .desktop + xdg-mime
```

`--install` escribe `~/.local/share/applications/fenestra.desktop` y llama a
`xdg-mime default`. Necesita `xdg-utils`, que la mayoría de los escritorios ya
traen.

Compilar desde el código fuente:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
```

## macOS

La **CLI** está disponible como binario precompilado a través de Homebrew:

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` es una confirmación única que Homebrew exige para los taps de
terceros.

La **aplicación del Finder** (`Fenestra.app`) no tiene binario precompilado a
propósito: un paquete sin firmar lo bloquea Gatekeeper, y la notarización cuesta
US$ 99 al año. [`macos/`](macos) es el kit de compilación: instala Go (instalador
oficial firmado, sin advertencias), luego haz `git clone` y ejecuta un comando.
Clonar con git no añade el atributo de cuarentena, así que macOS nunca pide
permiso. Sin git, descarga el ZIP y ejecuta `bash build.command`.
Consulta [macos/README.es.md](macos/README.es.md).

El kit se compila y se ejercita en un runner real `macos-latest` en CI:
compilación, `codesign --verify`, el droplet recibiendo un archivo mediante
`open -a` y el servidor respondiendo por HTTP. Solo la interfaz del Finder queda
sin probar (los runners no tienen interfaz gráfica).

## Cuándo funciona

| Caso | ¿Funciona? | Por qué |
|---|---|---|
| Compilación estática (`dist`, `build`, `out`) | **Sí** | HTML/CSS/JS puro |
| SPA con rutas en el cliente | **Sí** | el servidor recurre a `index.html` en las rutas sin extensión |
| HTML de un solo archivo | **Sí** | se abre mediante `file://`, sin servidor |
| Proyecto de código fuente (`/src/main.tsx`) | **No** | necesita el servidor de desarrollo del proyecto para compilar |
| Aplicación con backend/API | **No** | Fenestra sirve archivos; no hace de proxy ni ejecuta servicios |
| Entrada en una subcarpeta que referencia `../` | **No** | la raíz es la carpeta del archivo; `..` está confinado por seguridad |

Si la detección se equivoca, `--serve` fuerza el origen HTTP.

## Cómo funciona

Un solo archivo C#, sin dependencias, compilado con el `csc.exe` que viene con
Windows.

- **Servidor HTTP/1.1** sobre un `TcpListener`, ligado solo a `127.0.0.1`.
- **Resolución de rutas**: normaliza, rechaza `..`, `:` y `::$DATA` y confina a la
  raíz servida.
- **Asignación de puerto**: hash FNV-1a de la carpeta → puerto en `51000-63000`,
  guardado en un archivo de bloqueo por carpeta y reutilizado en la siguiente
  ejecución.
- **Fallback de SPA**: los 404 sin extensión devuelven `index.html`.
- **Instancia única**: un segundo doble clic consulta el servidor en ejecución y
  solo abre otra pestaña del navegador.
- **Ciclo de vida**: el servidor termina cuando termina el proceso del navegador
  que lanzó, o tras 4 horas de inactividad si el navegador ya estaba en ejecución.

## Seguridad

Fenestra es una herramienta local: solo loopback, sin TLS, sin autenticación y
sin telemetría. Sirve todos los archivos que hay bajo la carpeta abierta — no la
apuntes a un directorio con secretos. Consulta [SECURITY.es.md](SECURITY.es.md)
para ver el modelo de amenazas completo y la [página de privacidad](https://benedetleandro.github.io/fenestra/privacy.html) para saber qué guardan este sitio y la aplicación.

## Hoja de ruta

- [ ] `--root <carpeta>` para compilaciones cuya entrada está en una subcarpeta
- [x] Linux: compilación nativa en Go en [`linux/`](linux), amd64 y arm64
- [x] macOS: CLI por Homebrew y kit de compilación del Finder en [`macos/`](macos)
- [ ] Firma de código para las versiones, para quitar el aviso de SmartScreen
- [ ] Modo portátil (sin `%LOCALAPPDATA%`)
- [ ] Más pruebas: HEAD, ETag, range requests, rutas UTF-8
- [ ] Una grabación de demostración de 15 segundos

## Privacidad

El sitio web y la aplicación no usan cookies, analítica, publicidad ni telemetría.
El único almacenamiento local es el idioma que eliges y el aviso de privacidad que
descartas. Detalles: [página de privacidad](https://benedetleandro.github.io/fenestra/privacy.html).

## Contribuir

Herramienta pequeña, superficie pequeña. Lee [CONTRIBUTING.es.md](CONTRIBUTING.es.md);
allí está la lista de `good first issues`.

## Licencia

[MIT](LICENSE) © Colaboradores de Fenestra
