# Contribuer

[English](CONTRIBUTING.md) · [Português](CONTRIBUTING.pt-BR.md) · [Español](CONTRIBUTING.es.md) · [Français](CONTRIBUTING.fr.md) · [Deutsch](CONTRIBUTING.de.md) · [Italiano](CONTRIBUTING.it.md) · [Nederlands](CONTRIBUTING.nl.md)

<!-- translation of CONTRIBUTING.md @ 0.7.0 -->


Merci de votre intérêt. Cet outil est volontairement petit — gardons-le ainsi.

## Compilation

Prérequis : Windows 10/11 et le .NET Framework 4.x fourni avec. Aucun SDK, aucun Visual Studio, aucune connexion Internet.

```powershell
./build.ps1
```

Le binaire se trouve dans `bin/Fenestra.exe`.

## Tests

```powershell
./tests/run-tests.ps1
```

Le script ne compile rien ; exécutez d’abord `build.ps1`. Il démarre le serveur en mode sans interface (`FENESTRA_NO_OPEN=1`), envoie de véritables requêtes HTTP à une fixture temporaire et indique la réussite ou l’échec avec un code de sortie non nul.

## Style de code

- **C# 5 uniquement.** Le compilateur est celui de .NET Framework 4.8 : pas d’interpolation, pas de `?.`, pas de membres à corps d’expression.
- **Aucune dépendance.** Uniquement la BCL. Si une modification nécessite NuGet, elle doit d’abord faire l’objet d’une discussion.
- **Un seul fichier pour l’instant.** `src/Fenestra.cs` est volontairement un fichier unique ; sa division figure dans la feuille de route, mais n’est pas exigée pour les PR.
- Les commentaires expliquent le *pourquoi*, pas le *quoi*.

## Demandes de fusion (pull requests)

- Une seule modification par PR.
- Ajoutez un test si vous modifiez la gestion HTTP, la résolution des chemins ou la logique des ports.
- Mettez à jour `README.md` si le comportement change.
- Gardez un diff lisible ; évitez les reformatages opportunistes.

## Bonnes premières contributions

- `--root <folder>` pour servir un dossier différent de celui du fichier.
- Prise en charge de Windows ARM64.
- Signature du binaire publié (supprime l’avertissement SmartScreen).
- Mode portable (`fenestra.ini` à côté de l’exécutable, sans `%LOCALAPPDATA%`).
- Davantage de tests : HEAD, ETag, requêtes par plage, chemins UTF-8.
