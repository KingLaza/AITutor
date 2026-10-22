# 4. Tehnologije

Ovo poglavlje opisuje tehnologije na kojima se AITutor zasniva i razloge za svaki izbor. Odeljak 4.1 razmatra lokalno i udaljeno izvršavanje jezičkih modela, odeljak 4.2 modele za vektorsko predstavljanje teksta (engl. *embedding*), a odeljak 4.3 generisanje podržano pretragom i hibridnu pretragu. Odeljak 4.4 poredi skladišta vektora, a odeljak 4.5 sažeto navodi ostale korišćene tehnologije. Gde se izbor oslanja na merenje, navedeno je koji fajl iz repozitorijuma sadrži rezultat; gde se oslanja na dokumentaciju, to je izričito rečeno.

## 4.1 Lokalni i udaljeni jezički modeli

Veliki jezički modeli se u obrazovnim sistemima koriste na dva načina. Prvi je pozivanje komercijalnog modela u oblaku preko programskog interfejsa, što rade, na primer, Jill Watson sa modelom gpt-3.5-turbo [@taneja2024] i studije sa GPT-4 [@bastani2025; @kestin2025]. Drugi je pokretanje otvorenog modela na sopstvenom računaru, kako rade Katharakis i saradnici [@katharakis2025] sa osam modela veličine od 7 do 17 milijardi parametara i Reza i saradnici [@reza2025] sa modelima od 3 do 7 milijardi parametara. Razlike među pristupima prikazane su u tabeli 4.1.

Tabela 4.1. Lokalno i udaljeno izvršavanje jezičkog modela

| Svojstvo | Model u oblaku | Lokalni model |
| --- | --- | --- |
| Privatnost | tekst pitanja i delovi materijala napuštaju računar | ostaje na računaru |
| Cena | po upotrebi, uz zavisnost od cenovnika davaoca | jednokratna (hardver), bez cene po upitu |
| Kvalitet odgovora | obično viši kod velikih modela | niži kod malih modela, pa je pretraga i provera važnija |
| Kašnjenje | zavisi od mreže i opterećenja davaoca | zavisi od procesora računara; bez grafičke kartice znatno sporije |
| Dostupnost | traži internet | radi bez interneta |
| Ponašanje modela | davalac ga menja bez najave | stalno, dok korisnik ne zameni model |

Za AITutor je odlučujuće svojstvo privatnosti i rada bez interneta: sistem je zamišljen kao alat koji student pokreće na sopstvenom računaru, sa sopstvenim materijalom, bez naloga i bez slanja podataka trećoj strani. Cena te odluke je kvalitet: mali model greši češće od velikog, što je jedan od razloga za oslanjanje na pretragu i na deterministički proračun (poglavlje 5). Prednost lokalnog izvršavanja u pogledu privatnosti navode i radovi koji su ga koristili [@katharakis2025; @reza2025], ali je ovde ona posledica dizajna, a ne rezultat merenja. Poslednja stavka tabele je značajna za ponovljivost: ponašanje modela u oblaku menja se tokom vremena, zbog čega poređenje u poglavlju 7 mora biti vezano za datum, dok se lokalni model može zamrznuti i tačno opisati.

Za pokretanje modela koristi se Ollama [@ollama], program koji lokalno poslužuje preuzete modele preko HTTP interfejsa na računaru. Aplikacija sa Ollamom komunicira dvema operacijama: generisanje teksta i izračunavanje vektora. Za generisanje je izabran model gemma4:e4b, koji Ollama prijavljuje kao model od oko 7,5 milijardi parametara sa kvantizacijom Q4_K_M, a veličina kontekstnog prozora podešena je na 6144 tokena. Kvantizacija smanjuje preciznost brojeva u modelu, pa model zauzima manje memorije i radi brže, uz izvestan gubitak kvaliteta. Sistem je projektovan za računar bez grafičke kartice, pa se model izvršava na procesoru (merenja resursa su predmet poglavlja 7). Dva podešavanja su se u radu pokazala važnim. Prvo, Gemma 4 po podrazumevanim podešavanjima ispisuje dugačko „razmišljanje“ pre odgovora, što na procesoru znatno produžava vreme odgovora, pa je ono isključeno parametrom `think`. Drugo, ako se veličina kontekstnog prozora razlikuje između dva poziva, Ollama ponovo učitava model u memoriju, zbog čega svi pozivi koriste istu vrednost.

Modeli za generisanje i za vektorsko predstavljanje su odvojeni i biraju se nezavisno. Zamena modela za generisanje ne zahteva ništa osim promene podešavanja, dok zamena modela za vektorsko predstavljanje zahteva ponovno izračunavanje vektora za ceo materijal (odeljak 4.2).

## 4.2 Modeli za vektorsko predstavljanje teksta

Pretraga po značenju zasniva se na modelu koji tekst pretvara u vektor tako da su sličnim tekstovima vektori blizu. Pitanje i delovi materijala se predstavljaju u istom prostoru, pa su najbliži delovi materijala kandidati za odgovor. Razmatrana su tri otvorena modela dostupna preko Ollame.

- **nomic-embed-text** [@nussbaum2024] je model sa kontekstom od 8192 tokena čiji autori opisuju kao otvoreno objavljen model za engleski jezik.
- **bge-m3** [@chen2024] je višejezični model koji podržava više od 100 jezika i kontekst do 8192 tokena.
- **embeddinggemma** [@embeddinggemma2025] je lagan model od 300 miliona parametara, namenjen radu na samom uređaju.

Modeli su upoređeni na skupu od 27 pitanja iz predmeta Programski prevodioci, za koje su unapred određeni fajlovi u kojima treba da se nađe odgovor, i 19 pitanja van predmeta (skripta `eval/embedding_eval.py`, izlaz u `eval/results/embedding_eval.txt`). Mere se pogodak na prvom mestu (hit@1), pogodak među prva četiri (hit@4) i srednji recipročni rang (MRR), a za razdvajanje pitanja iz predmeta od pitanja van njega površina ispod ROC krive (AUC) po razdaljini najbližeg dela materijala. Rezultati su prikazani u tabeli 4.2.

Tabela 4.2. Poređenje modela za vektorsko predstavljanje (27 pitanja iz predmeta, 19 van predmeta; `eval/results/embedding_eval.txt`)

| Model | hit@1 | hit@4 | MRR | AUC | Uravnotežena tačnost |
| --- | --- | --- | --- | --- | --- |
| nomic-embed-text | 0,63 | 0,85 | 0,73 | 0,86 | 0,82 |
| embeddinggemma | 0,93 | 0,96 | 0,95 | 0,99 | 0,94 |
| bge-m3 | 0,85 | 0,96 | 0,91 | 0,97 | 0,92 |

Najslabije rezultate daje model nomic-embed-text, što je u skladu sa tim da je namenjen engleskom jeziku, ali taj razlog ovde nije proveren posebnim eksperimentom. Najbolji je embeddinggemma i on je izabran. Skup je mali (27 pitanja), pa razlike između embeddinggemma i bge-m3 treba čitati oprezno; u poglavlju 7 skup se proširuje na oko 50 označenih pitanja po predmetu.

Dva praktična detalja utiču na kvalitet. Prvo, embeddinggemma očekuje različit oblik teksta za upit i za dokument (prefiksi `task: search result | query:` odnosno `title: none | text:`), pa ih aplikacija dodaje pri svakom izračunavanju vektora. Drugo, mera sličnosti je kosinusna razdaljina: 0 znači istovetan smer vektora, a veće vrednosti manju sličnost. Raspodela razdaljina zavisi od modela i materijala, zbog čega je prag iznad kog se pitanje odbija različit po predmetu (poglavlje 5), a ne jedna opšta vrednost.

## 4.3 Generisanje podržano pretragom i hibridna pretraga

Generisanje podržano pretragom (engl. *retrieval-augmented generation*, RAG) uvedeno je u radu Lewisa i saradnika [@lewis2020]: pre generisanja se iz zadatog korpusa pronalaze relevantni delovi teksta i prosleđuju modelu uz pitanje. U AITutor-u materijal predmeta se unapred deli na delove (najčešće po jedna strana predavanja ili vežbe), svaki deo se pretvara u vektor i čuva u bazi. Pri pitanju se vektor pitanja poredi sa vektorima delova, a najbliži delovi ulaze u upit modelu uz uputstvo da odgovara samo na osnovu njih. Ako ni najbliži deo nije dovoljno blizu, model se uopšte ne poziva, nego sistem odgovara da nema dovoljno informacija (poglavlje 6).

Samo vektorska pretraga ima slabu tačku: pojmovi koji se u materijalu javljaju kao tačne oznake, poput `LL(1)`, `FIRST`, `cwait` ili `fork()`, mogu da budu predstavljeni slično sa srodnim pojmovima. Zato AITutor uz vektorsku koristi i pretragu po ključnim rečima, ugrađenu u PostgreSQL (funkcije `to_tsvector` i `ts_rank`). Dva spiska rezultata, jedan po razdaljini vektora i jedan po oceni ključnih reči, spajaju se postupkom recipročnog spajanja rangova (engl. *reciprocal rank fusion*, RRF) koji su predložili Cormack, Clarke i Büttcher [@cormack2009]. Ocena dela dokumenta $d$ je

$$\mathrm{RRF}(d) = \sum_{i} \frac{1}{k + r_i(d)},$$

gde je $r_i(d)$ rang dela $d$ u $i$-tom spisku, a $k$ konstanta (u originalnom radu 60, što AITutor koristi). Prednost postupka je što se spajaju rangovi, a ne ocene, pa nije potrebno uskladiti merne skale razdaljine i ocene ključnih reči. Isti pristup, vektorsku pretragu uz pretragu teksta u PostgreSQL-u, uz spajanje postupkom RRF, navodi kao mogućnost i dokumentacija ekstenzije pgvector [@pgvector].

Dva prilagođavanja su bila potrebna za srpski jezik. Pošto je materijal na srpskom, a korisnici često pišu bez dijakritičkih znakova, i tekst i upit se pre poređenja svode na slova bez dijakritika. Pošto srpski ima bogat sistem nastavaka, a jednostavna konfiguracija teksta u PostgreSQL-u ne poznaje njihovu morfologiju, reči iz upita se skraćuju na prvih pet slova i traže kao prefiks. Obe izmene su jednostavne i grube; njihov uticaj na kvalitet pretrage meri se u poglavlju 7.

## 4.4 Skladišta vektora

Pronalaženje najbližih vektora u velikom skupu skupo je ako se svaki vektor poredi sa upitom, pa se koristi aproksimativna pretraga najbližih suseda. Najrasprostranjeniji postupak je HNSW (engl. *hierarchical navigable small world*), koji vektore organizuje u višeslojni graf kroz koji se upit brzo približava susedima [@malkov2016]. Dokumentacija ekstenzije pgvector navodi dve vrste indeksa: HNSW, koji ima bolje performanse upita ali se duže gradi i zauzima više memorije, i IVFFlat, koji se brže gradi i zauzima manje memorije, ali ima slabije performanse upita [@pgvector].

Za čuvanje vektora razmatrana su četiri sistema: ekstenzija pgvector za PostgreSQL, namenska baza Qdrant, upravljana usluga Pinecone i baza Chroma. Poređenje je zasnovano isključivo na zvaničnoj dokumentaciji, bez sopstvenog eksperimenta, i prikazuje ono što dokumentacija navodi na stranicama pregledanim 22. oktobra 2026. godine. Gde stranica nešto ne navodi, tako je i zapisano; to ne znači da sistem tu mogućnost nema.

Tabela 4.3. Poređenje skladišta vektora prema zvaničnoj dokumentaciji

| Svojstvo | pgvector [@pgvector] | Qdrant [@qdrantdocs] | Pinecone [@pineconedocs] | Chroma [@chromadocs] |
| --- | --- | --- | --- | --- |
| Vrsta | ekstenzija PostgreSQL-a | namenska baza vektora | usluga za pretragu (dokumentacija ne opisuje način izvršavanja) | baza otvorenog koda za AI aplikacije |
| Izvršavanje | u okviru PostgreSQL-a (Docker slike za verzije 13 do 18) | samostalno, upravljani oblak, hibridni i privatni oblak | upravljana usluga; opcija sopstvenog oblaka (BYOC); lokalno nije navedeno | lokalno, samostalno ili upravljani oblak |
| Indeks | HNSW i IVFFlat | HNSW | nije navedeno | nije navedeno |
| Filtriranje | SQL (`WHERE`) | filtri po sadržaju (engl. *payload*) tokom pretrage | filtri po metapodacima | filtri po metapodacima |
| Hibridna pretraga | uz pretragu teksta u PostgreSQL-u, spajanje npr. postupkom RRF | gusti i retki vektori, BM25 | gusti i retki vektori, tekst | gusti i retki vektori, pretraga po ključnim rečima i regularnim izrazima |
| Relacioni podaci, transakcije | da (ACID, JOIN) | nije navedeno | nije navedeno | nije navedeno |

Za AITutor je izabran pgvector iz razloga koji proizlaze iz zahteva sistema, a ne iz merenja brzine. Prvo, sistem treba da radi lokalno sa što manje komponenata: pgvector je deo PostgreSQL-a, pa je jedan kontejner baze dovoljan, bez dodatne usluge. Drugo, aplikaciji su pored vektora potrebni i obični relacioni podaci (profili, razgovori, poruke), a pgvector dokumentacija navodi da uz vektore važe i svojstva PostgreSQL-a: ACID transakcije i spajanje tabela (JOIN) [@pgvector]. Treće, pretraga po ključnim rečima koja je potrebna za hibridnu pretragu (odeljak 4.3) ugrađena je u isti sistem, pa se oba spiska dobijaju jednim upitom. Četvrto, indeksiranje običnim tipom `vector` ograničeno je na 2000 dimenzija, što je dovoljno za 768 dimenzija modela embeddinggemma [@pgvector]. Pinecone, čija dokumentacija ne navodi lokalno izvršavanje [@pineconedocs], ne uklapa se u zahtev da podaci ostaju na računaru studenta.

Ograničenje ovog izbora treba reći otvoreno. Materijal za dva predmeta ima nešto više od dve hiljade delova, a pri toj veličini praktično svako od razmatranih skladišta bi bilo dovoljno brzo; odluka nije zasnovana na merenju performansi i ne tvrdi se da bi pgvector bio najbolji izbor za korpus od više miliona vektora. Sistemi poput Qdranta ili Chroma bi bili razumne alternative uz dodatnu komponentu za relacione podatke.

## 4.5 Ostale tehnologije

Serverski deo je napisan u jeziku C# na platformi .NET 8 (ASP.NET Core), a klijent u jeziku TypeScript uz biblioteku React i alat za izgradnju Vite. Baza je PostgreSQL 18 sa ekstenzijom pgvector u izdanju 0.8.6, a komponente se pokreću zajedno pomoću alata Docker Compose, uz web server nginx koji poslužuje klijenta i prosleđuje pozive serveru. Izbor jezika i biblioteka rukovodio se prvenstveno poznavanjem alata od strane autora i dostupnošću biblioteka (na primer PdfPig za čitanje PDF fajlova i Npgsql za pristup bazi); poređenje alternativa u ovoj oblasti nije predmet rada.
