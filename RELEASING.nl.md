# Een release uitbrengen

[English](RELEASING.md) · [Português](RELEASING.pt-BR.md) · [Español](RELEASING.es.md) · [Français](RELEASING.fr.md) · [Deutsch](RELEASING.de.md) · [Italiano](RELEASING.it.md) · [Nederlands](RELEASING.nl.md)

<!-- translation of RELEASING.md @ 0.7.0 -->


Fenestra is in de alfafase. Releases zijn **mijlpalen**, niet elke commit.

## Beleid

- `main` bouwt altijd. Wijzigingen worden verzameld onder **Unreleased** in
  [`CHANGELOG.md`](CHANGELOG.md).
- Een release gebeurt alleen wanneer de maintainer erom vraagt, of bij een
  mijlpaal — nooit omdat er een oplossing is samengevoegd.
- Eén tag per release (`vX.Y.Z`). Nooit een tag per wijziging.
- De tag moet overeenkomen met `VERSION`; CI dwingt dat af.
- Patchreleases bestaan alleen om een kapotte release te herstellen. Een normale
  oplossing wacht op de volgende mijlpaal.

## Stappen

1. Verplaats de `Unreleased`-regels naar een nieuwe versiesectie in
   `CHANGELOG.md`.
2. Verhoog `VERSION` en de versiestrings van de site (`docs/`); de CI-job
   `version` faalt als ze niet overeenkomen.
3. Commit als `Release X.Y.Z`.
4. Tag en push: `git tag vX.Y.Z && git push origin vX.Y.Z`.
5. De releaseworkflow bouwt en publiceert de assets.
6. Werk de Homebrew-tap bij met de nieuwe URL's en SHA-256.
7. Sluit de issues en de mijlpaal.

## Versiebeheer

`0.x` zolang de alfafase duurt. Breaking changes verhogen de minorversie. Oplossingen
gaan naar de volgende mijlpaal, niet naar een nieuwe tag. `1.0.0` is gereserveerd
voor de eerste stabiele release.
