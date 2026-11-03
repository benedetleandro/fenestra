# Veröffentlichungen

[English](RELEASING.md) · [Português](RELEASING.pt-BR.md) · [Español](RELEASING.es.md) · [Français](RELEASING.fr.md) · [Deutsch](RELEASING.de.md) · [Italiano](RELEASING.it.md) · [Nederlands](RELEASING.nl.md)

<!-- translation of RELEASING.md @ 0.7.0 -->


Fenestra ist im Alpha-Stadium. Releases sind **Meilensteine**, nicht jeder Commit.

## Richtlinie

- `main` baut immer. Änderungen sammeln sich unter **Unreleased** in
  [`CHANGELOG.md`](CHANGELOG.md).
- Ein Release gibt es nur, wenn der Maintainer eines anfordert oder ein Meilenstein
  erreicht ist – niemals, weil eine Korrektur gemergt wurde.
- Ein Tag pro Release (`vX.Y.Z`). Niemals ein Tag pro Änderung.
- Der Tag muss zu `VERSION` passen; die CI erzwingt das.
- Patch-Releases gibt es nur, um ein fehlerhaftes Release zu reparieren. Eine
  normale Korrektur wartet auf den nächsten Meilenstein.

## Schritte

1. Verschieben Sie die Einträge aus `Unreleased` in einen neuen Versionsabschnitt in
   `CHANGELOG.md`.
2. Erhöhen Sie `VERSION` und die Versionsangaben der Website (`docs/`); der CI-Job
   `version` schlägt fehl, wenn sie nicht übereinstimmen.
3. Committen Sie als `Release X.Y.Z`.
4. Tag setzen und pushen: `git tag vX.Y.Z && git push origin vX.Y.Z`.
5. Der Release-Workflow baut und veröffentlicht die Assets.
6. Aktualisieren Sie den Homebrew-Tap mit den neuen URLs und SHA-256.
7. Schließen Sie die Issues und den Meilenstein.

## Versionierung

`0.x` im Alpha-Stadium. Breaking Changes erhöhen die Minor-Version. Korrekturen
fließen in den nächsten Meilenstein, nicht in ein neues Tag. `1.0.0` ist dem ersten
stabilen Release vorbehalten.
