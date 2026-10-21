# Šta je do sada izmereno

Svi podaci su u `eval/results/`; sirovi odgovori sa doslovnim izvodima iz materijala kursa čuvaju se van repoa (`zavrsni/eval_raw_private/`).

## 1. Poređenje embedding modela (Programski prevodioci)

Skripta: `eval/embedding_eval.py`, izlaz: `eval/results/embedding_eval.txt`. Mali skup pitanja iz predmeta i pitanja van predmeta; broj pitanja treba navesti iz skripte pre upotrebe u radu.

| Model | hit@1 | hit@4 | MRR | AUC (u predmetu / van) | uravnotežena tačnost |
|---|---|---|---|---|---|
| nomic-embed-text | 0.63 | 0.85 | 0.73 | 0.86 | 0.82 |
| embeddinggemma | 0.93 | 0.96 | 0.95 | 0.99 | 0.94 |
| bge-m3 | 0.85 | 0.96 | 0.91 | 0.97 | 0.92 |

Odluka: embeddinggemma. Pragovi odbijanja su po predmetu (PP 0.56, OS 0.64), sivu zonu odlučuje LLM (`eval/threshold_eval.py`).
Ograničenje: mali označeni skup; za poglavlje 7.2 treba ~50 označenih pitanja po predmetu (recall@k, MRR) i kriva preciznost-odziv odbijanja.

## 2. Ablacija na ispitnim zadacima (Operativni sistemi), 10 zadataka, n=1

`eval/ablation.py` + `eval/ablation_report.py`, rezultat `eval/results/ablation_os_v1.json` (stanje posle ispravke klasifikatora namere za zadatak teorije).

| Režim | Potpuno tačno | Delimično |
|---|---|---|
| plain (bez pretrage) | 0/10 | 5 |
| RAG (bez alata) | 4/10 | 4 |
| RAG + alati | 10/10 | 0 |

Po vrsti zadatka: računski zadaci (raspoređivanje, disk, i-node, adrese, LRU) plain i RAG uglavnom greše, rešavaju ih tek alati; Da/Ne i teorija: RAG tačan, plain delimično; monitor: oba RAG režima tačna zahvaljujući proverenim obrascima (kartice), plain delimično.
Fisher (dvostrani): 0/10 prema 10/10 p≈1.1e-5, 4/10 prema 10/10 p≈0.011.

**Ne sme se tvrditi:** da 10/10 pokazuje uopštavanje. Alati, obrasci i kartice pisani su nad istim ispitima (in-sample), svaka ćelija je jedan pokušaj, tačni odgovori za računske zadatke su izračunati ručno (autor), ne od profesora.
Potrebno: hold-out ispit, 3-5 ponavljanja, nezavisna rešenja (evaluation-protocol.md).

Nalaz usput: klasifikator namere je pitanje "Navesti tri stavke..." slao u zadatke; ispravljeno opštim pravilom u promptu i ponovljena provera.

## 3. Ostalo

- `eval/chat_bench.py`: ranija poređenja chat modela (llama3.1:8b prema gemma4:e4b) i lestvice nagoveštaja za Programske prevodioce.
- `eval/os_bench.py` (fast): 17/17 provera rešavača na pravim ispitnim zadacima za OS; `eval/os_sync_probe.py`, `eval/os_theory_probe.py`: sinhronizacija i teorija.
- Poznate rupe u OS-u (nisu pokrivene): Clock sa tabelom stanja, brojanje pristupa disku pri čitanju/izmeni blokova, pthread C programi, shell skripte, vreme diska (rpm, seek).
- Resursi (latencija, RAM) još nisu sistematski mereni; potrebno za poglavlje 7.5.
