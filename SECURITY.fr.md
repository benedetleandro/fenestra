# Politique de sécurité

[English](SECURITY.md) · [Português](SECURITY.pt-BR.md) · [Español](SECURITY.es.md) · [Français](SECURITY.fr.md) · [Deutsch](SECURITY.de.md) · [Italiano](SECURITY.it.md) · [Nederlands](SECURITY.nl.md)

<!-- translation of SECURITY.md @ 0.7.0 -->


## Versions prises en charge

Fenestra est un projet en phase alpha. Seul le dernier commit sur `main` est pris en charge.

## Signaler une vulnérabilité

Veuillez **ne pas ouvrir de ticket public**. Utilisez le signalement privé des vulnérabilités de GitHub (onglet « Sécurité » → « Signaler une vulnérabilité ») sur ce dépôt.

Vous recevrez une réponse sous quelques jours. Si le rapport est valide, un correctif et une version suivront, et vous serez mentionné dans les remerciements, sauf si vous préférez ne pas l’être.

En cas de conflit, la version anglaise prévaut.

## Modèle de menace

Fenestra est un outil **local**. Il exécute un serveur HTTP lié à `127.0.0.1` et sert le dossier du fichier que vous avez ouvert. Il n’est pas conçu pour être exposé à un réseau et ne met en œuvre ni TLS, ni authentification, ni contrôle d’accès.

Ce qui est protégé :

- **Liaison** : l’écouteur est lié uniquement à `127.0.0.1`, jamais à `0.0.0.0`.
- **Traversée de chemins** : les requêtes sont normalisées et confinées à la racine servie. `..`, `:`, `::$DATA` et les chemins absolus sont rejetés.
- **Aucun code distant** : le serveur ne fait que lire et servir des fichiers. Il n’exécute rien depuis le dossier servi.

Ce qui n’est **pas** protégé, par conception :

- Tous les fichiers de la racine servie sont lisibles par tout ce qui peut atteindre le port, y compris d’autres processus locaux. Pointez Fenestra vers un dossier, pas vers votre répertoire personnel.
- Les fichiers et dossiers cachés (`.env`, `.git/`) renvoient une erreur 403 et sont exclus des listes de répertoire ; `.well-known/` fait exception.
- Il n’y a aucune authentification : un processus local peut demander n’importe quel fichier de la racine.
- Les en-têtes `Referer`/`Origin` ne sont pas validés ; une page malveillante pourrait donc sonder le port local (le DNS rebinding est partiellement atténué par la liaison à la boucle locale uniquement, mais pas totalement).

## Confidentialité

Le site web et l’application n’utilisent ni cookies, ni outils d’analyse, ni publicité, ni télémétrie. Les seules préférences locales sont la langue choisie et la notification de confidentialité fermée. Détails : [page de confidentialité](https://benedetleandro.github.io/fenestra/privacy.html).

## Périmètre

Hors périmètre : les vulnérabilités qui nécessitent que l’attaquant dispose déjà d’une exécution de code locale, ainsi que les problèmes causés par l’ouverture d’un dossier contenant des secrets.
