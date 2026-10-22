# Struktura rada

Uslov fakulteta: najmanje 30 stranica; plan je najmanje 40. Uvod do 2 stranice, zaključak do 1, poglavlje o sopstvenom doprinosu centralno i najduže. Korisnička studija nije deo rada (odluka: izbacuje se poglavlje 7.7).
Format: Times New Roman 12, A4, margine 2,5 cm, latinica; pravila su u `make_reference_docx.py`. Rok: 6 meseci od odobravanja teme (datum odobrenja proveriti u Službi; ako je 24.9.2026, rok je 24.3.2027).

## Istraživačka pitanja (kičma rada)

- **RQ1** Koliko pretraga po materijalu pomaže malom lokalnom modelu na ispitnim zadacima?
- **RQ2** Koliko dodaju deterministički alati (rešavači) iznad pretrage?
- **RQ3** Radi li lestvica nagoveštaja kako je zamišljena (ne otkriva rešenje prerano, vodi dalje)?
- **RQ4** Gde je lokalni model dovoljan, a gde ne (po vrsti zadatka, resursi, latencija)?

## Poglavlja i podela posla

| # | Poglavlje | Obim (str.) | Piše | Stanje |
| --- | --- | --- | --- | --- |
| 1 | Uvod: problem, cilj, RQ1-RQ4, doprinosi | do 2 | claude.ai | posle evaluacije |
| 2 | Učenje uz tutora | 6-8 | claude.ai | nacrt u `draft/02-...` (čeka čitanje izvora, vidi PROVERITI.md) |
| 3 | LLM tutori i RAG; pozicioniranje | 8-10 | claude.ai | nacrt u `draft/03-...` (čeka SortingHat u celini) |
| 4 | Tehnologije (lokalni modeli, embeddinzi, RAG, skladišta vektora) | 5-7 | Claude Code | nacrt u `draft/04-...`; poređenje skladišta samo iz dokumentacije |
| 5 | Zahtevi i dizajn | 5-7 | Claude Code | nacrt u `draft/05-...` |
| 6 | Implementacija (isečci koda do ~25 linija, dijagrami u Mermaid-u) | 8-10 | Claude Code | nacrt u `draft/06-...` |
| 7 | Evaluacija (po istraživačkim pitanjima) | 12-15 | brojeve daje Claude Code (`eval/`), tekst claude.ai | čeka merenja |
| 8 | Diskusija i budući rad | 3-4 | claude.ai | posle evaluacije |
| 9 | Zaključak | do 1 | claude.ai | poslednje |

Poglavlje 7: 7.1 metodologija i pretnje validnosti; 7.2 pretraga (embedding modeli, pragovi); 7.3 ablacija plain / RAG / RAG + alati, razvojni skup i hold-out; 7.4 lestvica nagoveštaja; 7.5 resursi i latencija; 7.6 poređenje sa ChatGPT i Claude.
Uz to: apstrakt na srpskom i engleskom, literatura (IEEE, svi autori), prilozi (model pretnji, rubrika za ocenjivanje, promptovi, šema baze).

Svaki broj u tekstu mora da pokazuje na fajl u `eval/results/`. Rezultati se ne izmišljaju ni ne zaokružuju "u korist" sistema; ograničenja idu na jedno mesto (7.1).

## Procena obima (nacrti, 22.10.2026)

Reči u nacrtima: poglavlje 2 oko 2000, 3 oko 2200, 4 oko 2000, 5 oko 2100, 6 oko 2700; zajedno oko 11000 reči, tj. otprilike 25-28 stranica uz tabele i slike pre literature. Poglavlja 1, 7, 8, 9 i apstrakt treba da dodaju još oko 15 stranica; obim od 40 stranica je ostvariv.
Brojanje: `bash count.sh` (reči po poglavlju). Tačan broj stranica daje Word (ili LibreOffice, ako je instaliran, preko `build.sh`).

## Predloženi doprinosi (skromna formulacija, bez "prvi" i "jedinstven")

1. Referentna lokalna implementacija RAG tutora za više predmeta koja radi na laptopu bez GPU-a, uz izmeren utrošak resursa.
2. Hibridna arhitektura: pretraga za objašnjenja i deterministički rešavači za šablonske zadatke, uz vidljivo označavanje porekla odgovora.
3. Empirijska evaluacija (ablacija plain / RAG / RAG + alati i poređenje sa komercijalnim četbotovima) na stvarnim ispitnim zadacima na srpskom, uz hold-out ispit i ponavljanja.

Upload sopstvenog materijala **nije** doprinos (isto nude ChatGPT Projects, Claude Projects, NotebookLM); razlika je lokalnost podataka i to ide u dizajn i model pretnji.

## Dogovorene odluke

- Svaki student pokreće sopstvenu instancu (Docker Compose); nema zajedničke baze. Deljenje materijala među instancama je budući rad.
- Gotova baza znanja za dva predmeta može da se isporuči kao dump (`db/seed/`), ali samo uz dozvolu autora materijala; nije u javnom repou.
- Materijal koji student doda zajednički je za instancu (ne po profilu).
- Korišćenje LLM alata pri izradi koda i pisanju navodi se u radu jednom neutralnom rečenicom (predlog: „Pri razvoju koda i pisanju korišćeni su LLM alati; autor je proverio rezultate i odgovara za sadržaj.“), a mentoru se to pominje uz draft. Pravilo fakulteta proveriti (mentor-questions.md, pitanje 10).
