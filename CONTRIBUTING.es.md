# Cómo contribuir

[English](CONTRIBUTING.md) · [Português](CONTRIBUTING.pt-BR.md) · [Español](CONTRIBUTING.es.md) · [Français](CONTRIBUTING.fr.md) · [Deutsch](CONTRIBUTING.de.md) · [Italiano](CONTRIBUTING.it.md) · [Nederlands](CONTRIBUTING.nl.md)

<!-- translation of CONTRIBUTING.md @ 0.7.0 -->


Gracias por considerarlo. Esta es una herramienta pequeña a propósito — manténla
pequeña.

## Compilación

Requisitos: Windows 10/11 y el .NET Framework 4.x que viene incluido.
Sin SDK, sin Visual Studio, sin internet.

```powershell
./build.ps1
```

El binario queda en `bin/Fenestra.exe`.

## Pruebas

```powershell
./tests/run-tests.ps1
```

El script no compila nada; ejecuta `build.ps1` primero. Inicia el servidor en modo
headless (`FENESTRA_NO_OPEN=1`), hace peticiones HTTP reales contra un fixture
temporal e informa de aprobados y fallos con un código de salida distinto de cero.

## Estilo de código

- **Solo C# 5.** El compilador es el de .NET Framework 4.8, así que nada de
  interpolación, ni `?.`, ni miembros con cuerpo de expresión.
- **Cero dependencias.** Solo la BCL. Si un cambio necesita NuGet, primero hay que
  discutirlo.
- **Un solo archivo por ahora.** `src/Fenestra.cs` es un único archivo a propósito;
  dividirlo está en la hoja de ruta, pero no es un requisito para los PR.
- Los comentarios explican *por qué*, no *qué*.

## Pull requests

- Una sola cosa por PR.
- Incluye una prueba si tocas el manejo de HTTP, la resolución de rutas o la lógica
  de puertos.
- Actualiza `README.md` si cambia el comportamiento.
- Mantén el diff legible; nada de reformateos ajenos al cambio.

## Buenos primeros issues

- `--root <carpeta>` para servir una carpeta distinta de la del archivo.
- Compatibilidad con Windows ARM64.
- Firma de código para el binario de publicación (elimina el aviso de SmartScreen).
- Modo portátil (`fenestra.ini` junto al ejecutable, sin `%LOCALAPPDATA%`).
- Más pruebas: HEAD, ETag, range requests, rutas UTF-8.
