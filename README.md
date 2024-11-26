# Hive 5 - README

## Introduktion
Hive 5 er et softwareprojekt designet til distribueret videoproduktion og streaming. Denne README guider dig igennem de nødvendige trin for at opsætte og køre projektet lokalt.

---

## Forudsætninger
Før du starter, skal følgende være opfyldt:

1. **.NET 9:** Installer den nyeste version af .NET SDK ([Download her](https://dotnet.microsoft.com/download)).
2. **NuGet-pakker:** Alle nødvendige pakker installeres automatisk, når projektet buildes i Visual Studio.

---

## Opsætning og Kørsel

### Generelt
Projektet kræver, at både en **Engine** og en **StreamHub** kører for at fungere korrekt. Der er allerede konfigureret launch-profiler for begge komponenter.

#### Start projektet
1. Åbn løsningen (`.sln`) i Visual Studio.
2. Vælg den ønskede launch-profil (se detaljer nedenfor).
3. Start følgende instanser:
        - To **Engine**-instanser.
        - To **StreamHub**-instanser.

### Seeded data
Når StreamHub kører, seedes der automatisk data i systemet. Dette bør resultere i, at to "workers" pr. engine vises i systemet, når både en Engine og en StreamHub er startet korrekt.

---

## Sådan bruges systemet

### Adgang til brugergrænsefladen
StreamHub leverer brugergrænsefladen. Afhængigt af hvilken instans du starter, skal du åbne følgende URL i din browser:

- [http://localhost:8999/](http://localhost:8999/)
- [http://localhost:9000/](http://localhost:9000/)

> **Bemærk:** Engine-komponenten har kun minimal brugergrænseflade.

---

## Demonstration af mange-til-mange-forbindelser
Når alle fire instanser er startet (to Engines og to StreamHubs), vil systemet demonstrere Hive 5's mange-til-mange-forbindelser. Dette betyder, at flere Engine-instanser kan kommunikere med flere StreamHub-instanser samtidigt, hvilket illustrerer systemets fleksibilitet og skalerbarhed.

---

## Netværkskonfiguration
Når du starter projektet, kan der komme en popup med en anmodning om at åbne porte i firewallen. Projektet bruger følgende porte:

- **8999, 9000**: StreamHub-instanser.
- **9001, 9002**: Engine-instanser.

Sørg for, at disse porte er åbne for at sikre korrekt kommunikation mellem komponenterne.

---

## Datamappe
Projektet opretter en datamappe i samme mappe som projektet. Hvis denne proces fejler, skal en absolut sti angives i `launchSettings.json`. Dette skal gøres for både **Engine** og **StreamHub**.

Eksempel på konfiguration i `launchSettings.json`:
```json
"HIVE_BASE_PATH": "c:\\temp\\StreamhubData8999"
