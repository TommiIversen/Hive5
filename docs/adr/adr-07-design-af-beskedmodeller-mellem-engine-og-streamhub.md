ADR 07: Design af Beskedmodeller mellem Engine og StreamHub
-----------------------------------------------------------

**Placering:** /docs/adr/adr-07-design-af-beskedmodeller-mellem-engine-og-streamhub.md\
**Status:** Besluttet\
**Dato:** 24/10 2024\
**Beslutningstager:** Tommi Iversen

##### Kontekst

Hive 5 kræver effektiv og struktureret kommunikation mellem **Engine** og **StreamHub** for at opfylde kravene om realtidsydelse, lav latenstid og robusthed. For at sikre dette er det nødvendigt at definere klare beskedmodeller (DTOs), der kan bruges til at udveksle data mellem komponenterne. Beskedmodellerne skal understøtte forskellige kommunikationsbehov, herunder kommandoer, hændelser og forespørgsler.

I tidligere beslutninger er det fastlagt, at Hive 5 skal have en modulær og distribueret arkitektur (*Bilag 3: ADR 01: Design af Systemarkitektur*) og implementere nøglekomponenter og services, der muliggør realtidskommunikation og overvågning (*Bilag 4: ADR 02: Identifikation af Nøglekomponenter og Services*).

##### Problemet

Hvordan kan vi designe beskedmodeller (DTOs) mellem **Engine** og **StreamHub**, der:

-   **Opfylder kravene** om realtidskommunikation, lav latenstid og robusthed.
-   **Understøtter forskellige kommunikationstyper** som Commands, Events og Queries.
-   **Sikrer fleksibilitet**, genbrug og vedligeholdelsesvenlighed.
-   **Er konsistente** og lette at implementere på tværs af komponenter.
-   **Kan deles** mellem **Engine** og **StreamHub** for at sikre ensartethed og minimere kode-duplikering.

##### Beslutning

Vi har besluttet at:

1.  **Opdele beskedmodellerne i tre hovedkategorier**:

-   **Commands**: Anmodninger fra **StreamHub** til **Engine** om at udføre specifikke handlinger.
-   **Events**: Meddelelser fra **Engine** til **StreamHub** om ændringer, opdateringer eller hændelser.
-   **Queries**: Forespørgsler fra **StreamHub** til **Engine** for at hente specifikke data eller historik.

3.  **Definere fælles basisklasser**:

-   **BaseMessage**: Indeholder fælles egenskaber som tidsstempler og unikke identifikatorer.
-   **BaseWorkerInfo** og **BaseEngineInfo**: Indeholder fælles egenskaber relateret til Workers og Engines.

5.  **Placere beskedmodellerne (DTOs) i et fælles projekt** kaldet **Common**, som både **Engine** og **StreamHub** deler.
6.  **Strukturere DTO'erne i passende mapper og namespaces** inden for **Common/DTOs**, såsom:

-   DTOs/Commands/
-   DTOs/Events/
-   DTOs/Queries/
-   DTOs/Enums/

##### Begrundelse

-   **Opdeling i Commands, Events og Queries**:

-   **Klarhed og Struktur**: Gør det tydeligt, hvilken retning beskederne sendes, og hvad deres formål er.
-   **Fleksibilitet**: Muliggør nem tilføjelse af nye beskedtyper uden at påvirke eksisterende funktionalitet.
-   **Vedligeholdelse**: Letter forståelsen for udviklere og reducerer kompleksiteten i systemet.

-   **Fælles Basisklasser**:

-   **Genbrug af Kode**: Reducerer kode-duplikering ved at samle fælles egenskaber i basisklasser.
-   **Ensartethed**: Sikrer, at alle beskedtyper følger samme struktur og standarder.

-   **Fælles Projekt for DTO'er (Common)**:

-   **Konsistens på Tværs af Komponenter**: Ved at dele DTO'erne mellem **Engine** og **StreamHub** elimineres risikoen for inkonsistenser.
-   **Effektiv Udvikling**: Ændringer i beskedmodellerne behøver kun at foretages ét sted, hvilket forenkler vedligeholdelsen.

-   **Struktureret Organisering af Koden**:

-   **Overskuelighed**: Mapper og namespaces gør det nemt at navigere i koden og finde specifikke beskedtyper.
-   **Skalerbarhed**: Forbereder projektet på fremtidige udvidelser og tilføjelser af nye beskedtyper.

##### Overvejede Alternativer

1.  **Separate DTO'er i Hver Komponent**:

-   *Afvisning*: Dette ville føre til dobbeltarbejde og risiko for inkonsistens, da ændringer skulle synkroniseres manuelt mellem **Engine** og **StreamHub**.
-   *Begrundelse*: En fælles kodebase for DTO'erne er mere effektiv og sikker.

3.  **Brug af Protokolbuffere (Protobuf) eller Andre Serialiseringsværktøjer**:

-   *Afvisning*: Introducerer ekstra kompleksitet og afhængigheder, som ikke er nødvendige for projektets behov.
-   *Begrundelse*: Vores behov kan opfyldes med en enklere løsning, der er lettere at implementere og vedligeholde.

5.  **Flad Struktur Uden Opdeling i Beskedtyper**:

-   *Afvisning*: Ville reducere klarheden og gøre koden mere uoverskuelig.
-   *Begrundelse*: Opdelingen i Commands, Events og Queries giver en naturlig og logisk struktur, der understøtter systemets funktionalitet.

##### Konsekvenser

-   **Positive Konsekvenser**:

-   *Fleksibilitet og Udvidelighed*: Let at tilføje nye beskedtyper og funktioner.
-   *Forbedret Vedligeholdelse*: Mindre risiko for fejl og inkonsistens.
-   *Effektiv Kommunikation*: Struktureret og klar kommunikation mellem komponenter sikrer høj ydeevne og lav latenstid.

-   **Negative Konsekvenser**:

-   *Øget Initial Kompleksitet*: Kræver en initial indsats for at designe og implementere strukturen.
-   *Afhængighed mellem Projekter*: Ændringer i **Common**-projektet kan påvirke begge komponenter, hvilket kræver koordinering.

##### Relaterede User Stories og Ikke-Funktionelle Krav

-   **User Stories**:

-   *US02*: Realtidsovervågning af CPU-, GPU-, netværks- og RAM-forbrug.
-   *US05*: Automatisk genstart af workers ved fejl.
-   *US07*: Realtime statusoverblik for alle workers.
-   *US08*: Fleksibel tilpasning af pipeline-konfigurationer.

-   **Ikke-Funktionelle Krav**:

-   *IFK05*: Hurtig og enkel opsætning.
-   *IFK07*: Lav latenstid under 1 ms på localhost.
-   *IFK16*: Robust distribueret arkitektur.
-   *IFK17*: Modulært design for fleksibilitet.

##### Relaterede ADR'er

-   **ADR 01**: Design af Systemarkitektur.
-   **ADR 02**: Identifikation af Nøglekomponenter og Services.

##### Konklusion

Ved at designe beskedmodellerne mellem **Engine** og **StreamHub** som beskrevet ovenfor opfylder vi systemets krav om realtidskommunikation, lav latenstid og robusthed. Strukturen understøtter fleksibilitet, genbrug og vedligeholdelsesvenlighed, hvilket er afgørende for systemets succes og fremtidige udvikling.
