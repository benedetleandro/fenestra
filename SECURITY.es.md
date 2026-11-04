# Política de seguridad

[English](SECURITY.md) · [Português](SECURITY.pt-BR.md) · [Español](SECURITY.es.md) · [Français](SECURITY.fr.md) · [Deutsch](SECURITY.de.md) · [Italiano](SECURITY.it.md) · [Nederlands](SECURITY.nl.md)

<!-- translation of SECURITY.md @ 0.7.0 -->


## Versiones compatibles

Fenestra es un proyecto 1.1. Solo se da soporte al último commit de `main`.

## Cómo reportar una vulnerabilidad

Por favor, **no abras un issue público**. Usa el reporte privado de
vulnerabilidades de GitHub (pestaña "Security" → "Report a vulnerability") en este
repositorio.

Puedes esperar una respuesta en pocos días. Si el informe es válido, habrá una
corrección y una versión nueva, y se te acreditará, salvo que prefieras lo
contrario.

En caso de conflicto, prevalece la versión en inglés.

## Modelo de amenazas

Fenestra es una herramienta **local**. Ejecuta un servidor HTTP ligado a
`127.0.0.1` y sirve la carpeta del archivo que abriste. No está diseñada para
exponerse a una red y no implementa TLS, autenticación ni control de acceso.

Lo que está protegido:

- **Binding**: el listener se liga solo a `127.0.0.1`, nunca a `0.0.0.0`.
- **Path traversal**: las peticiones se normalizan y se confinan a la raíz servida.
  Se rechazan `..`, `:`, `::$DATA` y las rutas absolutas.
- **Sin código remoto**: el servidor solo lee y sirve archivos. No ejecuta nada de
  la carpeta servida.

Lo que **no** está protegido, por diseño:

- Cualquier cosa que pueda alcanzar el puerto puede leer todos los archivos que hay
  bajo la raíz servida, incluidos otros procesos locales. Apunta Fenestra a una
  carpeta, no a tu directorio personal.
- Los archivos y directorios ocultos (`.env`, `.git/`) devuelven 403 y se omiten de
  los listados; `.well-known/` es la excepción.
- No hay autenticación: un proceso local puede pedir cualquier archivo de la raíz.
- Las cabeceras `Referer`/`Origin` no se validan, así que una página maliciosa
  podría sondear el puerto local (el DNS rebinding se mitiga en parte al ligar solo
  a loopback, pero no del todo).

## Privacidad

El sitio web y la aplicación no usan cookies, analítica, publicidad ni telemetría.
Las únicas preferencias locales son el idioma elegido y el aviso de privacidad
descartado. Detalles: [página de privacidad](https://benedetleandro.github.io/fenestra/privacy.html).

## Alcance

Fuera del alcance: las vulnerabilidades que exigen que el atacante ya tenga
ejecución de código local, y los problemas causados por abrir una carpeta que
contiene secretos.
