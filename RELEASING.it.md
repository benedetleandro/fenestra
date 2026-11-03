# Pubblicare una release

[English](RELEASING.md) · [Português](RELEASING.pt-BR.md) · [Español](RELEASING.es.md) · [Français](RELEASING.fr.md) · [Deutsch](RELEASING.de.md) · [Italiano](RELEASING.it.md) · [Nederlands](RELEASING.nl.md)

<!-- translation of RELEASING.md @ 0.7.0 -->


Fenestra è alpha. Le release sono **milestone**, non ogni commit.

## Politica

- `main` compila sempre. Le modifiche si accumulano sotto **Unreleased** in
  [`CHANGELOG.md`](CHANGELOG.md).
- Una release avviene solo quando il maintainer la chiede, oppure a una milestone —
  mai perché è stata unita una correzione.
- Un tag per release (`vX.Y.Z`). Mai un tag per modifica.
- Il tag deve corrispondere a `VERSION`; la CI lo impone.
- Le release patch esistono solo per correggere una release rotta. Una correzione normale
  aspetta la milestone successiva.

## Passaggi

1. Sposta le voci di `Unreleased` in una nuova sezione di versione in `CHANGELOG.md`.
2. Aggiorna `VERSION` e le stringhe di versione del sito (`docs/`); il job CI `version`
   fallisce se non coincidono.
3. Esegui il commit come `Release X.Y.Z`.
4. Crea il tag e fai il push: `git tag vX.Y.Z && git push origin vX.Y.Z`.
5. Il workflow di release compila e pubblica gli asset.
6. Aggiorna il tap Homebrew con i nuovi URL e lo SHA-256.
7. Chiudi le issue e la milestone.

## Versionamento

`0.x` finché è alpha. Le modifiche incompatibili incrementano la versione minor. Le correzioni
vanno nella milestone successiva, non in un nuovo tag. `1.0.0` è riservato alla prima release
stabile.
