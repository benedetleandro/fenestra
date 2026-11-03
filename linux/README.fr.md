# Fenestra sous Linux

[English](README.md) · [Português](README.pt-BR.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md)

<!-- translation of linux/README.md @ 0.7.0 -->


Fenestra pour Linux est un binaire Go statique unique. Aucun environnement d’exécution, aucune interface graphique, aucun privilège root.

## Installation

Téléchargez le binaire correspondant à votre architecture depuis la [dernière version](https://github.com/benedetleandro/fenestra/releases/latest) :

- `fenestra-linux-amd64`
- `fenestra-linux-arm64`

```bash
chmod +x fenestra-linux-amd64
mkdir -p ~/.local/bin
mv fenestra-linux-amd64 ~/.local/bin/fenestra
```

Assurez-vous que `~/.local/bin` figure dans votre `PATH`.

## Utilisation

```bash
fenestra ~/projects/app/dist/index.html   # open a build
fenestra ~/projects/app/dist              # open a folder
fenestra --serve ~/index.html             # force the server
fenestra --port 8080 ~/index.html         # pick the port
fenestra --browser firefox ~/app/index.html
fenestra --browsers                       # what is installed
```

## Renforcement de la sécurité

- Seuls les en-têtes `Host` de la boucle locale sont acceptés (`127.0.0.1`, `localhost`), ce qui bloque le DNS rebinding.
- Les liens symboliques qui pointent en dehors de la racine servie sont rejetés.
- Les réponses comportent `ETag` et `Last-Modified`, se revalident avec `304` et prennent en charge `Range` pour les médias volumineux.

## Points d’entrée source

Si le HTML référence `/src/` (un point d’entrée de développement Vite ou équivalent), Fenestra ne le sert pas : il cherche `dist/`, `build/` ou `out/` à côté et sert cette compilation à la place. En l’absence de compilation, il s’arrête avec un message vous invitant à lancer d’abord la compilation.

## Enregistrer le gestionnaire `.html`

```bash
fenestra --install
```

Cette commande écrit `~/.local/share/applications/fenestra.desktop`, actualise la base de données des applications de bureau et appelle :

```bash
xdg-mime default fenestra.desktop text/html
```

Pour annuler :

```bash
fenestra --uninstall
```

Si `xdg-mime` est absent, installez `xdg-utils` ou exécutez la commande ci-dessus manuellement.

## Navigateurs

La détection parcourt le `PATH`, dans cet ordre : `microsoft-edge`, `google-chrome`, `chromium`, `firefox`, `brave-browser`, `vivaldi`, `opera`. La préférence est enregistrée dans `~/.config/fenestra/browser` ; `--browser default` délègue à `xdg-open`.

## Emplacement de l’état

| Chemin | Contenu |
|---|---|
| `~/.config/fenestra/browser` | navigateur préféré |
| `~/.local/share/fenestra/fenestra.log` | journal du serveur |
| `~/.local/share/fenestra/` | un fichier de verrouillage par dossier servi (port stable) |

`XDG_CONFIG_HOME` et `XDG_DATA_HOME` sont respectés.

## Compiler depuis les sources

Go 1.21+, sans cgo :

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
go test ./...
```

Compilation croisée pour ARM :

```bash
GOOS=linux GOARCH=arm64 CGO_ENABLED=0 go build -o fenestra-arm64 .
```

## Différences avec les autres versions

- Ni registre ni Finder : le fichier `.desktop` associé à `xdg-mime` constitue la méthode d’association.
- L’état réside dans les chemins XDG, et non dans `%LOCALAPPDATA%` ou `~/Library/Application Support`.
- Même serveur, mêmes options, même repli SPA que sous Windows et macOS.

## Dépannage

- **`could not find <browser> installed on this machine`** — exécutez `fenestra --browsers` pour voir ce qui a été détecté, ou utilisez `--browser default`.
- **`xdg-mime failed`** — installez `xdg-utils`, ou définissez le gestionnaire manuellement.
- **Rien ne s’ouvre** — `--browser default` utilise `xdg-open` ; vérifiez que cette commande fonctionne seule.
- **Aucune association après `--install`** — certains environnements de bureau mettent en cache les gestionnaires MIME ; déconnectez-vous, ou exécutez `update-desktop-database ~/.local/share/applications`.

## Testé dans la CI

`ubuntu-latest` : `go test`, compilation et un véritable test HTTP de bon fonctionnement. ARM64 est compilé de façon croisée dans le même pipeline ; il n’est pas exécuté.
