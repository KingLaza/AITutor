# 6. Implementacija

Ovo poglavlje opisuje kako je dizajn iz poglavlja 5 ostvaren. Kod se ne prikazuje red po red: izdvojeni su kratki isečci tamo gde je odluka važna, a ostalo je opisano rečima. Odeljak 6.1 daje pregled projekta, odeljak 6.2 unos materijala, a odeljak 6.3 pretragu. Odeljci 6.4 i 6.5 opisuju prepoznavanje namere i odgovaranje na pitanja, odeljak 6.6 lestvicu nagoveštaja, a odeljak 6.7 rešavače. Odeljak 6.8 se bavi profilima, razgovorima i otpremanjem materijala, odeljak 6.9 klijentom, a odeljak 6.10 testovima i ograničenjima.

## 6.1 Pregled projekta

Projekat ima tri dela: server, klijent i skripte za evaluaciju. Server u jeziku C# ima oko 3200 linija u biblioteci `AITutor.Core` i API-ju, od toga oko 1500 linija čine rešavači za predmet Operativni sistemi. Uz njih je oko 1270 linija testova. Klijent u jeziku TypeScript (React) ima oko 1600 linija koda i stilova. Skripte za evaluaciju u jeziku Python nalaze se u folderu `eval/`, a rezultati u `eval/results/`.

Tabela 6.1. Struktura serverskog dela

| Deo | Sadržaj |
| --- | --- |
| `AITutor.Core/Rag` | pretraga i odgovaranje (`RagQueryService`), razvrstavanje namere |
| `AITutor.Core/Data` | pristup bazi: delovi materijala, profili i poruke |
| `AITutor.Core/Ollama` | klijenti za generisanje teksta i za vektore |
| `AITutor.Core/Parsing`, `Chunking`, `Ingestion` | čitanje fajlova, deljenje na delove, unos |
| `AITutor.Core/Subjects` | profili predmeta i rešavači zadataka |
| `AITutor_api/Controllers` | HTTP interfejs: razgovor, predmeti, profili, materijal, unos |
| `AITutor.Tests` | jedinični testovi |

## 6.2 Unos materijala

Unos materijala ima četiri koraka: čitanje fajla, deljenje na delove, računanje vektora i upis u bazu. Fajlovi tipa PDF čitaju se bibliotekom PdfPig, a tekstualni fajlovi (`.txt`, `.md`, `.c`, `.h`, `.java`, `.sh` i fajlovi specifikacija `.cup` i `.flex`) čitaju se kao običan tekst.

Način deljenja na delove zavisi od tipa izvora, jer materijal različito izgleda (tabela 6.2). Predavanja, vežbe i studentski materijal dele se po stranama, uz spajanje veoma kratkih strana (ispod 40 znakova) sa sledećom. Provereni obrasci pišu se u fajlovima u kojima svaki obrazac počinje naslovom `### `, pa se dele po odeljcima. Svaki deo duži od 3000 znakova dodatno se seče, da bi stao u kontekstni prozor modela.

Tabela 6.2. Strategije deljenja materijala prema tipu izvora

| Tip izvora | Strategija |
| --- | --- |
| `predavanja`, `vezbe`, `moje` | po strani (spajaju se veoma kratke strane) |
| `reseni_blanketi` | ceo dokument je jedan deo |
| `usmeni` | po numerisanim pitanjima |
| `pismeni` | po zadacima |
| `obrasci` | po odeljcima koji počinju sa `### ` |

Pri čitanju PDF slajdova biblioteka ponekad izgubi razmake u formulama, pa nastane „reč“ od više desetina znakova koja ne nosi značenje. Takvi delovi se odbacuju: ako je više od 30% reči u delu duže od 25 znakova, deo se ne upisuje. Ceo postupak za jedan fajl sažet je u metodi koja se koristi i pri unosu iz foldera i pri otpremanju od strane studenta:

```csharp
public async Task<int> IngestFileAsync(string filePath, string sourceFileName, string subject,
                                       string sourceType, Action<int, int>? progress = null)
{
    var pages = await DocumentParserFactory.GetParser(filePath).ExtractPagesAsync(filePath);
    var chunks = ChunkingStrategyFactory.GetStrategy(sourceType).Chunk(pages)
        .SelectMany(SplitIfTooLarge)
        .Where(c => !string.IsNullOrWhiteSpace(c) && !LooksLikeGarbledFormula(c))
        .ToList();

    progress?.Invoke(0, chunks.Count);
    var done = 0;
    foreach (var chunk in chunks)
    {
        await _repository.InsertAsync(new ChunkRecord { Subject = subject, SourceType = sourceType,
            SourceFile = sourceFileName, Content = chunk,
            Embedding = await _embeddings.GetEmbeddingAsync(chunk) });
        progress?.Invoke(++done, chunks.Count);
    }
    return chunks.Count;
}
```

Parametar `progress` služi da klijent prikaže napredak: računanje vektora na procesoru traje, pa se otpremanje izvršava u pozadini (odeljak 6.8).

## 6.3 Pretraga

Vektori se čuvaju u koloni tipa `vector(768)`, a nad njom je napravljen HNSW indeks sa kosinusnom razdaljinom. Hibridna pretraga je jedan SQL upit koji spaja dva rangiranja postupkom RRF (odeljak 4.3):

```sql
WITH base AS (
    SELECT id, content, source_file, source_type, embedding <=> $1 AS dist,
           ts_rank(to_tsvector('simple', translate(lower(coalesce(source_file,'') || ' ' || content),
                               'čćžšđ', 'cczsd')), to_tsquery('simple', $2)) AS fts
    FROM chunks
    WHERE (cardinality($4::text[]) = 0 OR source_type = ANY($4)) AND ($5::text IS NULL OR lower(subject) = lower($5))
),
d AS (SELECT id, row_number() OVER (ORDER BY dist)     AS rd FROM base),
f AS (SELECT id, row_number() OVER (ORDER BY fts DESC) AS rf FROM base WHERE fts > 0)
SELECT b.content, b.source_file, b.dist, b.source_type, b.id
FROM base b JOIN d ON d.id = b.id LEFT JOIN f ON f.id = b.id
ORDER BY 1.0 / (60 + d.rd) + COALESCE(1.0 / (60 + f.rf), 0) DESC
LIMIT $3
```

Upit pokazuje dve odluke. Prvo, deo koji nema nijednu reč iz upita dobija samo rang po vektoru, a ne i po ključnim rečima (`WHERE fts > 0`), pa pretraga po ključnim rečima ne može da ga pokvari. Drugo, naziv fajla ulazi u tekst koji se pretražuje, jer su nazivi vežbi i predavanja informativni. Tekst upita se pre pretrage normalizuje: bez dijakritika, bez čestih reči i bez reči kraćih od četiri slova, a preostale reči se skraćuju na prvih pet slova i traže kao prefiks. Posle pretrage, kod zadataka se uz svaki rešen primer dodaje i naredni deo materijala, jer su postavka i rešenje često na susednim stranama.

## 6.4 Prepoznavanje namere

Namera poruke (ćaskanje, pitanje, zadatak) određuje se jednim kratkim pozivom modela sa temperaturom 0 i najviše pet tokena odgovora. Upit sadrži definicije tri kategorije, nekoliko opštih primera i primere koje daje profil predmeta, a nejasan odgovor se tumači kao pitanje iz gradiva (podrazumevano ponašanje):

```csharp
var prompt = $"""
    Klasifikuj poruku studenta u TAČNO jednu kategoriju i odgovori samo jednom rečju.

    CASUAL - pozdrav, zahvalnost, kratak razgovor, pitanje o tebi ili o učenju uopšte.
    SUBJECT - bilo koje stručno ili činjenično pitanje o nekom pojmu.
    TASK - ispitni zadatak ili zahtev da se nešto uradi, reši, napiše ili transformiše.
    Pitanje koje traži da se navede, objasni ili definiše pojam je SUBJECT. TASK je samo kada
    treba izračunati, nacrtati, napisati kod ili rešiti zadatak sa zadatim podacima.
    ...
    {subjectExamples}
    Poruka: "{snippet}"
    Kategorija:
    """;
```

Pravilo „Pitanje koje traži da se navede, objasni ili definiše pojam je SUBJECT“ dodato je posle nalaza evaluacije: pitanje „Navesti tri stavke u upravljačkom bloku niti koje se menjaju pri zameni niti“ razvrstano je kao zadatak, pa je student umesto odgovora dobio plan nagoveštaja. Pravilo je opšte, a ne vezano za to pitanje (isečak gore je skraćen), i posle izmene pitanje se ispravno razvrstava. Ovo je primer za grešku u razvrstavanju namere, koju bi merenje samo tačnosti odgovora previdelo ili pogrešno pripisalo pretrazi.

## 6.5 Odgovor na pitanje

Za pitanje iz gradiva upit modelu sadrži pravila stila (srpski, ekavica, običan tekst bez LaTeX-a i emotikona), uputstvo da odgovara samo na osnovu konteksta, tačnu rečenicu za slučaj da konteksta nema, istoriju razgovora, pronađene delove materijala (svaki skraćen na 2000 znakova) i pitanje. Za višedelna pitanja uputstvo traži odgovor na svaku stavku posebno u obliku „a) Da/Ne – obrazloženje u jednoj rečenici“, a kontekst se sastavlja iz posebnih pretraga za svaku stavku (najviše šest stavki). Temperatura je 0,2, a dužina odgovora ograničena. Ako je najbliži deo dalji od praga predmeta, model se ne poziva uopšte.

## 6.6 Lestvica nagoveštaja

Lestvica je ostvarena u metodi koja prima tekst zadatka i traženi nivo. Stanje (tekst zadatka i dostignuti nivo) čuva klijent i vraća ga pri svakom zahtevu, pa server ostaje bez stanja. Nivo se određuje iz poruke: fraza koja sadrži „rešenje“ vodi na nivo 3, a fraze poput „nagoveštaj“, „sledeći korak“ ili „ne znam“ povećavaju nivo za jedan; razmatraju se samo kratke poruke (do šest reči), da duže pitanje ne bi slučajno bilo pročitano kao zahtev za nagoveštaj.

Redosled odlučivanja za zadatak je sledeći. Najpre se pokušava rešavač predmeta; ako ga zadatak prepoznaje, nivoi 1 i 2 vraćaju unapred napisane nagoveštaje, a nivo 3 izračunato rešenje (odeljak 6.7). Ako ne, traže se rešeni primeri i teorija: za zadatke sa semaforima i monitorima profil predmeta zadaje izvore obrazaca, a inače se uzimaju dva najsličnija primera iz vežbi i rešenih blanketa. Zatim se sastavlja upit koji se razlikuje po nivou, a razlika je samo u uputstvu:

```csharp
var instruction = level switch
{
    1 => "Student traži NAGOVEŠTAJ, a ne rešenje. ... napiši kratak plan rešavanja ovakvog zadatka:
          3 do 5 numerisanih koraka ... NE rešavaj zadatak studenta, ne piši kod, tabele ni
          konačnu gramatiku.",
    2 => "Student je već dobio ovaj plan rešavanja: {plan} ... Razradi rečima SAMO korake 1 i 2
          tog plana ... NE piši kod, tabele ni konačno rešenje.",
    _ => "Zadaci iz ovog predmeta su šablonski. ... Reši zadatak studenta tako što ćeš strukturu
          i oblik rešenja iz tog primera PREPISATI i prilagoditi zadatku ... Ako ne možeš da
          dovršiš zadatak, reši deo koji možeš i reci šta nedostaje.",
};
```

Nivo 2 koristi plan iz prethodnog odgovora, koji se vraća iz istorije razgovora, pa se razrada oslanja na ono što je student već video. Nivo 3 je jedini u kome se traži rešenje, a uputstvo kaže da se ne odbija ako se ne može dovršiti, nego da se reši deo i prizna šta nedostaje. Nivo 1 se završava pozivom studentu da proba sam, i putanjom do najsličnijeg primera; nivo 3 se završava napomenom da je rešenje generisao model, i doslovnim izvodom rešenog primera iz materijala. Najsličniji primer se u upitu stavlja poslednji, odmah pre zadatka, što je odluka dizajna čiji uticaj nije posebno meren.

## 6.7 Rešavači

Rešavači za Operativne sisteme čine oko 1500 linija koda i pokrivaju sledeće tipove zadataka (tabela 6.3). Za svaki tip postoji deterministički algoritam napisan nezavisno od modela, a ispravnost je proverena testovima čija su očekivanja izračunata ručno, na zadacima sa pravih ispita.

Tabela 6.3. Tipovi zadataka koje rešavaju programi

| Tip | Šta se računa |
| --- | --- |
| raspoređivanje procesa | SJF, SRTF, FCFS, prioritet (sa prekidanjem i bez), Round Robin, višenivojski red sa povratnom spregom; ko se izvršava u traženom intervalu, vremena završetka |
| raspoređivanje sa U/I fazama | procesi sa fazama oblika 4-(5)-3: vreme izvršenja i iskorišćenost procesora za multiprogramiranje (FCFS, SRTF) i bez njega |
| planiranje diska | SSTF, SCAN, C-SCAN, LOOK, C-LOOK, FCFS; broj staza diska |
| zamena stranica | FIFO, LRU, OPT; broj grešaka i stanje okvira |
| i-node | broj blokova sa podacima i indeksnih blokova za date veličine fajlova |
| format adrese | broj bita za segment, tabele stranica i pomeraj; veličina adresnog prostora |
| Bankerov algoritam | matrica potreba, bezbedno stanje, odgovor na zahtev |
| `fork()` | broj procesa koje stvara program na jeziku C |

Tok je isti za sve tipove osim poslednjeg. Model dobija tekst zadatka i spisak tipova sa oblikom JSON-a za svaki, pa vraća samo JSON sa tipom i parametrima. Kod ga razlaže, proverava i računa. Poslednji tip, `fork()`, ne koristi model: kod programa se čita direktno iz teksta zadatka, pa je i izdvajanje parametara deterministički.

Provera je ono što rešavač čini pouzdanijim od same ekstrakcije. Primer je raspoređivanje sa U/I fazama, gde zapis faza `4-(5)-3` čita regularni izraz iz teksta zadatka, a od modela se uzimaju samo imena procesa i vremena aktiviranja. Ako se broj procesa koje je model našao ne poklapa sa brojem zapisa faza u tekstu, rešavač odustaje i zadatak ide na pretragu i generisanje:

```csharp
var notations = IoScheduling.PhasesInText(task);            // npr. "4-(5)-3", "2-(4)-1" iz teksta
var jsonProcs = arr.EnumerateArray().ToList();              // procesi koje je izvukao model
if (notations.Count == 0 || notations.Count != jsonProcs.Count) return null;

var procs = new List<IoProc>();
for (var i = 0; i < notations.Count; i++)
    procs.Add(new IoProc(Str(jsonProcs[i], "name")!, Int(jsonProcs[i], "arrival") ?? 0,
                         IoScheduling.ParsePhases(notations[i])));
```

Isto pravilo važi i za raspoređivanje procesa: algoritam koji je model naveo prihvata se samo ako ga tekst zadatka zaista pominje, jer ih model ponekad dodaje nezatražene. Nivo 1 i 2 za ove tipove ne generiše model: nagoveštaji metode napisani su unapred za svaki tip, na primer da procesor tokom U/I faze miruje samo kada se multiprogramiranje ne koristi.

Test koji dokumentuje očekivanje za zadatak sa ispita (proces A sa fazama 4-(5)-3 i aktiviranjem 0, proces B sa fazama 2-(4)-1 i aktiviranjem 1) i čija su očekivanja izračunata ručno:

```csharp
var r = IoScheduling.Simulate("FCFS", Procs);
Assert.Equal("A:0-4 B:4-6 A:9-12 B:12-13", string.Join(" ", r.CpuTimeline.Select(s => $"{s.Name}:{s.Start}-{s.End}")));
Assert.Equal(12, r.Completion["A"]);
Assert.Equal(13, r.Completion["B"]);
Assert.Equal(76.9, r.Utilization, 1);   // 10 ms procesora od ukupno 13 ms
```

Rešavač ne pokriva sve zadatke predmeta. Nisu podržani, na primer, zamena stranica algoritmom Clock nad tabelom stanja (tabela sa ispita se u PDF-u gubi pri čitanju), brojanje pristupa disku pri čitanju i izmeni blokova datoteke (konvencija brojanja mora da se potvrdi sa predmetom), pisanje programa sa nitima ni shell skripti. Takvi zadaci prolaze kroz pretragu i generisanje modela, uz oznaku da je rešenje generisano.

## 6.8 Profili, razgovori i otpremanje materijala

Profil je samo ime, bez lozinke. Tabele profila i poruka se prave i dopunjuju pri pokretanju servera (`CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`), pa nadogradnja ne traži ručnu migraciju postojeće baze. Interfejs je mali (tabela 6.4): profili se prave, preimenuju i brišu (brisanje profila briše i njegove poruke), razgovori se listaju po profilu i predmetu, a materijal se otprema, lista i briše.

Tabela 6.4. Pregled HTTP interfejsa

| Putanja | Namena |
| --- | --- |
| `POST /api/chat` | poruka; vraća odgovor, izvore, nameru, aktivni zadatak i poreklo |
| `GET /api/subjects` | spisak predmeta |
| `GET`, `POST /api/profiles`; `PUT`, `DELETE /api/profiles/{id}` | profili |
| `GET /api/profiles/{id}/conversations`, `GET`, `DELETE /api/profiles/{id}/messages` | razgovori i poruke |
| `GET`, `DELETE /api/materials` | spisak i brisanje materijala |
| `POST /api/materials/upload`, `GET /api/materials/jobs/{id}` | otpremanje i praćenje obrade |
| `POST /api/ingest` | unos materijala iz foldera na serveru (za pripremu baze znanja) |

Otpremanje je asinhrono jer računanje vektora traje: server primi fajl (najviše 50 MB, dozvoljeni formati PDF, TXT, MD, C, H, JAVA i SH), upiše ga u privremeni folder, pokrene obradu u pozadini i odmah vrati identifikator posla. Klijent svake sekunde pita za stanje (u obradi, završeno, greška) i napredak. Ponovno otpremanje fajla istog naziva zamenjuje prethodnu verziju, a ako obrada ne uspe, delimično upisani delovi se uklanjaju, tako da u bazi ne ostaje nepotpun materijal. Stanje poslova je u memoriji servera i gubi se pri ponovnom pokretanju; obrada koja je u toku tada nije nastavljena, što je prihvatljivo za alat sa jednim korisnikom.

## 6.9 Klijent

Klijent je jedna React stranica sa bočnom trakom i prostorom za razgovor (slike 6.1 do 6.3). Bočna traka sadrži profil, izbor predmeta, razgovore izabranog predmeta, panel „Moj materijal“ za otpremanje i brisanje fajlova (i prevlačenjem) i podešavanje „Napredno“ za izbor režima odgovora. Stranica profila omogućava preimenovanje, izvoz razgovora u tekst i brisanje.

Dve odluke vrede pomena. Prvo, uz svaki odgovor prikazuje se poreklo (izračunato programski, unapred napisan nagoveštaj ili generisao model) i izvori; izvori se svode na tip i naziv materijala bez ponavljanja, jer je udaljenost iz pretrage studentu bez značenja. Drugo, tekst odgovora prikazuje se bez ugrađenog HTML-a: prepoznaju se samo blokovi koda, liste, podebljan tekst i kod u tekstu, i prikazuju kao React elementi, pa tekst koji vrati model ne može da ubaci markup u stranicu.

![Slika 6.1. Odgovor modela sa izvorima](figures/preview-razgovor-izvori.jpg){width=90%}

![Slika 6.2. Rešenje izračunato programski, sa oznakom porekla](figures/preview-izracunato.jpg){width=90%}

![Slika 6.3. Stranica profila](figures/preview-profil.jpg){width=90%}

Raspored je namerno prilagođen računaru: ne postoji prikaz za telefon, jer se aplikacija koristi na računaru na kome radi i Ollama. Klijent pamti samo izabrani profil, predmet i režim u lokalnom skladištu pregledača; sve ostalo je na serveru.

## 6.10 Testovi i ograničenja

Server ima 159 jediničnih testova (u 13 fajlova sa testovima i pomoćnim klasama) koji se izvršavaju bez Ollame i baze, uz lažne implementacije pretrage, modela za generisanje i modela za vektore. Testovi proveravaju sledeće: rešavače na zadacima sa ispita (očekivanja izračunata ručno), izvlačenje parametara iz JSON-a, deljenje materijala, razvrstavanje namere, nivoe lestvice nagoveštaja, pretragu po ključnim rečima, registar predmeta i unos fajla. Ponašanje celog sistema sa pravim modelom proverava se skriptama u `eval/` (poglavlje 7), jer jedinični testovi ne mogu da kažu da li model dobro odgovara.

Poznata ograničenja implementacije su sledeća:

- Lestvica ne proverava da li je student pokušao sam (odeljak 5.5).
- Rešavači pokrivaju samo navedene tipove zadataka za predmet Operativni sistemi; Programski prevodioci nemaju rešavač.
- Profil nema lozinku, a razgovori su u bazi u čistom tekstu.
- Na procesoru bez grafičke kartice odgovor traje od nekoliko sekundi do više od minuta, zavisno od vrste zadatka (poglavlje 7).
- Stanje otpremanja materijala čuva se u memoriji servera.
