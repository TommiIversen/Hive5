ADR 09: WorkerManager - Design og Implementering i Engine
---------------------------------------------------------

**Bilag:** ADR 09 - WorkerManager Design\
**Placering:** /docs/adr/adr-09-workermanager-design.md\
**Status:** Besluttet\
**Dato:** 25/10 2024\
**Beslutningstager:** Tommi Iversen

##### Kontekst

WorkerManager er en kernekomponent i Engine, der styrer livscyklussen for Workers. Den administrerer oprettelse, initialisering, styring og overvågning af alle Workers i systemet og sikrer effektiv håndtering af ressourcer, fejltolerance og skalerbarhed. WorkerManager interagerer med flere nøglekomponenter, herunder WorkerService, IStreamerService og IStreamerWatchdogService, og modtager kommandoer fra StreamHub via SignalR.

##### Problemet

Hvordan kan WorkerManager designes til at:

-   Understøtte dynamisk oprettelse og administration af Workers?
-   Sikre stabilitet og robusthed ved fejl i Workers?
-   Muliggøre fleksibel opsætning af streamingteknologier uden genstart eller kodeændringer?
-   Opfylde krav om skalerbarhed, lav latenstid og robusthed?

##### Beslutning

WorkerManager designes som en central koordinator, der anvender følgende designmønstre og integrationer:

**Hovedfunktioner:**

1.  **Oprettelse og Initialisering:**

-   Opretter nye Workers baseret på konfigurationsdata fra StreamHub.
-   Initialiserer WorkerService og konfigurerer StreamerFactory til dynamisk indlæsning af streaming-implementeringer.

3.  **Styring og Kontrol:**

-   Understøtter operationer som StartWorker, StopWorker og EditWorker, som modtages via SignalR fra StreamHub.
-   Udfører batch-opgaver såsom opstart af flere Workers under systeminitialisering.

5.  **Overvågning og Fejlhåndtering:**

-   Integrerer med IStreamerWatchdogService for at overvåge Worker-tilstande og genstarte Workers ved fejl.
-   Sender statusopdateringer og hændelseslogs tilbage til StreamHub.

7.  **Fleksibilitet i StreamerOpsætning:**

-   Bruger StreamerFactory til at understøtte dynamisk opsætning og udskiftning af streamingteknologier, som GStreamer, uden behov for kodeændringer.
-   Muliggør konfigurationsændringer direkte fra brugergrænsefladen.

9.  **Skalerbarhed og Samtidighed:**

-   Understøtter dynamisk tilføjelse og fjernelse af Workers baseret på produktionsbehov.
-   Implementerer asynkron behandling og samtidighedsstyring for at sikre responsivitet under høj belastning.

##### Interaktion med Andre Komponenter

-   **WorkerService:**

-   Håndterer individuelle Worker-operationer, såsom start, stop og tilstandsændringer.
-   Opsætter EventHandlers for realtidsmonitorering af logs og billeder genereret af hver Worker.

-   **IStreamerService:**

-   Repræsenterer den specifikke streaming-implementering (f.eks. GStreamer), der udfører operationer som start, stop og streaming.

-   **IStreamerWatchdogService:**

-   Overvåger tilstanden af Workers og sikrer automatisk genstart ved fejl eller inaktivitet.

-   **StreamHub:**

-   Modtager kommandoer fra StreamHub via SignalR og sender statusopdateringer, WorkerEvents og fejlhændelser tilbage.

##### Designvalg og Begrundelse

1.  **Fejltolerance og Robusthed:**

-   Integration med IStreamerWatchdogService muliggør automatisk genstart ved fejl, hvilket sikrer høj stabilitet (US05, US06).

3.  **Fleksibilitet og Udvidelsesmuligheder:**

-   StreamerFactory muliggør dynamisk indlæsning og udskiftning af streamingteknologier, hvilket understøtter fleksibilitet i produktionen (US08).

5.  **Skalerbarhed:**

-   Ved at understøtte dynamisk oprettelse og fjernelse af Workers kan WorkerManager skalere med systemets behov uden genstart (IFK15).

7.  **Performance og Responsivitet:**

-   Asynkron behandling sikrer, at systemet forbliver responsivt, selv under høj belastning (IFK07).

9.  **Sikkerhed i Samtidig Adgang:**

-   WorkerManager anvender låsemekanismer og samtidighedskontrol for at sikre data-integritet i multitrådede miljøer.

##### Refleksion over Designet

Designet af WorkerManager prioriterer robusthed, fleksibilitet og skalerbarhed. Ved at kombinere dynamisk opsætning med fejltolerance og skalerbarhed adresserer komponenten både funktionelle og ikke-funktionelle krav. Udfordringerne ved øget kompleksitet i samtidighedsstyring opvejes af fordelene ved modularitet og stabilitet.

##### Konsekvenser

**Positive:**

-   **Modularitet:** Muliggør nem udvidelse af systemet med nye streamingteknologier.
-   **Skalerbarhed:** Tilpasning til ændrede produktionskrav uden systemnedetid.
-   **Robusthed:** Automatiseret genstart af Workers reducerer risikoen for driftsafbrydelser.

**Negative:**

-   **Øget kompleksitet:** Kræver avanceret samtidighedsstyring og fejlhåndtering.
-   **Ressourcekrav:** Håndtering af mange samtidige Workers kan øge systemressourceforbruget.

##### Relaterede User Stories og Ikke-Funktionelle Krav

-   **User Stories:**

-   US01: Tilføj og ændr systemtypologi uden nedetid.
-   US05: Automatisk genstart af Workers ved fejl.
-   US08: Fleksibel tilpasning af pipeline-konfigurationer.

-   **Ikke-Funktionelle Krav:**

-   IFK07: Lav latenstid under 1 ms.
-   IFK15: Horisontal skalerbarhed.
-   IFK16: Robust distribueret arkitektur.
-   IFK18: Håndtering af forbindelsestab med synkronisering.
