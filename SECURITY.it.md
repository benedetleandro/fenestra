# Politica di sicurezza

[English](SECURITY.md) · [Português](SECURITY.pt-BR.md) · [Español](SECURITY.es.md) · [Français](SECURITY.fr.md) · [Deutsch](SECURITY.de.md) · [Italiano](SECURITY.it.md) · [Nederlands](SECURITY.nl.md)

<!-- translation of SECURITY.md @ 0.7.0 -->


## Versioni supportate

Fenestra è un progetto 1.0. È supportato solo l'ultimo commit su `main`.

## Segnalare una vulnerabilità

Per favore **non aprire una issue pubblica**. Usa la segnalazione privata di
vulnerabilità di GitHub ("Security" → "Report a vulnerability") su questo repository.

Puoi aspettarti una risposta entro pochi giorni. Se la segnalazione è valida,
seguiranno una correzione e una release, e sarai citato come autore, a meno che tu
non preferisca il contrario.

In caso di conflitto, prevale la versione inglese.

## Modello di minaccia

Fenestra è uno strumento **locale**. Esegue un server HTTP in ascolto su `127.0.0.1` e
serve la cartella del file che hai aperto. Non è progettato per essere esposto a una
rete, e non implementa TLS, autenticazione o controllo degli accessi.

Cosa è protetto:

- **Binding**: il listener si lega solo a `127.0.0.1`, mai a `0.0.0.0`.
- **Path traversal**: le richieste vengono normalizzate e confinate alla radice servita.
  `..`, `:`, `::$DATA` e i percorsi assoluti vengono rifiutati.
- **Nessun codice remoto**: il server legge e serve solo file. Non
  esegue nulla dalla cartella servita.

Cosa **non** è protetto, per scelta progettuale:

- Ogni file sotto la radice servita è leggibile da qualsiasi cosa riesca a raggiungere la
  porta, compresi altri processi locali. Punta Fenestra su una cartella, non sulla
  tua cartella home.
- I file e le directory nascosti (`.env`, `.git/`) restituiscono 403 e sono omessi dagli
  elenchi; `.well-known/` è l'eccezione.
- Non c'è autenticazione: un processo locale può richiedere qualsiasi file nella radice.
- Gli header `Referer`/`Origin` non vengono validati, quindi una pagina malevola potrebbe
  sondare la porta locale (il DNS rebinding è parzialmente mitigato dal binding solo su
  loopback, ma non del tutto).

## Privacy

Il sito web e l'applicazione non usano cookie, analisi, pubblicità né
telemetria. Le uniche preferenze locali sono la lingua scelta e l'avviso sulla privacy
chiuso. Dettagli: [pagina privacy](https://benedetleandro.github.io/fenestra/privacy.html).

## Ambito

Fuori ambito: vulnerabilità che presuppongono che l'attaccante possa già eseguire codice
in locale, e problemi causati dall'aprire una cartella che contiene segreti.
