ADR 03: Valg af Blazor Server til Brugergrænsefladen
----------------------------------------------------

**Placering:** /docs/adr/adr-03-valg-af-blazor-server.md\
**Status:** Besluttet\
**Dato:** 24/11 2024\
**Beslutningstager:** Tommi Iversen

##### Kontekst

Hive 5 kræver en webbaseret brugergrænseflade, der kan understøtte realtidskommunikation, systemstyring og overvågning. Brugergrænsefladen skal være enkel at implementere, vedligeholde og skalerbar. Derudover skal løsningen understøtte integration med eksisterende .NET-komponenter og overholde kravene til lav latenstid (**IFK 7**), enkel opsætning (**IFK 5**) og udvikling i C# (**IFK 11**).

##### Problemet

Hvordan kan Hive 5 levere en webbaseret brugergrænseflade, der muliggør realtidsopdateringer, reducerer teknisk overhead og er let at vedligeholde, uden at introducere unødvendig kompleksitet eller afhængighed af andre teknologier?

##### Beslutning

Vi har besluttet at anvende **Blazor Server** som frontend-framework for Hive 5's webbaserede brugergrænseflade.

##### Begrundelse

1.  **Integration med .NET og C#**

-   **Kravopfyldelse:** **IFK 11** (udvikling i C#).
-   **Fordel:** Blazor muliggør udvikling af frontend og backend med samme teknologistak (C# og .NET), hvilket reducerer behovet for at skifte mellem forskellige sprog og frameworks. Dette skaber en ensartet udviklingsoplevelse og forenkler vedligeholdelsen.

3.  **Reduceret Teknisk Overhead**

-   **Kravopfyldelse:** **IFK 5** (hurtig og enkel opsætning), **US 9** (robust distribueret system).
-   **Fordel:** Blazor Server kræver ikke separate API-endpoints til kommunikation mellem frontend og backend. Komponenter og services kan tilgås direkte via Dependency Injection (DI), hvilket mindsker udviklingsomkostninger og risikoen for fejl.

5.  **Realtidskommunikation**

-   **Kravopfyldelse:** **IFK 7** (lav latenstid), **US 7** (realtime statusoverblik).
-   **Fordel:** Blazor Server understøtter realtidsopdateringer via **SignalR** uden yderligere opsætning, hvilket sikrer en effektiv brugeroplevelse. Denne integration muliggør lav latenstid og direkte opdatering af UI-komponenter uden behov for polling eller eksterne frameworks.

7.  **Ingen Afhængighed af Node.js**

-   **Kravopfyldelse:** **IFK 5** (enkel opsætning).
-   **Fordel:** Ved at undgå Node.js og JavaScript-baserede frameworks som Vue eller React reduceres projektets kompleksitet og teknologiske afhængighed. Dette er i tråd med DR's standarder og krav om minimal overhead.

9.  **Fleksibilitet og Skalerbarhed**

-   **Kravopfyldelse:** **IFK 15** (horisontal skalerbarhed).
-   **Fordel:** Blazor Server kan let skaleres ved at tilføje flere serverinstanser bag en load balancer, hvilket understøtter skiftende belastninger uden betydelige ændringer i arkitekturen.

##### Konsekvenser

**Positive Konsekvenser:**

-   Forenklet udvikling: Direkte adgang til backend-services og SignalR via Dependency Injection.
-   Reduceret kompleksitet: Ingen behov for API'er eller JavaScript-baserede frameworks.
-   Effektiv realtime-opsætning: Realtidsopdateringer fungerer "as is" uden behov for yderligere udvikling.
-   Ensartet teknologi: Samme teknologistak (C#/.NET) i hele applikationen.

**Negative Konsekvenser:**

-   Serverafhængighed: Blazor Server kræver, at alle forbindelser håndteres via serveren, hvilket kan medføre øget serverbelastning ved mange samtidige brugere.
-   Begrænset offline-funktionalitet: Da Blazor Server kræver en konstant forbindelse til serveren, kan applikationen ikke fungere uden en aktiv netværksforbindelse.

##### Overvejede Alternativer

1.  **Vue.js eller React**

-   **Fordele:** Velkendte frameworks med omfattende økosystemer og stor fleksibilitet.
-   **Ulemper:** Kræver en Node.js-baseret udviklingspipeline og integration med API-endpoints, hvilket øger projektets kompleksitet og tekniske overhead.

3.  **Blazor WebAssembly**

-   **Fordele:** Kører i browseren og reducerer belastningen på serveren.
-   **Ulemper:** Har længere initial loadtid (der skal loades 10Mb) og kræver flere ressourcer på klienten, hvilket gør det mindre egnet til realtidsapplikationer med mange samtidige forbindelser.

##### Relaterede User Stories og Ikke-Funktionelle Krav

**User Stories:**

-   **US 7:** Realtime statusoverblik for alle workers.
-   **US 9:** Robust distribueret system.

**Ikke-Funktionelle Krav:**

-   **IFK 5:** Hurtig og enkel opsætning.
-   **IFK 7:** Lav latenstid under 1 ms.
-   **IFK 11:** Udvikling i C#.
-   **IFK 15:** Horisontal skalerbarhed.
