ADR 05 Valg af SignalR til Realtidskommunikation
------------------------------------------------

**Placering**: /docs/adr/adr-05-system-architecture-design.md**\
Status:** Besluttet\
**Dato:** 30/9 2024\
**Beslutningstagere:** Tommi Iversen\
**Relaterede ADR'er:** Ingen

##### Kontekst

Hive 5 er designet som et distribueret videostreaming-system, der skal kunne håndtere realtidskommunikation mellem forskellige komponenter såsom **engines** og **streamhubs**. For at sikre effektiv, skalerbar og robust kommunikation er det nødvendigt at vælge et passende bibliotek eller framework, der understøtter disse krav.

##### Problemet

Hvordan kan Hive 5 implementere effektiv og pålidelig realtidskommunikation mellem engines og streamhubs for at opnå lav latenstid, høj tilgængelighed og nem integration med eksisterende teknologier som Blazor og .NET?

##### Overvejede Alternativer

1.  **SignalR**

○ **Beskrivelse:** Et open-source bibliotek fra Microsoft til at tilføje realtidsfunktionalitet til webapplikationer. Understøtter WebSockets og fallback-mekanismer.

○ **Fordele:**

■ Nemt at integrere med .NET og Blazor.

■ Understøtter forskellige transportmetoder (WebSockets, Server-Sent Events, Long Polling).

■ Aktivt vedligeholdt og godt dokumenteret.

■ Høj ydeevne og lav latenstid.

○ **Ulemper:**

■ Kan være overkill for meget simple kommunikationsbehov.

1.  **WebSockets**

○ **Beskrivelse:** En protokol, der giver fuld-dupleks kommunikation over en enkelt TCP-forbindelse.

○ **Fordele:**

■ Lav latenstid og høj effektivitet.

■ Understøtter realtidsdataudveksling.

○ **Ulemper:**

■ Mangler de højere niveau funktioner, som SignalR tilbyder (f.eks. gruppestyring, automatisk genforbindelse).

■ Kræver mere manuel håndtering af tilstand og forbindelser.

1.  **gRPC**

○ **Beskrivelse:** Et open-source RPC-framework fra Google, der bruger HTTP/2 til transport.

○ **Fordele:**

■ Høj ydeevne og lav latenstid.

■ Understøtter streaming og realtidskommunikation.

■ God integration med .NET.

○ **Ulemper:**

■ Mere kompleks opsætning sammenlignet med SignalR.

■ Mindre veletableret til webbaserede realtidsapplikationer sammenlignet med SignalR.

1.  **REST API'er med Polling**

○ **Beskrivelse:** Traditionelle HTTP-baserede API'er, hvor klienten regelmæssigt forespørger serveren for opdateringer.

○ **Fordele:**

■ Simpelt at implementere.

■ Bredt understøttet.

○ **Ulemper:**

■ Højere latenstid og ineffektiv for realtidskommunikation.

■ Øget belastning på serveren og netværket.

##### Beslutning

Vi vælger at anvende **SignalR** til realtidskommunikationen i Hive 5.

##### Begrundelse

-   **Integration med .NET og Blazor:** SignalR er designet til at arbejde problemfrit med .NET-økosystemet og Blazor, hvilket gør det nemt at integrere uden behov for betydelig ekstra konfiguration.
-   **Funktionalitet:** SignalR tilbyder højere niveau funktioner såsom gruppeadministration, automatisk genforbindelse og message broadcasting, hvilket reducerer behovet for manuel implementering af disse funktioner.
-   **Fleksibilitet:** Understøttelse af forskellige transportmetoder sikrer, at kommunikationen forbliver robust under forskellige netværksforhold.
-   **Ydeevne:** SignalR leverer lav latenstid og høj ydeevne, hvilket er afgørende for realtidsvideostreaming og overvågning.
-   **Community og Support:** Som et Microsoft-produkt har SignalR en stor brugerbase, omfattende dokumentation og aktiv support, hvilket gør det til et pålideligt valg for fremtidig vedligeholdelse og opdateringer.

##### Konsekvenser

-   **Afhængighed af Microsoft-teknologier:** Valget af SignalR binder Hive 5 tættere til Microsofts teknologistak, hvilket kan begrænse valgmulighederne for fremtidige teknologiske skift.
-   **Læringskurve:** Selvom SignalR er godt dokumenteret, kræver det en vis forståelse af de overordnede koncepter for realtidskommunikation og SignalR-specifikke funktioner.
-   **Deployment:** SignalR kræver korrekt konfiguration af servermiljøet for at understøtte WebSockets og andre transportmetoder, hvilket kan kræve yderligere opsætning og vedligeholdelse.

##### Alternativer Vurderet Men Ikke Valgt

-   **WebSockets:** Selvom WebSockets tilbyder lav latenstid, mangler det de højere niveau funktioner, som SignalR tilbyder. Implementering af disse funktioner manuelt ville kræve yderligere udviklingsindsats.
-   **gRPC:** På trods af sin høje ydeevne er gRPC mere komplekst at sætte op til webbaserede applikationer og tilbyder ikke samme niveau af integration med Blazor som SignalR.
-   **REST API'er med Polling:** Polling er ineffektivt for realtidskommunikation og ville introducere unødvendig belastning på serveren og netværket.
