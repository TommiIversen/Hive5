ADR 11: MetricsService i Hive 5
-------------------------------

**Placering:** /docs/adr/adr-11-metricsservice.md\
**Status:** Besluttet\
**Dato:** 2/09 2024\
**Beslutningstagere:** Tommi Iversen

##### Kontekst

Hive 5 kræver kontinuerlig overvågning af systemressourcer for at sikre stabilitet og performance. MetricsService er designet til at indsamle og rapportere data om CPU-, RAM-, netværks- og GPU-forbrug, som muliggør realtidsindsigt i systemets ydeevne. Disse data er kritiske for teknikere til overvågning og fejlhåndtering (US02, IFK07).

MetricsService anvender hjælpeklasser som CpuUsageMonitor og NetworkUsageMonitor for at modularisere dataindsamlingen og integrerer med en MessageQueue for at sikre effektiv dataudsendelse til StreamHub.

##### Problemet

Hvordan kan MetricsService designes til at indsamle og levere metrics effektivt uden at belaste systemets ressourcer og samtidig forblive fleksibel og modulær, så komponenten let kan udvides eller ændres?

##### Beslutning

MetricsService implementeres med følgende designvalg:

1.  **Modularisering af Dataindsamling:**\
    Indsamling af metrics håndteres af specialiserede hjælpeklasser (CpuUsageMonitor, MemoryUsageMonitor, NetworkUsageMonitor), hvilket gør det muligt at udskifte eller opdatere individuelle moduler uden at påvirke hele MetricsService.
2.  **Integration med MessageQueue:**\
    MetricsService bruger en intern MessageQueue til at afkoble dataindsamling fra dataudsendelse. Dette sikrer, at metrics kan håndteres i batches og reducerer risikoen for flaskehalse ved kommunikation med StreamHub.
3.  **Periodisk Dataindsamling:**\
    Dataindsamling sker med faste intervaller, konfigurerbare gennem systemets indstillinger, for at balancere opdateringsfrekvens og ressourceforbrug.

##### Begrundelse

-   **Ydeevneovervågning (US02):** MetricsService leverer opdateret information om systemressourcer, hvilket gør det muligt for teknikere at identificere og reagere på ressourcebelastninger i realtid.
-   **Modularitet (IFK17):** Ved at isolere dataindsamlingen sikrer designet fleksibilitet, så nye ressourceovervågningselementer kan tilføjes uden større systemændringer.
-   **Skalerbarhed (IFK15):** Integration med MessageQueue og asynkron databehandling gør det muligt for MetricsService at håndtere stigende krav uden ydelsesnedgang.

##### Tekniske Overvejelser

1.  **Udvidelsesmuligheder:**\
    Designet muliggør nem tilføjelse af nye monitoreringsklasser, såsom DiskUsageMonitor eller GpuUsageMonitor, for yderligere indsigt i systemets ydeevne.

##### Konsekvenser

**Positive Konsekvenser:**

-   **Realtidsindsigt:** Teknikere kan overvåge systemets ydeevne i realtid og reagere proaktivt på belastninger eller fejl.
-   **Modularitet:** Designet muliggør nem udskiftning og vedligeholdelse af monitoreringsklasser.
-   **Lav belastning:** Ressourceoptimering sikrer, at MetricsService ikke forringer systemets samlede ydeevne.

**Negative Konsekvenser:**

-   **Kompleksitet:** Integrationen med MessageQueue og afhængigheder af flere hjælpeklasser øger komponentens kompleksitet.
-   **Skalerbarhedsbegrænsninger:** Selvom MetricsService er optimeret, kan den samlede belastning stige markant i systemer med mange Engines.

##### Relaterede User Stories og Ikke-Funktionelle Krav

**User Stories:**

-   **US02:** Realtidsovervågning af systemets ydeevne.

**Ikke-Funktionelle Krav:**

-   **IFK07:** Lav latenstid under 1 ms på localhost.
-   **IFK15:** Horisontal skalerbarhed.
-   **IFK17:** Modulært design for fleksibilitet.
