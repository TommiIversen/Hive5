ADR 06 Prioritering af CAP-egenskaber i Hive 5
----------------------------------------------

**Status:** Besluttet\
**Dato:** 22/09 2024\
**Beslutningstagere:** Tommi Iversen\
**Relaterede ADR'er:** Ingen

##### Kontekst

Hive 5 er et distribueret videostreaming-system designet til at håndtere realtidskommunikation og dataoverførsel mellem Engines og StreamHubs. Systemet skal kunne opretholde stabil drift og tilbyde opdateret information til brugere, selv under forbindelsesproblemer.

I henhold til **CAP-teoremet** (Gilbert & Lynch, 2002) kan et distribueret system kun opnå to af tre egenskaber:

-   **Konsistens (Consistency):** Alle noder ser de samme data på samme tid.
-   **Tilgængelighed (Availability):** Hver forespørgsel modtager et svar, selv hvis nogle komponenter fejler.
-   **Partitionstolerance (Partition Tolerance):** Systemet kan håndtere netværksproblemer, såsom pakketab og forbindelsestab.

Hive 5 prioriterer **tilgængelighed** og **partitionstolerance** for at sikre en robust og brugervenlig oplevelse i et distribueret miljø.

##### Problemet

Hvordan kan Hive 5 balancere kravene om realtidsydelse, dataintegritet og robusthed i et distribueret videostreaming-miljø, hvor forbindelsen mellem Engines og StreamHubs kan være ustabil, men hvor Workers skal fortsætte uden afbrydelse?

##### Overvejede Alternativer

**1\. Konsistens og Partitionstolerance (CP)**

**Fordele:**

-   Sikrer dataintegritet og konsistens på tværs af noder, selv under netværksproblemer.\
    **Ulemper:**
-   Lavere tilgængelighed, da forespørgsler kan blive afvist eller forsinket, hvis nogle komponenter ikke kan synkroniseres.
-   Uegnet til realtidskrav i videostreaming, hvor tilgængelighed er afgørende.

**2\. Tilgængelighed og Konsistens (CA)**

**Fordele:**

-   Data er altid konsistente og tilgængelige for brugerne.\
    **Ulemper:**
-   Kan ikke håndtere netværksproblemer, hvilket fører til systemnedbrud ved partitionering.

**3\. Tilgængelighed og Partitionstolerance (AP)**

**Fordele:**

-   Systemet forbliver tilgængeligt, selv under forbindelsesproblemer.
-   Egnet til realtidsapplikationer, hvor midlertidig inkonsistens er acceptabel.\
    **Ulemper:**
-   Data kan midlertidigt være inkonsistente mellem noder, indtil synkronisering er genoprettet.

##### Beslutning

Hive 5 prioriterer **Tilgængelighed (A)** og **Partitionstolerance (P)** i overensstemmelse med CAP-teoremet.

##### Begrundelse

1.  **Tilgængelighed:**

-   I tilfælde af forbindelsestab skal brugerne fortsat kunne se senest kendte data og interagere med systemet.
-   StreamHubs cacher de seneste data, så arbejdet kan fortsætte uden afbrydelser, hvilket opfylder kravene om høj tilgængelighed (IFK 18).

3.  **Partitionstolerance:**

-   Hive 5 er designet til at håndtere netværksproblemer som pakketab og forbindelsestab uden systemnedbrud.
-   Når forbindelsen genoprettes, synkroniserer systemet automatisk data ved hjælp af tidsstempler, hvilket sikrer dataintegritet (IFK 16).

5.  **Eventuel Konsistens:**

-   For at sikre tilgængelighed og partitionstolerance accepterer Hive 5 midlertidig inkonsistens mellem noder.
-   Denne tilgang er praktisk i videostreaming-miljøer, hvor forbindelsestab mellem Engine og StreamHub ikke påvirker Workers' funktionalitet.
-   Mekanismer som **event streaming**, **tidsstempler** og **caching** minimerer virkningen af inkonsistens, indtil systemet synkroniseres.

##### Konsekvenser

1.  **Fordele:**

-   Systemet kan opretholde drift og levere opdateret information, selv under forbindelsesproblemer.
-   Brugervenlig oplevelse, da brugerne ikke oplever nedetid eller manglende dataadgang.

3.  **Ulemper:**

-   Midlertidig inkonsistens mellem noder kan påvirke beslutninger, der kræver realtidskonsistens.
-   Øget kompleksitet i håndtering af eventuel konsistens og data-synkronisering.

5.  **Implikationer:**

-   Systemets design kræver robust eventhåndtering og caching for at sikre, at brugerne får adgang til de seneste data.
-   Yderligere test og overvågning er nødvendigt for at håndtere potentielle konflikter og undgå inkonsistens.

##### Relaterede Krav

-   **IFK 7:** Lav latenstid under 1 ms på localhost.
-   **IFK 16:** Robust distribueret systemarkitektur.
-   **IFK 18:** Fortsat adgang til data under forbindelsestab.
-   **US 9:** Skalerbart og robust distribueret system.

##### Alternativer Vurderet Men Ikke Valgt

1.  **Konsistens og Partitionstolerance (CP):**\
    For stor risiko for forsinkelse og tabt tilgængelighed i realtidsapplikationer.
2.  **Tilgængelighed og Konsistens (CA):**\
    Ikke egnet til netværksproblemer, hvilket gør det ubrugeligt i distribuerede miljøer.
