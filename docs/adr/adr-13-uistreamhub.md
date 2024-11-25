ADR 13: Design af Brugergrænsefladen for StreamHub

Placering: /docs/adr/adr-13-uistreamhub.md
Status: Besluttet
Dato: 18/10 2024
Beslutningstagere: Tommi Iversen


Kontekst
StreamHub kræver en brugervenlig og realtidsopdateret grænseflade til overvågning og kontrol af systemets Engines og Workers. Brugergrænsefladen skal understøtte visning af metrics, logs og kontrolmuligheder for hver Engine og Worker. Fokus er på at sikre en responsiv og dynamisk oplevelse for brugeren i et produktionsmiljø med høje krav til stabilitet og effektivitet.

Problemet
Hvordan kan brugergrænsefladen designes og implementeres for at:
1.	Sikre intuitiv navigation og præsentation af data (IFK06).
2.	Tilbyde realtidsopdateringer uden manuel sideopdatering (IFK07).
3.	Understøtte kontrolfunktioner som start/stop af Workers og visning af metrics/logs i realtid (US01, US02, US05).

Beslutning
Valg af Blazor til Brugergrænsefladen
Brugergrænsefladen implementeres med Blazor, som muliggør en komponentbaseret struktur og dynamisk opdatering via SignalR.
•	Hovedkomponenter:
o	EngineComponent: Centraliseret styring og visning af data for hver Engine.
o	WorkerComponent: Præsentation og kontrol af individuelle Workers.
o	WorkerControlsComponent: Mulighed for at starte/stoppe Workers samt se videofeeds og logfiler.

Begrundelse
•	Brugervenlighed: En klar og intuitiv struktur, der giver brugeren adgang til alle nødvendige funktioner uden kompleks navigation (IFK06).
•	Realtidsopdateringer: SignalR sikrer dynamiske opdateringer, så systemtilstand reflekteres med det samme i UI’et (IFK07, US02).
•	Modularitet og Genbrug: Den komponentbaserede arkitektur gør det muligt at genbruge og udskifte funktionalitet uden at påvirke hele systemet (IFK17).

Arkitektoniske Overvejelser
•	Dynamisk Datavisning: Metrics og logs opdateres i realtid via SignalR, hvilket reducerer forsinkelser og sikrer præcision i produktionsmiljøer.
•	Komponenter og Interaktion:
o	EngineComponent håndterer dataindsamling og kommunikation med backend via SignalR.
o	WorkerComponent og dets child-komponenter giver detaljeret kontrol over hver Worker.
•	Mockup: Som illustreret i Figure 14, er grænsefladen designet til at præsentere alle relevante data og kontrolmuligheder på en overskuelig måde.

Konsekvenser
Positive:
•	Intuitiv og dynamisk grænseflade, der forbedrer brugeroplevelsen.
•	Modularitet sikrer fremtidig skalerbarhed og fleksibilitet.
•	Realtidsopdateringer øger systemets effektivitet i produktionsmiljøer.
Negative:
•	Kompleksiteten i SignalR-integrationen kan øge udviklings- og vedligeholdelsesomkostninger.
•	Blazor-serverens afhængighed af konstant forbindelse kan være en begrænsning i netværk med høj latens.

Relaterede User Stories og Ikke-Funktionelle Krav
User Stories:
•	US01: Tilføj og administrer Workers uden nedetid.
•	US02: Realtidsmonitorering af systemets ydeevne.
•	US05: Automatisk håndtering af Workers i fejltilstand.
Ikke-Funktionelle Krav:
•	IFK06: En brugervenlig webgrænseflade til teknikere.
•	IFK07: Lav latenstid i dataopdateringer.
•	IFK17: Et modulært design, der understøtter skalerbarhed.








