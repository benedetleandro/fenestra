# Beveiligingsbeleid

[English](SECURITY.md) · [Português](SECURITY.pt-BR.md) · [Español](SECURITY.es.md) · [Français](SECURITY.fr.md) · [Deutsch](SECURITY.de.md) · [Italiano](SECURITY.it.md) · [Nederlands](SECURITY.nl.md)

<!-- translation of SECURITY.md @ 0.7.0 -->


## Ondersteunde versies

Fenestra is een 1.1-project. Alleen de laatste commit op `main` wordt ondersteund.

## Een kwetsbaarheid melden

Open alsjeblieft **geen publieke issue**. Gebruik de privémelding voor
kwetsbaarheden van GitHub (tabblad "Security" → "Report a vulnerability") in deze
repository.

Je kunt binnen een paar dagen een antwoord verwachten. Is de melding geldig, dan
volgen een oplossing en een release, en krijg je naamsvermelding, tenzij je dat
liever niet wilt.

Bij tegenstrijdigheden prevaleert de Engelse versie.

## Dreigingsmodel

Fenestra is een **lokaal** hulpmiddel. Het draait een HTTP-server gebonden aan
`127.0.0.1` en serveert de map van het bestand dat je hebt geopend. Het is niet
bedoeld om aan een netwerk te worden blootgesteld en implementeert geen TLS,
authenticatie of toegangscontrole.

Wat is beschermd:

- **Binding**: de listener bindt uitsluitend aan `127.0.0.1`, nooit aan `0.0.0.0`.
- **Path traversal**: verzoeken worden genormaliseerd en ingeperkt tot de
  geserveerde root. `..`, `:`, `::$DATA` en absolute paden worden geweigerd.
- **Geen code op afstand**: de server leest en serveert alleen bestanden. Hij
  voert niets uit de geserveerde map uit.

Wat **niet** is beschermd (bewust):

- Elk bestand onder de geserveerde root is leesbaar voor alles wat de poort kan
  bereiken, inclusief andere lokale processen. Wijs Fenestra naar een map, niet
  naar je thuismap.
- Verborgen bestanden en mappen (`.env`, `.git/`) geven 403 en worden weggelaten
  uit lijsten; `.well-known/` is de uitzondering.
- Er is geen authenticatie: een lokaal proces kan elk bestand in de root
  opvragen.
- De headers `Referer`/`Origin` worden niet gevalideerd, dus een kwaadwillende
  pagina zou de lokale poort kunnen aftasten (DNS rebinding wordt gedeeltelijk
  afgezwakt door alleen aan loopback te binden, maar niet volledig).

## Privacy

De website en de applicatie gebruiken geen cookies, analytics, advertenties of
telemetrie. De enige lokale voorkeuren zijn de gekozen taal en de weggeklikte
privacyverklaring. Details:
[privacypagina](https://benedetleandro.github.io/fenestra/privacy.html).

## Scope

Buiten scope: kwetsbaarheden waarbij de aanvaller al lokale code-uitvoering moet
hebben, en problemen die ontstaan door een map met geheimen te openen.
