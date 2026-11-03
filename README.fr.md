# Fenestra

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of README.md @ 0.7.0 -->

**Double-cliquez sur un fichier local `dist/index.html`. Il s’ouvre dans votre navigateur comme un site web — parce que Fenestra l’a servi.**

Aucun terminal. Aucun `npx serve`. Aucune extension à activer. Aucune fenêtre de navigateur personnalisée.

![compilation](https://github.com/benedetleandro/fenestra/actions/workflows/build.yml/badge.svg)
![licence](https://img.shields.io/badge/license-MIT-blue.svg)
![plateforme](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)

[**Site web →**](https://benedetleandro.github.io/fenestra/) · [Confidentialité](https://benedetleandro.github.io/fenestra/privacy.html)

> **État : alpha.** Le serveur HTTP et l’intégration au navigateur fonctionnent. L’association
> de fichiers nécessite une confirmation unique de l’utilisateur — Windows n’autorise
> pas une application à devenir gestionnaire par défaut de sa propre initiative.

## Le problème

Une compilation moderne (`Vite`, `React`, `Svelte`, tout ce qui utilise des modules ES) ne s’exécute pas depuis `file://`. Le navigateur refuse les scripts de modules pour des raisons de CORS, les chemins `/assets/...` pointent vers la racine du disque et les service workers ne sont pas disponibles. Il vous faut une origine HTTP.

D’ordinaire, cela suppose d’ouvrir un terminal et de lancer un serveur statique. Fenestra supprime cette étape : il *est* le serveur statique, rattaché au double-clic.

## Ce qu’il fait

Lorsque vous ouvrez un fichier `.html` :

1. Fenestra lit le fichier et détermine s’il a besoin d’une origine HTTP (modules ES, chemins absolus depuis la racine, `fetch`, service worker, manifeste web).
2. **S’il en a besoin**, il sert le dossier contenant le fichier sur `127.0.0.1`, sur un port stable, et ouvre un onglet normal de votre navigateur à cette URL.
3. **Sinon**, il ouvre directement le fichier via `file://`. Aucun serveur n’est démarré.

Le serveur conserve le même port pour chaque dossier, de sorte que `localStorage`, les cookies et le service worker persistent d’une session à l’autre.

## Installation

Prérequis : Windows 10/11. .NET Framework 4.x et Edge sont déjà présents — et tout navigateur basé sur Chromium ou Firefox fonctionne également. Sous Linux, il existe une version native distincte, voir [Linux](#linux).

```powershell
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Le binaire est `bin\Fenestra.exe`. Aucun SDK, aucun NuGet, aucune connexion Internet nécessaire pour compiler.

## Utilisation

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

### Faire de Fenestra l’application par défaut pour `.html` (une fois)

1. Exécutez `Fenestra.exe --install`.
2. Cliquez avec le bouton droit sur un fichier `index.html` → **Ouvrir avec** → **Choisir une autre application**.
3. Sélectionnez **Fenestra** et cochez **Toujours utiliser cette application**.

Windows n’autorise qu’un seul gestionnaire par extension ; cette confirmation ne peut donc pas être automatisée. Le gestionnaire est invisible ; la fenêtre qui s’ouvre est votre navigateur.

### Navigateurs

**Le navigateur du système est utilisé par défaut.** Chrome, Firefox, Brave, Vivaldi, Opera et Chromium sont détectés automatiquement, et `--browser edge` impose Edge. Sur macOS, **Safari** figure aussi dans la liste.

```powershell
Fenestra.exe --browsers              # what is installed
Fenestra.exe --browser chrome        # use Chrome from now on
Fenestra.exe --browser default       # let Windows decide
```

Le choix est enregistré dans `%LOCALAPPDATA%\Fenestra\browser.txt` et s’applique à chaque double-clic ; vous n’avez donc pas besoin de préciser à nouveau l’option.

### Sans interface (tests / CI)

Définissez `FENESTRA_NO_OPEN=1` pour que le serveur démarre sans ouvrir de navigateur. Le port est écrit dans `%LOCALAPPDATA%\Fenestra\sessions\<hash>.txt`.

## Linux

Une version native distincte se trouve dans [`linux/`](linux) : c’est un binaire statique unique, sans environnement d’exécution ni Wine. Il est écrit en Go et compilé de façon croisée pour `linux/amd64` et `linux/arm64`. Guide complet : [linux/README.fr.md](linux/README.fr.md).

```bash
chmod +x fenestra-linux-amd64
./fenestra-linux-amd64 ~/projects/app/dist/index.html
```

Même comportement que la version Windows : port stable par dossier, repli SPA, confinement des chemins et choix du navigateur.

```bash
./fenestra-linux-amd64 --browsers
./fenestra-linux-amd64 --browser firefox
./fenestra-linux-amd64 --install      # .desktop + xdg-mime
```

`--install` écrit `~/.local/share/applications/fenestra.desktop` et appelle `xdg-mime default`. Il nécessite `xdg-utils`, que la plupart des environnements de bureau fournissent déjà.

Compiler depuis les sources :

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
```

## macOS

La **CLI** est disponible sous forme de binaire précompilé via Homebrew :

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

`brew trust` est une confirmation unique exigée par Homebrew pour les dépôts tiers.

L’**application Finder** (`Fenestra.app`) n’a volontairement pas de binaire précompilé : un bundle non signé est bloqué par Gatekeeper, et la notarisation coûte 99 $ US par an. [`macos/`](macos) constitue le kit de compilation : installez Go (programme d’installation officiel signé, sans avertissement), puis clonez le dépôt avec `git clone` et lancez une commande. Un clonage avec git n’ajoute pas l’attribut de quarantaine ; macOS ne vous demande donc jamais d’autorisation. Sans git, téléchargez le ZIP et exécutez `bash build.command`. Voir [macos/README.fr.md](macos/README.fr.md).

Le kit est compilé et testé sur un véritable runner `macos-latest` dans la CI : compilation, `codesign --verify`, réception d’un fichier par le droplet via `open -a`, et réponse HTTP du serveur. Seule l’interface Finder elle-même n’est pas testée (les runners sont sans interface graphique).

## Quand cela fonctionne

| Cas | Fonctionne ? | Pourquoi |
|---|---|---|
| Compilation statique (`dist`, `build`, `out`) | **Oui** | HTML/CSS/JS simple |
| SPA avec routes côté client | **Oui** | le serveur se replie sur `index.html` pour les chemins sans extension |
| Fichier HTML unique simple | **Oui** | ouvert via `file://`, sans serveur |
| Projet source (`/src/main.tsx`) | **Non** | nécessite le serveur de développement du projet pour compiler |
| Application avec backend/API | **Non** | Fenestra sert des fichiers ; il n’agit pas comme proxy et ne lance aucun service |
| Entrée dans un sous-dossier référençant `../` | **Non** | la racine est le dossier du fichier ; `..` est confiné par sécurité |

Si la détection se trompe, `--serve` force l’origine HTTP.

## Fonctionnement

Un seul fichier C#, aucune dépendance, compilé avec le `csc.exe` fourni avec Windows.

- **Serveur HTTP/1.1** sur un `TcpListener`, lié uniquement à `127.0.0.1`.
- **Résolution des chemins** : normaliser, rejeter `..`, `:` et `::$DATA`, confiner à la racine servie.
- **Attribution des ports** : hachage FNV-1a du dossier → port dans `51000-63000`, stocké dans un fichier de verrouillage par dossier et réutilisé lors de l’exécution suivante.
- **Repli SPA** : les erreurs 404 sans extension renvoient `index.html`.
- **Instance unique** : un second double-clic interroge le serveur en cours et ouvre seulement un nouvel onglet de navigateur.
- **Durée de vie** : le serveur s’arrête quand le processus de navigateur qu’il a lancé se termine, ou après 4 heures d’inactivité si le navigateur était déjà en cours d’exécution.

## Sécurité

Fenestra est un outil local : boucle locale uniquement, pas de TLS, pas d’authentification, pas de télémétrie. Il sert tous les fichiers du dossier ouvert — ne le pointez pas vers un répertoire contenant des secrets. Consultez [SECURITY.fr.md](SECURITY.fr.md) pour le modèle de menace complet et la [page de confidentialité](https://benedetleandro.github.io/fenestra/privacy.html) pour savoir ce que ce site et l’application stockent.

## Feuille de route

- [ ] `--root <folder>` pour les compilations dont le point d’entrée se trouve dans un sous-dossier
- [x] Linux : compilation Go native dans [`linux/`](linux), amd64 et arm64
- [x] macOS : CLI Homebrew et kit de compilation Finder dans [`macos/`](macos)
- [ ] Signature de code des versions publiées, pour supprimer l’avertissement SmartScreen
- [ ] Mode portable (sans `%LOCALAPPDATA%`)
- [ ] Davantage de tests : HEAD, ETag, requêtes par plage, chemins UTF-8
- [ ] Une démonstration vidéo de 15 secondes

## Confidentialité

Le site web et l’application n’utilisent ni cookies, ni outils d’analyse, ni publicité, ni télémétrie. Les seules données stockées localement sont la langue que vous choisissez et l’avis de confidentialité que vous fermez. Détails : [page de confidentialité](https://benedetleandro.github.io/fenestra/privacy.html).

## Contribution

Petit outil, petite surface. Lisez [CONTRIBUTING.fr.md](CONTRIBUTING.fr.md) ; la liste `good first issues` s’y trouve.

## Licence

[MIT](LICENSE) © les contributeurs de Fenestra
