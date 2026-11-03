# Fenestra sous macOS

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of macos/README.md @ 0.7.0 -->


Ce dossier constitue le kit de compilation. Vous compilez sur votre machine — aucun binaire précompilé n’est à télécharger, et c’est volontaire : une application compilée localement ne passe jamais par Gatekeeper.

## Prérequis : Go 1.21+

Le programme d’installation officiel disponible sur <https://go.dev/dl/> est signé et notarisé par Google ; **aucun avertissement n’apparaît** donc. C’est le seul élément à installer.

## Méthode A — sans git (Mac neuf, sans Command Line Tools)

Un Mac fraîchement réinitialisé n’a pas `git` : la commande déclenche l’installation des Command Line Tools (~1,5 Go). Vous pouvez l’éviter :

1. Téléchargez l’archive ZIP : <https://github.com/benedetleandro/fenestra/archive/refs/heads/main.zip>
2. Décompressez-la
3. Dans le Terminal :

```bash
cd ~/Downloads/fenestra-main/macos
bash build.command
```

`bash` est fourni avec le système, et le script est **lu**, et non exécuté par le Finder — Gatekeeper n’intervient donc pas. `fenestra` et `Fenestra.app` sont créés localement, sans quarantaine.

## Méthode B — clonage git (si git est déjà installé)

```bash
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra/macos
./build.command
```

## Méthode C — double-clic

Uniquement si Go est déjà installé et que vous souhaitez éviter le Terminal. macOS peut demander une autorisation une fois : **Réglages Système → Confidentialité et sécurité → Ouvrir quand même**.

## Utilisation

```bash
./fenestra ~/projects/app/dist/index.html   # opens in the default browser
./fenestra --browsers                       # what is installed
./fenestra --serve ~/index.html             # force the server
./fenestra --port 8080 ~/index.html         # pick the port
./fenestra --browser firefox                # switch browsers
./fenestra --browser safari                 # Safari is in the list too
./fenestra --install                        # Finder instructions
```

Pour le Finder :

1. Faites glisser `Fenestra.app` vers `/Applications`.
2. Cliquez avec le bouton droit sur un fichier `index.html` → **Ouvrir avec** → **Fenestra**.
3. Cochez **Toujours utiliser cette application**.

Si le HTML référence `/src/` (un point d’entrée de développement), Fenestra utilise à la place la compilation présente dans `dist/`, `build/` ou `out/` au même niveau, et explique quoi faire lorsqu’il n’y en a aucune.

## Homebrew (CLI uniquement)

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` est une confirmation unique exigée pour les dépôts tiers. Cette installation ne fournit que la ligne de commande ; `Fenestra.app` est toujours produit par `./build.command`.

## Fonctionnement de l’application `.app`

`Fenestra.app` est un *droplet* AppleScript (construit avec `osacompile`, fourni avec macOS). Il reçoit les fichiers que le Finder lui transmet et appelle le binaire Go contenu dans le bundle. C’est ce qui permet à « Ouvrir avec » de fonctionner sans application Cocoa.

## En cas de problème

Le script affiche `FAILED at line N`. Copiez ce message.

En attendant, la procédure manuelle permet de localiser le problème :

```bash
cd fenestra/linux
go version
GOOS=darwin CGO_ENABLED=0 go build -o ../macos/fenestra .
../macos/fenestra ~/path/dist/index.html
```

Si `go build` fonctionne et que `./fenestra` ouvre la page, le problème concerne uniquement l’empaquetage de l’application `.app` — et le programme est déjà utilisable.

Si `osacompile` échoue (il peut demander les Command Line Tools), le script émet un avertissement et conserve le binaire. Dans ce cas, le Finder ne dispose d’aucune association, mais le Terminal fonctionne.

## Limites assumées

- **Testé dans la CI, sur un véritable runner `macos-latest`** : compilation, `codesign --verify`, réception d’un fichier par le droplet via le Finder (`open -a`) et réponse HTTP du serveur.
- **Non testé en session graphique** : le clic dans le Finder, le menu « Ouvrir avec » et la boîte de dialogue Gatekeeper n’existent que sur un écran — le runner est sans interface graphique.
- L’application `.app` est générée par `osacompile` ; si votre installation de macOS ne dispose pas de cet outil, le script émet un avertissement et ne compile que le binaire.
- Chacun compile sa propre version. Envoyer l’application `.app` à une autre machine via AirDrop ajoute la quarantaine, et Gatekeeper la bloque de nouveau.
- Ni signature de développeur, ni notarisation, ni frais de 99 $ US par an.

## Annulation

Supprimez `Fenestra.app` et `fenestra` de ce dossier. Pour le Finder, choisissez une autre application dans **Ouvrir avec → Toujours**.
