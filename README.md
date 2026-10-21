# AITutor

Asistent za učenje zasnovan na velikim jezičkim modelima i RAG-u (pretraga po materijalu predmeta). Radi potpuno lokalno:
modeli se pokreću preko Ollame, materijal i vektori su u PostgreSQL bazi sa pgvector ekstenzijom, a studentski materijal ne napušta računar.
Trenutno su podržana dva predmeta (Programski prevodioci, Operativni sistemi); novi predmet se dodaje implementacijom `ISubjectProfile`.

## Šta sistem radi

- Odgovara na pitanja iz gradiva, uz izvore (fajl i udaljenost chunka); ako materijal ne sadrži odgovor, to i kaže.
- Prepoznaje nameru poruke (ćaskanje, pitanje iz gradiva, zadatak) i razume prethodni tok razgovora.
- Za zadatke vodi lestvicu nagoveštaja: 1) plan, 2) razrada prva dva koraka, 3) rešenje. Ponavljajući tipovi zadataka
  (npr. proračuni) se rešavaju programski, a model samo izvlači parametre.
- Čuva istoriju razgovora po lokalnom profilu (samo ime, bez lozinke) i predmetu; razgovor se nastavlja i posle osvežavanja stranice.
- Student može da doda sopstveni materijal (PDF, TXT, MD, C, H, JAVA, SH) za izabrani predmet; fajl se obrađuje u pozadini
  i odmah učestvuje u pretrazi (izvor `moje/...`). Može se ukloniti iz panela „Moj materijal“.

## Pokretanje (Docker Compose)

Potrebno: Docker Desktop i oko 16 GB RAM-a (model od oko 6,6 GB).

1. Kopiraj `.env.example` u `.env` (opciono, podrazumevane vrednosti rade).
2. Pokreni Ollamu na računaru i preuzmi modele (jednom):
   ```
   ollama pull gemma4:e4b
   ollama pull embeddinggemma
   ```
3. Pokreni stek:
   ```
   docker compose up -d --build
   ```
4. Otvori http://localhost:8080.

Sve u kontejnerima, uključujući Ollamu (modeli se preuzimaju pri prvom pokretanju, nekoliko GB):
```
OLLAMA_BASE_URL=http://ollama:11434 docker compose --profile ollama up -d --build
```
(u PowerShell-u: `$env:OLLAMA_BASE_URL="http://ollama:11434"; docker compose --profile ollama up -d --build`)

Zaustavljanje: `docker compose down` (podaci ostaju u volumenu `aitutor_stack_pgdata`).

## Ubacivanje materijala (ingest)

Materijal se stavlja u folder (`MATERIALS_DIR`, podrazumevano `./data`) u kome **podfolderi nose naziv tipa izvora**:
`predavanja`, `vezbe`, `pismeni` (ispiti), `usmeni`, `reseni_blanketi`, `obrasci`. Podržani su PDF i tekstualni fajlovi (`.txt`, `.c`, `.h`, `.sh`, `.java`, ...).
Svaki ingest ide za jedan predmet:

```
curl -X POST http://localhost:8080/api/ingest -H "Content-Type: application/json" ^
  -d "{\"materialsPath\":\"/data/materials/os_additions\",\"subject\":\"Operativni sistemi\"}"
```

Putanja je unutar kontejnera (`/data/materials/...`). Ingest nije idempotentan: ponovni ingest istog materijala pravi duplikate.
Materijal kursa nije deo repoa; u `data/os_additions/` su samo dodaci koje je napisao autor (obrasci i kratke činjenice za Operativne sisteme).

## Gotova baza znanja (opciono)

Ako u `db/seed/` postoji `knowledge.sql.gz`, učitava se pri prvom pokretanju (prazan volumen baze), pa student odmah ima ugrađen materijal
bez ingesta. Dump se pravi iz postojeće baze (ne uključuje ispite ni studentski upload):

```
bash scripts/export-knowledge.sh <ime-kontejnera-baze>
```

Dump sadrži tekst materijala kursa, zato nije u javnom repou (`db/seed/` je u `.gitignore`) i deli se samo uz dozvolu autora materijala.
Vektori su za model `embeddinggemma` (768 dimenzija); sa drugim embedding modelom treba ponovo uraditi ingest. Ako je volumen već pravljen,
seed se ne učitava ponovo (`docker compose down -v` briše podatke).

## Razvoj bez Dockera

- Baza: PostgreSQL sa pgvector (`db/init.sql` kreira tabelu), connection string preko `dotnet user-secrets` (`ConnectionStrings:Default`).
- API: `dotnet run` u `Server/AITutor_api` (http://localhost:5000, Swagger na `/swagger`).
- Klijent: `npm install`, pa `npm run dev` u `Client/AITutor_web` (http://localhost:5173).
- Testovi: `dotnet test` u `Server` (ne trebaju Ollama ni baza).
- Eksperimenti i skripte za evaluaciju: `eval/`. Polje `mode` u `/api/chat` bira režim odgovora: `Rag` (podrazumevano, pretraga + programski alati),
  `RagNoTools` (pretraga, ali model sam rešava zadatke) i `Plain` (bez pretrage); koristi ih `eval/ablation.py` za poređenje.

## Podešavanja

`Server/AITutor_api/appsettings.json` (ili promenljive okruženja, npr. `Ollama__ChatModel`):
`Ollama:ChatModel`, `Ollama:EmbeddingModel`, `Ollama:BaseUrl`, `Subjects:DefaultSubject`, `Cors:Origins`.
Promena embedding modela zahteva novu dimenziju vektora u bazi i ponovni ingest svih materijala.
