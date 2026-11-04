# Sicherheitsrichtlinie

[English](SECURITY.md) · [Português](SECURITY.pt-BR.md) · [Español](SECURITY.es.md) · [Français](SECURITY.fr.md) · [Deutsch](SECURITY.de.md) · [Italiano](SECURITY.it.md) · [Nederlands](SECURITY.nl.md)

<!-- translation of SECURITY.md @ 0.7.0 -->


## Unterstützte Versionen

Fenestra ist ein 1.0-Projekt. Nur der neueste Commit auf `main` wird unterstützt.

## Eine Schwachstelle melden

Bitte **öffnen Sie kein öffentliches Issue**. Nutzen Sie die private
Schwachstellenmeldung von GitHub (Tab "Security" → "Report a vulnerability") in
diesem Repository.

Sie erhalten innerhalb weniger Tage eine Antwort. Wenn der Bericht zutrifft, folgen
eine Korrektur und ein Release, und Sie werden genannt, sofern Sie nichts anderes
wünschen.

Bei Widersprüchen gilt die englische Fassung.

## Bedrohungsmodell

Fenestra ist ein **lokales** Werkzeug. Es betreibt einen HTTP-Server, der an
`127.0.0.1` gebunden ist, und liefert den Ordner der von Ihnen geöffneten Datei aus.
Es ist nicht dafür ausgelegt, einem Netzwerk ausgesetzt zu werden, und implementiert
weder TLS noch Authentifizierung oder Zugriffskontrolle.

Was geschützt ist:

- **Bindung**: Der Listener bindet ausschließlich an `127.0.0.1`, nie an `0.0.0.0`.
- **Path Traversal**: Anfragen werden normalisiert und auf die ausgelieferte Wurzel
  begrenzt. `..`, `:`, `::$DATA` und absolute Pfade werden abgelehnt.
- **Kein entfernter Code**: Der Server liest und liefert nur Dateien aus. Er führt
  nichts aus dem ausgelieferten Ordner aus.

Was bewusst **nicht** geschützt ist:

- Jede Datei unter der ausgelieferten Wurzel ist für alles lesbar, was den Port
  erreichen kann, einschließlich anderer lokaler Prozesse. Richten Sie Fenestra auf
  einen Ordner, nicht auf Ihr Home-Verzeichnis.
- Versteckte Dateien und Verzeichnisse (`.env`, `.git/`) liefern 403 und werden in
  Auflistungen weggelassen; Ausnahme ist `.well-known/`.
- Es gibt keine Authentifizierung: Ein lokaler Prozess kann jede Datei in der Wurzel
  anfordern.
- Die Header `Referer`/`Origin` werden nicht validiert, sodass eine bösartige Seite
  den lokalen Port abfragen könnte (DNS-Rebinding wird durch die Bindung an
  ausschließlich Loopback teilweise entschärft, aber nicht vollständig).

## Datenschutz

Die Website und die Anwendung verwenden keine Cookies, keine Analyse, keine Werbung
und keine Telemetrie. Die einzigen lokalen Einstellungen sind die gewählte Sprache und
der weggeklickte Datenschutzhinweis. Details:
[Datenschutzseite](https://benedetleandro.github.io/fenestra/privacy.html).

## Geltungsbereich

Nicht im Geltungsbereich: Schwachstellen, die voraussetzen, dass der Angreifer
bereits lokalen Code ausführen kann, sowie Probleme, die durch das Öffnen eines
Ordners mit Geheimnissen entstehen.
