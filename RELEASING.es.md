# Cómo publicar versiones

[English](RELEASING.md) · [Português](RELEASING.pt-BR.md) · [Español](RELEASING.es.md) · [Français](RELEASING.fr.md) · [Deutsch](RELEASING.de.md) · [Italiano](RELEASING.it.md) · [Nederlands](RELEASING.nl.md)

<!-- translation of RELEASING.md @ 0.7.0 -->


Fenestra está en alpha. Las versiones son **hitos**, no cada commit.

## Política

- `main` siempre compila. Los cambios se acumulan bajo **Unreleased** en
  [`CHANGELOG.md`](CHANGELOG.md).
- Una versión se publica solo cuando el mantenedor la pide, o al alcanzar un
  hito — nunca porque se haya fusionado una corrección.
- Una etiqueta por versión (`vX.Y.Z`). Nunca una etiqueta por cambio.
- La etiqueta debe coincidir con `VERSION`; la CI lo verifica.
- Las versiones de parche existen solo para arreglar una versión rota. Una
  corrección normal espera al siguiente hito.

## Pasos

1. Mueve las entradas de `Unreleased` a una sección de versión nueva en `CHANGELOG.md`.
2. Sube `VERSION` y las cadenas de versión del sitio (`docs/`); el job `version`
   de la CI falla si no coinciden.
3. Haz commit como `Release X.Y.Z`.
4. Crea la etiqueta y súbela: `git tag vX.Y.Z && git push origin vX.Y.Z`.
5. El flujo de trabajo de publicación compila y publica los artefactos.
6. Actualiza el tap de Homebrew con las URL nuevas y el SHA-256.
7. Cierra los issues y el hito.

## Versionado

`0.x` mientras esté en alpha. Los cambios incompatibles suben la versión menor.
Las correcciones van al siguiente hito, no a una etiqueta nueva. `1.0.0` está
reservada para la primera versión estable.
