# Struktura rada

Fakultet traži najmanje 30 stranica, uvod do 2 i zaključak do 1 stranice; poglavlje o sopstvenom doprinosu je centralno i najduže (proveriti sa mentorom, vidi mentor-questions.md).

## Istraživačka pitanja (kičma rada)

- **RQ1** Koliko pretraga po materijalu pomaže malom lokalnom modelu na ispitnim zadacima?
- **RQ2** Koliko dodaju deterministički alati (rešavači) iznad pretrage?
- **RQ3** Radi li lestvica nagoveštaja kako je zamišljena (ne otkriva rešenje prerano, vodi dalje)?
- **RQ4** Gde je lokalni model dovoljan, a gde ne (po vrsti zadatka, resursi, latencija)?

## Poglavlja

| # | Poglavlje | Obim (str.) | Oslanja se na | Stanje |
|---|---|---|---|---|
| 1 | Uvod: problem, cilj, RQ1-RQ4, doprinosi | do 2 | - | pisati poslednje |
| 2 | Učenje uz tutora: ITS i dokazi o efikasnosti, skela i nagoveštaji, produktivna borba, kognitivno prepuštanje | 6-8 | literature.md odeljci 1 i 4 | čeka čitanje izvora |
| 3 | LLM tutori i RAG u obrazovanju: komercijalni režimi, akademski sistemi, dokazi, praznine, pozicioniranje (poslednji odeljak) | 8-10 | literature.md odeljci 2 i 3 | čeka čitanje i ponovnu pretragu |
| 4 | Tehnologije: lokalni naspram cloud LLM-a, embedding modeli, RAG i hibridna pretraga (RRF), poređenje skladišta vektora (pgvector, Qdrant, Pinecone i dr.) | 5-7 | `eval/embedding_eval.py`, dokumentacija skladišta | može se pisati odmah (osim poređenja skladišta) |
| 5 | Zahtevi i dizajn: zahtevi izvedeni iz 2 i 3, arhitektura, privatnost, predmeti kao moduli, granica "izračunato" / "generisano" | 5-7 | README, `ISubjectProfile`, privacy-threat-model.md | može se pisati odmah |
| 6 | Implementacija: ingest, hibridna pretraga, prepoznavanje namere, lestvica nagoveštaja, rešavači, profili i upload, UI, Docker | 8-10 | kod u `Server/`, `Client/` | može se pisati odmah |
| 7 | Evaluacija (po istraživačkim pitanjima): 7.1 metodologija i pretnje validnosti; 7.2 pretraga (embedding modeli, pragovi); 7.3 ablacija plain / RAG / RAG + alati uključujući hold-out; 7.4 lestvica nagoveštaja; 7.5 resursi i latencija; 7.6 poređenje sa ChatGPT/Claude; 7.7 mala korisnička studija (opciono) | 12-15 | evaluation-protocol.md, results-so-far.md | deo gotov, hold-out i ponavljanja nedostaju |
| 8 | Diskusija i budući rad: deljenje materijala među studentima, model studenta, cloud model | 3-4 | - | posle evaluacije |
| 9 | Zaključak | do 1 | - | poslednje |

Uz to: literatura (IEEE) i prilozi (promptovi, šema baze, rubrika za ocenjivanje, model pretnji).

## Predloženi doprinosi (skromna formulacija, bez "prvi" i "jedinstven")

1. Referentna lokalna implementacija RAG tutora za više predmeta koja radi na laptopu bez GPU-a, uz izmeren utrošak resursa.
2. Hibridna arhitektura: pretraga za objašnjenja i deterministički rešavači za šablonske zadatke, uz vidljivo označavanje porekla odgovora.
3. Empirijska evaluacija (ablacija plain / RAG / RAG + alati i poređenje sa komercijalnim četbotovima) na stvarnim ispitnim zadacima na srpskom, uz hold-out ispit i ponavljanja.

Upload sopstvenog materijala **nije** doprinos (isto nude ChatGPT Projects, Claude Projects, NotebookLM); razlika je lokalnost podataka i to ide u dizajn i model pretnji, ne u tvrdnje o novosti.

## Dogovorene odluke

- Svaki student pokreće sopstvenu instancu (Docker Compose); nema zajedničke baze. Deljenje materijala među instancama je budući rad.
- Gotova baza znanja za dva predmeta može da se isporuči kao dump (`db/seed/`), ali samo uz dozvolu autora materijala (vidi mentor-questions.md).
