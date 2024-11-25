ADR 10: Database og State Management i Engine
Bilag: ADR 10 - Database og State Management Design
Placering: /docs/adr/adr-10-database-og-state-management.md
Status: Besluttet
Dato: 5/10 2024
Beslutningstager: Tommi Iversen

Kontekst
Databasen i Hive 5 er ansvarlig for at gemme og administrere systemets tilstand og konfigurationer. Den understøtter kritiske funktioner som logning af Worker-ændringer, Watchdog-events og systemtilstand. Denne persistens sikrer konsistens og stabilitet i systemet, især under genstart og fejlhåndtering. SQLite er tidligere valgt som database på grund af enkelhed og fleksibilitet i standalone-miljøer.

Problemet
Hvordan kan database- og state management-laget designes til at:
•	Sikre konsistens mellem Engine og StreamHub?
•	Understøtte vedvarende logning og rollback af systemtilstand?
•	Minimere kompleksitet, samtidig med at systemets fleksibilitet og modularitet opretholdes?

Beslutning
Databasen implementeres som en filbaseret SQLite-løsning, med et struktureret dataadgangsmønster baseret på Repository Pattern. Dette design sikrer isoleret dataadgang, vedligeholdelsesvenlighed og understøtter systemets funktionelle krav.
Hovedfunktioner:
1.	Konfigurationsstyring:
o	Gemmer pipeline-konfigurationer og systemindstillinger med mulighed for versionering og rollback.
o	Muliggør dynamisk tilpasning af systemet uden systemnedetid (US08, IFK17).
2.	State Management:
o	Vedvarende lagring af systemtilstand, der sikrer konsistens mellem Engine og StreamHub.
o	Genopretter systemtilstand ved genstart eller opdateringer (IFK18).
3.	Logning af Worker-ændringer:
o	Logger alle ændringer i Worker-konfigurationer for at give et historisk overblik og mulighed for audit.
4.	Watchdog Event-logning:
o	Gemmer hændelseslogfiler op til en fejl for at lette efterfølgende fejlfinding.

Interaktion med Andre Komponenter
•	WorkerManager:
o	Bruger databasen til at opdatere og læse Worker-tilstand og historik.
o	Logger ændringer i Worker-konfigurationer i WorkerChangeLogs-tabellen.
•	RunnerWatchdog:
o	Gemmer kritiske logdata om Watchdog-hændelser og fejl i WorkerEvent og WorkerEventLog.
•	StreamHub:
o	Synkroniserer med Engine for at opdatere konfigurationer og overvåge systemtilstand.

Designvalg og Begrundelse
1.	Repository Pattern:
o	Anvendes til at adskille databaseinteraktion fra forretningslogikken.
o	Letter fremtidig vedligeholdelse og understøtter mulige ændringer i databasearkitekturen.
2.	Skalerbarhed og Modularitet:
o	Databasearkitekturen er designet til at understøtte horisontal skalerbarhed og fremtidige udvidelser uden at ændre det eksisterende design (IFK15).
3.	Data Konsistens og Sikkerhed:
o	Atomare transaktioner og dataintegritet sikres gennem SQLite’s indbyggede mekanismer, hvilket minimerer risikoen for korruption eller tab af kritiske data (IFK16).

Refleksion over Designet
Dette design balancerer enkelhed og robusthed, hvilket gør det muligt at understøtte dynamiske krav til pipeline-konfigurationer og logning. Ved brug af Repository Pattern opnås en klar adskillelse mellem forretningslogik og dataadgang, hvilket gør koden lettere at vedligeholde og udvide.

Konsekvenser
Positive:
•	Modularitet: Let at tilføje nye funktioner uden at ændre eksisterende dataadgangsmønstre.
•	Robusthed: Sikrer dataintegritet selv under netværksproblemer eller pludselige systemafbrydelser.
Negative:
•	Centralisering: Selvom decentral lagring er en fordel, kræver sammenlægning af data fra flere Engines yderligere integration.

Relaterede User Stories og Ikke-Funktionelle Krav
•	User Stories:
o	US08: Tilpasning og rollback af pipeline-konfigurationer.
o	US05: Automatisk genstart af Workers ved fejl.
•	Ikke-Funktionelle Krav:
o	IFK15: Horisontal skalerbarhed.
o	IFK16: Robust distribueret arkitektur.
o	IFK18: Håndtering af forbindelsestab med synkronisering.


