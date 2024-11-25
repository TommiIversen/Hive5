# ADR 01: Design af Systemarkitektur

- **Placering:** `/docs/adr/adr-01-system-architecture-design.md`  
- **Status:** Besluttet  
- **Dato:** 24/09 2024  
- **Beslutningstagere:** Tommi Iversen  
- **Relaterede ADR'er:** [ADR 02: Valg af SignalR til Realtidskommunikation]  

## Kontekst
Hive 5 er designet som et distribueret system til håndtering af videostreaming og realtidskommunikation mellem Engines og StreamHubs. Arkitekturen skal opfylde krav om lav latenstid, horisontal skalerbarhed og robusthed (fx IFK 7, IFK 15, IFK 16) og muliggøre dynamisk tilføjelse af komponenter uden nedetid (fx US 1). Designet skal sikre, at systemet er fleksibelt, modulært og nemt at vedligeholde.

## Problemet
Hvordan kan arkitekturen i Hive 5 designes, så den:
- Understøtter mange-til-mange-forbindelser mellem Engines og StreamHubs.
- Sikrer lav latenstid og høj ydeevne.
- Muliggør dynamisk tilføjelse og udvidelse af komponenter uden systemnedetid.
- Opfylder funktionelle og ikke-funktionelle krav som skalerbarhed, modularitet og robusthed.

## Overvejede Alternativer
### 1. Centraliseret Arkitektur
- **Beskrivelse:** Et centraliseret system, hvor al kommunikation og behandling går gennem én hovedkomponent.
- **Fordele:**
  - Simpel arkitektur og let at implementere.
  - Reducerer kompleksiteten i forbindelsesadministration.
- **Ulemper:**
  - Ikke skalerbart ved stigende antal Engines og StreamHubs.
  - Risiko for enkeltpunktfejl (single point of failure).

### 2. Modulær og Distribueret Arkitektur
- **Beskrivelse:** Et distribueret system med horisontal skalerbarhed, hvor hver komponent (Engine og StreamHub) opererer uafhængigt og kommunikerer via realtidsprotokoller.
- **Fordele:**
  - Understøtter skalerbarhed og fleksibilitet i tilføjelse af nye komponenter (fx US 1).
  - Høj robusthed, da fejl i én komponent ikke påvirker hele systemet (IFK 16).
  - Lav latenstid og effektiv performance ved mange samtidige forbindelser (IFK 7).
- **Ulemper:**
  - Mere kompleks implementering og vedligeholdelse.

### 3. Hybrid Arkitektur
- **Beskrivelse:** Kombinerer centraliserede og distribuerede elementer, hvor visse opgaver er centraliserede (fx logik), mens kommunikation sker distribueret.
- **Fordele:**
  - Balancerer kompleksitet og ydeevne.
  - Muliggør specialisering af visse komponenter.
- **Ulemper:**
  - Mindre skalerbar end fuldt distribuerede løsninger.

## Beslutning
Hive 5 implementerer en modulær og distribueret arkitektur for at opfylde kravene om skalerbarhed, fleksibilitet og robusthed.

## Begrundelse
- **Skalerbarhed (IFK 15):** Den modulære opbygning sikrer, at flere Engines og StreamHubs kan tilføjes uden betydelige ændringer i systemet.
- **Robusthed (IFK 16):** Fejl i en komponent påvirker ikke resten af systemet, hvilket sikrer høj tilgængelighed.
- **Lav Latenstid (IFK 7):** Arkitekturen understøtter realtidskommunikation mellem komponenterne med minimal forsinkelse, kritisk for applikationer som live videostreaming.
- **Modularitet (IFK 17):** Nye Engines eller StreamHubs kan tilføjes ad hoc uden nedetid eller genstart, hvilket understøtter fleksibiliteten i US 1.
- **Realtidsdata (US 2):** Arkitekturen muliggør overvågning af metrics i realtid, hvilket er afgørende for teknikernes arbejde.

## Konsekvenser
- **Kompleksitet:** Den distribuerede tilgang kræver mere avanceret logik og infrastruktur til at håndtere netværksforbindelser og synkronisering.
- **Deployment:** Skalering og vedligeholdelse kræver opsætning af miljøer, der understøtter horisontal skalerbarhed, fx containere og orkestrering.
- **Integration:** Arkitekturen kræver, at andre komponenter (fx SignalR) fungerer optimalt for at sikre stabil kommunikation.

## Alternativer Vurderet Men Ikke Valgt
- **Centraliseret Arkitektur:** Mangler robusthed og skalerbarhed. Risiko for enkeltpunktfejl gør den uegnet til Hive 5's krav.
- **Hybrid Arkitektur:** På trods af fordelene ved balance mellem centralisering og distribution blev denne løsning vurderet som mindre fleksibel og skalerbar end en fuldt distribueret arkitektur.

## Relaterede User Stories
- US 1: Tilføj og ændr systemtypologi uden nedetid.
- US 2: Realtidsovervågning af systemets ydeevne.
- US 9: Skalerbart og robust distribueret system.

## Relaterede Ikke-Funktionelle Krav
- IFK 7: Lav latenstid under 1 ms på localhost.
- IFK 15: Horisontal skalerbarhed.
- IFK 16: Robust distribueret arkitektur.
- IFK 17: Modulært design for fleksibilitet.
