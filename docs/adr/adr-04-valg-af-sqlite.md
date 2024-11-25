ADR 04: Valg af SQLite til Database og State Management
-------------------------------------------------------

**Placering:** /docs/adr/adr-04-valg-af-sqlite.md\
**Status:** Besluttet\
**Dato:** 24/11 2024\
**Beslutningstager:** Tommi Iversen

##### Kontekst

Hive 5 kræver en database til lagring af konfigurationsdata, systemtilstand og historik, som understøtter funktioner som rollback af pipeline-konfigurationer og hurtig adgang til lokale data. Løsningen skal være let at implementere, vedligeholde og passe til et distribueret system med multiple Engines. Krav som enkel opsætning (**IFK 5**), lav latenstid (**IFK 7**) og kompatibilitet med standard PC-hardware (**IFK 1**) er afgørende.

##### Problemet

Hvordan kan Hive 5 håndtere lagring og state management, så det understøtter decentralitet, realtidsydelse og enkel vedligeholdelse uden at introducere unødig kompleksitet?

##### Beslutning

Vi har besluttet at anvende **SQLite** som database til Hive 5.

##### Begrundelse

1.  **Enkel Implementering og Vedligeholdelse**

-   **Kravopfyldelse:** **IFK 5** (hurtig og enkel opsætning).
-   **Fordel:** SQLite kræver ingen opsætning af en central database-server og fungerer som en embedded database, hvilket reducerer kompleksiteten og gør det muligt at implementere databasen hurtigt og problemfrit.

3.  **Lokal Dataadgang og Ydeevne**

-   **Kravopfyldelse:** **IFK 7** (lav latenstid).
-   **Fordel:** Hver Engine kan have sin egen lokale SQLite-instans, hvilket eliminerer netværkslatens og sikrer hurtig adgang til data. Dette understøtter realtidsydelse og gør løsningen robust i distribuerede systemer.

5.  **Understøttelse af Transaktioner og Struktur**

-   **Kravopfyldelse:** **US08** (tilpasning og rollback af pipeline-konfigurationer).
-   **Fordel:** SQLite tilbyder en fuldt ACID-kompatibel struktur, der sikrer data-integritet og konsistens. Dette gør den velegnet til funktioner som rollback og historikstyring.

7.  **Lav Ressourcepåvirkning**

-   **Kravopfyldelse:** **IFK 1** (understøttelse af standard PC-hardware).
-   **Fordel:** SQLite er en lightweight database med minimal ressourceforbrug, hvilket gør den ideel til systemer, der skal køre på almindelig PC-hardware uden dedikeret database-infrastruktur.

##### Konsekvenser

**Positive Konsekvenser:**

-   **Decentral lagring:** Lokal SQLite-instans pr. Engine reducerer afhængigheden af en central server.
-   **Forenklet vedligeholdelse:** Ingen krav om opsætning og overvågning af en database-server.
-   **Lav overhead:** SQLite's letvægtsdesign sikrer hurtig implementering og minimal ressourcepåvirkning.

**Negative Konsekvenser:**

-   **Begrænset skalerbarhed:** SQLite er mindre velegnet til håndtering af store mængder data eller mange samtidige forbindelser sammenlignet med serverbaserede løsninger som PostgreSQL.
-   **Mangel på centraliseret adgang:** Decentral lagring gør det sværere at samle og analysere data fra flere Engines uden yderligere implementering.

##### Overvejede Alternativer

1.  **PostgreSQL eller MySQL**

-   **Fordele:** Håndterer store datamængder og mange samtidige forbindelser.
-   **Ulemper:** Kræver opsætning og vedligeholdelse af en central database-server, hvilket øger kompleksiteten og introducerer et single point of failure.

3.  **NoSQL-løsninger som MongoDB**

-   **Fordele:** Tilbyder fleksibilitet og enkel skalerbarhed.
-   **Ulemper:** Mangler fuld ACID-kompatibilitet, hvilket gør dem uegnede til funktioner som rollback og transaktionsstyring.

##### Relaterede User Stories og Ikke-Funktionelle Krav

**User Stories:**

-   **US08:** Tilpasning og rollback af pipeline-konfigurationer.

**Ikke-Funktionelle Krav:**

-   **IFK 1:** Understøtter standard PC-hardware.
-   **IFK 5:** Hurtig og enkel opsætning.
-   **IFK 7:** Lav latenstid under 1 ms.
