ADR 02: Identifikation af Nøglekomponenter og Services
------------------------------------------------------

**Placering: /docs/adr/adr-02-identifikation-af-komponenter-og-services.md\
Status: Besluttet\
Dato: 4/10 2024\
Beslutningstager: Tommi Iversen**

##### Kontekst

Hive 5 er designet til at opfylde både funktionelle og ikke-funktionelle krav, der er kritiske for systemets funktionalitet og ydeevne i produktionsmiljøer. Disse krav understreger behovet for skalerbarhed (IFK 15), robusthed (IFK 16), lav latenstid (IFK 7) og modularitet (IFK 17). Arkitekturen er udformet til at understøtte fleksibilitet og brugervenlighed, som det er beskrevet i US 1 (tilføjelse af nye komponenter uden nedetid) og US 8 (fleksibel pipeline-konfiguration).

For at opfylde disse krav skal arkitekturen definere og implementere nøglekomponenter og services, der understøtter funktioner som styring af Workers, realtidskommunikation, overvågning, fejlhåndtering og brugervenligt interface.

##### Problemet

Hvordan identificerer og designer vi nøglekomponenter og services, der kan opfylde kravene om skalerbarhed, robusthed, lav latenstid og fleksibilitet, samtidig med at systemets modularitet og vedligeholdelse bevares?

##### Beslutning

Vi har besluttet at implementere følgende nøglekomponenter og services i Hive 5:

**På Engine-siden**

1.  **WorkerManager**: Centraliserer oprettelse og styring af Workers.
2.  **LoggerService**: Håndterer central logning af hændelser og fejl.
3.  **MetricsService**: Indsamler og sender performancedata i realtid.
4.  **WatchdogService**: Overvåger og genstarter Workers ved fejl.
5.  **Streamer**: Implementerer GStreamer til video encoding/decoding.
6.  **StateManagement**: Håndterer lokalt tilstandsdata og koordinering.
7.  **DB-SQLite**: Lokal lagring af data uden behov for central database.
8.  **StreamHubClient**: SignalR-komponent til kommunikation med StreamHub.

**På StreamHub-siden**

1.  **EngineHubServer**: SignalR-komponent til at håndtere forbindelser fra Engines.
2.  **EngineManager**: Centraliserer administration af tilsluttede Engines.
3.  **WorkerService**: Administrerer Workers på StreamHub-niveau.
4.  **BlazorServer**: Webserver til brugergrænsefladen og realtidsinteraktion.
5.  **RazorTemplates**: UI-komponenter til det webbaserede interface.

##### Begrundelse for Komponenter

1.  **StreamerService**

-   Kravopfyldelse: US 1 (dynamisk tilføjelse af Workers), US 3 (videostreaming med lav latenstid), IFK 7 (real-time ydeevne).
-   Funktion: Implementerer GStreamer og håndterer encoding/decoding for videostreams. Sikrer dynamisk opsætning og tilpasning til produktionsbehov.
-   Begrundelse: StreamerService gør det muligt at håndtere forskellige videostreams effektivt og med høj ydeevne.

3.  **MetricsService**

-   Kravopfyldelse: US 2 (realtidsovervågning af ressourcer), IFK 7 (lav latenstid), IFK 16 (robusthed).
-   Funktion: Indsamler og sender system- og performancedata (CPU, GPU, RAM, netværk) til StreamHub.
-   Begrundelse: MetricsService giver teknikere værktøjerne til at overvåge og reagere på ressourcebelastning i realtid.

5.  **WatchdogService**

-   Kravopfyldelse: US 5 (automatisk genstart af Workers), US 6 (håndtering af fejlmønstre).
-   Funktion: Overvåger Workers for fejl og uregelmæssigheder. Genstarter automatisk Workers ved fejl og håndterer gentagne fejl baseret på definerede mønstre.
-   Begrundelse: Automatiseret fejlhåndtering sikrer, at systemet forbliver stabilt, selv ved fejl.

7.  **WorkerManager**

-   Kravopfyldelse: US 1 (fleksibel typologi), IFK 15 (skalerbarhed), IFK 17 (modularitet).
-   Funktion: Centraliserer oprettelse og styring af Workers. Muliggør dynamisk tilføjelse og fjernelse uden nedetid.
-   Begrundelse: Sikrer, at systemet kan tilpasse sig ændringer i produktionsbehov uden at forstyrre driften.

9.  **LoggerService**

-   Kravopfyldelse: US 4 (logning af fejl og hændelser).
-   Funktion: Håndterer central logning af hændelser og fejl for hver Worker. Logfiler gemmes og kan tilgås via UI'et.
-   Begrundelse: Giver teknikere indsigt i systemets driftstilstand og mulighed for fejlfinding.

11. **BlazorServer** og SignalR-komponenter

-   Kravopfyldelse: US 7 (realtidsoverblik), US 8 (fleksibel konfiguration), IFK 6 (browserbaseret interface), IFK 7 (lav latenstid).
-   Funktion:

-   BlazorServer: Viser realtidsdata og styrer interaktionen mellem brugeren og systemet.
-   SignalR-komponenter: Sikrer effektiv kommunikation mellem UI og Engines via realtidsopdateringer.

-   Begrundelse: Intuitivt webinterface og robust realtidskommunikation er nødvendige for effektiv styring og overvågning.

##### Overvejede Alternativer

1.  Kombinere funktioner i færre komponenter

-   Begrundelse for afvisning: Dette ville mindske modulariteten og gøre systemet mere komplekst at vedligeholde. Separate komponenter muliggør uafhængig udvikling, test og vedligeholdelse.

3.  Anvende en central database til alle komponenter

-   Begrundelse for afvisning: En central database ville introducere et enkelt punkt af fejl (single point of failure) og kunne skabe flaskehalse. Lokale SQLite-databaser giver højere robusthed og ydeevne.

5.  Brug af alternative kommunikationsprotokoller

-   Begrundelse for afvisning: Protokoller som gRPC eller HTTP API'er opfylder ikke kravene om lav latenstid og robust realtidskommunikation på samme måde som SignalR.

##### Konsekvenser

Positive Konsekvenser:

-   Modularitet: Separate komponenter sikrer fleksibilitet og vedligeholdelsesvenlighed (IFK 17).
-   Skalerbarhed: Systemet kan nemt udvides ved at tilføje flere Engines og StreamHubs uden nedetid (IFK 15).
-   Robusthed: Fejl i én komponent påvirker ikke resten af systemet (IFK 16).

Negative Konsekvenser:

-   Øget kompleksitet: Flere komponenter kræver mere udviklings- og vedligeholdelsesarbejde.
-   Ressourcekrav: Flere komponenter kan kræve flere ressourcer til drift og vedligeholdelse.

##### Relaterede User Stories og Ikke-Funktionelle Krav

User Stories:

-   US 1: Tilføj og ændr systemtypologi uden nedetid.
-   US 2: Realtidsovervågning af ressourcer.
-   US 5: Automatisk genstart af Workers ved fejl.
-   US 6: Automatisk håndtering af fejlmønstre.
-   US 8: Fleksibel tilpasning af pipeline-konfigurationer.

Ikke-Funktionelle Krav:

-   IFK 6: Webinterface tilgængeligt fra enhver browser.
-   IFK 7: Lav latenstid under 1 ms på localhost.
-   IFK 15: Horisontal skalerbarhed.
-   IFK 16: Robust distribueret arkitektur.
-   IFK 17: Modulært design for fleksibilitet.
