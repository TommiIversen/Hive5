ADR 08: Design og Implementering af StreamHubClient
---------------------------------------------------

**Placering:** /docs/adr/adr-08-streamhubclient-eip-design.md\
**Status:** Besluttet\
**Dato:** 25/09 2024\
**Beslutningstager:** Tommi Iversen

##### Kontekst

StreamHubClient er en central komponent i Hive 5's Engine, som understøtter realtidskommunikation mellem Engine og StreamHub. For at sikre skalerbarhed, robusthed og fleksibilitet i kommunikationen anvendes veldefinerede **Enterprise Integration Patterns (EIP)** som grundlag for designet. StreamHubClient håndterer:

-   Dataudsendelse af metrics, logs og events til StreamHub.
-   Kommandomodtagelse og eksekvering.
-   Forbindelsesstyring og robusthed over for netværksforstyrrelser.
-   Afkobling af forbindelser mellem flere StreamHubs via køer.

##### Problemet

Hvordan kan StreamHubClient designes til effektivt at håndtere tovejs-kommunikation med flere StreamHubs, samtidig med at krav om modularitet, robusthed, lav latenstid og skalerbarhed opfyldes?

##### Beslutning

StreamHubClient designes baseret på følgende EIP-mønstre:

**Hoveddesign ved brug af EIP Patterns:**

1.  **Content-Based Router (CBR):**

-   **Funktion:** Dirigerer beskeder baseret på type (metrics, logs, events) til de korrekte handlers på StreamHub-siden.
-   **Begrundelse:** Sikrer, at forskellige beskeder behandles korrekt uden at påvirke andre beskedtyper.

3.  **Content Enricher:**

-   **Funktion:** Tilføjer metadata såsom tidsstempel, sekvensnummer og EngineID til alle udgående beskeder.
-   **Begrundelse:** Metadata giver modtageren mulighed for at afgøre beskedens relevans og rækkefølge, hvilket er essentielt for pålidelig realtidskommunikation.

5.  **Aggregator:**

-   **Funktion:** Samler kommandoer fra flere StreamHubs og videresender dem som en samlet enhed til WorkerManager for eksekvering.
-   **Begrundelse:** Reducerer kompleksitet og muliggør effektiv behandling af kommandoer.

7.  **Message Queue pr. StreamHub:**

-   **Funktion:** Hver StreamHub-forbindelse får en dedikeret kø for at sikre afkobling og isolering mellem forbindelser.
-   **Begrundelse:** Forhindrer, at midlertidige fejl i én forbindelse påvirker andre StreamHubs.

9.  **Retry-mekanisme:**

-   **Funktion:** Implementerer automatisk genforbindelse ved forbindelsestab.
-   **Begrundelse:** Øger robustheden og sikrer, at kommunikationen genoptages uden manuel indgriben.

##### Designvalg og Begrundelse

1.  **Modularitet og Skalerbarhed:**

-   Valget af EIP-mønstre sikrer en modulær arkitektur, hvor hver del af kommunikationen håndteres af specifikke mønstre. Dette muliggør nem tilføjelse af nye funktioner uden at påvirke eksisterende logik.
-   Købaseret afkobling muliggør horisontal skalering ved at tilføje flere StreamHubs uden risiko for flaskehalse.

3.  **Realtidskommunikation:**

-   Anvendelse af Content Enricher og Content-Based Router sikrer, at beskeder leveres hurtigt og korrekt til de rette modtagere, selv i miljøer med høj belastning.

5.  **Robusthed:**

-   Retry-mekanismen og Message Queues sikrer, at midlertidige forbindelsesproblemer ikke fører til tab af data eller destabilisering af systemet.

7.  **Dataintegritet:**

-   Metadata som sekvensnummer og tidsstempel giver modtagerne mulighed for at evaluere beskedernes relevans, hvilket er essentielt for stabil drift.

##### Interaktion med Andre Komponenter

-   **WorkerManager:**\
    Modtager aggregerede kommandoer fra Aggregator og udfører operationer på Workers.
-   **Content Enricher:**\
    Tilføjer nødvendige metadata til alle beskeder, hvilket muliggør korrekt behandling.
-   **Message Queue:**\
    Sikrer isolering og robusthed i kommunikationen mellem flere StreamHubs.
-   **Content-Based Router:**\
    Dirigerer beskeder til de korrekte handlers på StreamHub baseret på beskedens type.

##### Refleksion over Designet

Designet af StreamHubClient baseret på EIP-mønstre er valgt for at balancere kompleksitet og robusthed:

-   **Fordele:**\
    EIP-mønstre giver en veldokumenteret og struktureret tilgang til at håndtere komplekse integrationsscenarier. Modulariteten gør det nemt at tilføje nye funktioner eller tilpasse eksisterende logik.
-   **Udfordringer:**\
    Introduktion af EIP-mønstre kan øge den initiale udviklingskompleksitet, men dette opvejes af fordelene ved skalerbarhed og vedligeholdelsesvenlighed.

##### Konsekvenser

**Positive:**

-   **Modularitet:** Let vedligeholdelse og udvidelse af funktionalitet.
-   **Skalerbarhed:** Understøtter mange samtidige forbindelser uden performance degradation.
-   **Robusthed:** Systemet forbliver stabilt selv under netværksproblemer.
-   **Realtidsdata:** Hurtig og præcis levering af beskeder.

**Negative:**

-   Øget udviklingskompleksitet ved introduktion af flere EIP-mønstre.
-   Højere krav til systemressourcer ved håndtering af mange forbindelser og køer.

##### Relaterede User Stories og Ikke-Funktionelle Krav

-   **User Stories:**

-   US01: Tilføj og ændr system uden nedetid.
-   US02: Realtidsovervågning af CPU-, GPU-, netværks- og RAM-forbrug.
-   US09: Skalerbart og robust distribueret system.

-   **Ikke-Funktionelle Krav:**

-   IFK07: Lav latenstid under 1 ms på localhost.
-   IFK15: Horisontal skalerbarhed.
-   IFK16: Robust distribueret arkitektur.
