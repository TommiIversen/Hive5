ADR 12: EngineHub i StreamHub
-----------------------------

**Placering:** /docs/adr/adr-12-enginehub.md\
**Status:** Besluttet\
**Dato:** 13/10 2024\
**Beslutningstagere:** Tommi Iversen

##### Kontekst

StreamHub kræver en central komponent til at håndtere forbindelser mellem Engines og frontend-klienter. EngineHub fungerer som en SignalR-hub, der modtager og distribuerer data som metrics, logs og events fra Engines samt videresender kommandoer til specifikke Engines. Denne komponent er afgørende for at sikre en centraliseret, realtidsbaseret kontrol over systemet (US07, IFK07).

##### Problemet

Hvordan kan EngineHub designes til effektivt at håndtere mange samtidige forbindelser, formidle data og kommandoer i realtid samt sikre skalerbarhed og robusthed?

##### Beslutning

EngineHub implementeres med følgende designvalg:

1.  **Forbindelsesstyring:**\
    EngineHub overvåger tilslutning og frakobling af Engines og sikrer, at kun aktive Engines vises i brugergrænsefladen. Den sender automatisk opdateringer om forbindelsestilstande til frontend-komponenterne.
2.  **Datahåndtering:**\
    EngineHub modtager metrics, logs og events fra Engines og distribuerer disse data selektivt til relevante frontend-klienter for at optimere netværksbelastning.
3.  **Kommandoformidling:**\
    Kommandoer fra brugere via frontend (fx "StartWorker", "StopWorker") videresendes til de relevante Engines gennem en struktureret og robust kommunikationspipeline.
4.  **Realtidsopdateringer:**\
    SignalR anvendes til at sikre realtidskommunikation, hvilket giver brugerne opdateret information og hurtig kommandoeksekvering.
5.  **Integration med EngineManager:**\
    EngineHub samarbejder med EngineManager og WorkerService for at administrere tilsluttede Engines og systemtilstand.

##### Begrundelse

-   **Centraliseret kontrol (US07):** EngineHub gør det muligt at administrere systemet gennem en enkelt brugergrænseflade, hvilket forenkler styringen af Engines og Workers.
-   **Realtidskommunikation (IFK07):** SignalR sikrer lav latenstid og gør det muligt at opdatere brugergrænsefladen og eksekvere kommandoer næsten øjeblikkeligt.
-   **Skalerbarhed (IFK15):** EngineHub er designet til at håndtere mange samtidige forbindelser og selektiv datadistribution, hvilket gør det muligt at skalere systemet uden væsentlige ændringer.

##### Tekniske Overvejelser

1.  **Effektiv Beskeddeling:**\
    EngineHub anvender SignalR's indbyggede grupper og forbindelsesstyring til at sende beskeder selektivt til bestemte klienter eller grupper, hvilket reducerer netværksbelastning.
2.  **Fejltolerance:**\
    EngineHub implementerer retry-mekanismer for at sikre, at data og kommandoer leveres pålideligt selv ved midlertidige netværksproblemer.
3.  **Skalerbarhed:**\
    Håndteringen af mange samtidige forbindelser muliggøres ved optimering af ressourcestyring og brug af SignalR's skaleringsfunktioner.

##### Konsekvenser

**Positive Konsekvenser:**

-   **Realtidsopdateringer:** Brugerne modtager opdateringer uden forsinkelse, hvilket sikrer en responsiv brugeroplevelse.
-   **Centraliseret styring:** Systemet kan administreres fra én central instans, hvilket forenkler brugerens interaktion.
-   **Målrettet kommunikation:** Selektiv beskeddeling reducerer netværksbelastning og sikrer, at kun relevante klienter modtager data.

**Negative Konsekvenser:**

-   **Kompleksitet:** Integration med flere komponenter som EngineManager og WorkerService øger kompleksiteten af systemets afhængigheder.
-   **Ressourcekrav:** Håndtering af mange samtidige forbindelser kan kræve mere avanceret ressourceallokering og opsætning af servere.

##### Relaterede User Stories og Ikke-Funktionelle Krav

**User Stories:**

-   **US07:** Realtidsoverblik for alle Workers og Engines.

**Ikke-Funktionelle Krav:**

-   **IFK07:** Lav latenstid under 1 ms på localhost.
-   **IFK15:** Horisontal skalerbarhed.
