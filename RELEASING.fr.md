# Publication des versions

[English](RELEASING.md) · [Português](RELEASING.pt-BR.md) · [Español](RELEASING.es.md) · [Français](RELEASING.fr.md) · [Deutsch](RELEASING.de.md) · [Italiano](RELEASING.it.md) · [Nederlands](RELEASING.nl.md)

<!-- translation of RELEASING.md @ 0.7.0 -->


Fenestra est en phase alpha. Les versions sont des **jalons**, pas chaque commit.

## Politique

- `main` compile toujours. Les modifications s’accumulent sous la rubrique **Unreleased** dans [`CHANGELOG.md`](CHANGELOG.md).
- Une version n’est publiée que lorsque le mainteneur le demande, ou à l’occasion d’un jalon — jamais parce qu’un correctif a été fusionné.
- Une étiquette (tag) par version (`vX.Y.Z`). Jamais une étiquette par modification.
- L’étiquette doit correspondre à `VERSION` ; la CI l’impose.
- Les versions correctives n’existent que pour réparer une version défectueuse. Un correctif normal attend le jalon suivant.

## Étapes

1. Déplacez les entrées de `Unreleased` vers une nouvelle section de version dans `CHANGELOG.md`.
2. Incrémentez `VERSION` et les chaînes de version du site (`docs/`) ; la tâche CI `version` échoue en cas de divergence.
3. Validez avec le message de commit `Release X.Y.Z`.
4. Créez l’étiquette et poussez-la : `git tag vX.Y.Z && git push origin vX.Y.Z`.
5. Le workflow de publication compile et publie les artefacts.
6. Mettez à jour le tap Homebrew avec les nouvelles URL et les empreintes SHA-256.
7. Fermez les tickets et le jalon.

## Numérotation des versions

`0.x` pendant la phase alpha. Les changements incompatibles incrémentent le numéro de version mineure. Les correctifs sont intégrés au jalon suivant, et non à une nouvelle étiquette. `1.0.0` est réservé à la première version stable.
