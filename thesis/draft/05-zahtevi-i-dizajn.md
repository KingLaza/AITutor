# 5. Zahtevi i dizajn

Ovo poglavlje izvodi zahteve za sistem iz pregleda u poglavljima 2 i 3, a zatim opisuje dizajn koji ih ispunjava. Odeljak 5.1 povezuje svaki zahtev sa izvorom iz literature i sa rešenjem u sistemu. Odeljci 5.2 i 5.3 prikazuju arhitekturu i tok jednog zahteva, odeljak 5.4 pretragu i odbijanje pitanja, odeljak 5.5 lestvicu nagoveštaja, a odeljak 5.6 podelu na predmete. Odeljak 5.7 opisuje granicu između onoga što izračunava program i onoga što generiše model, odeljak 5.8 podatke, a odeljak 5.9 privatnost i isporuku.

## 5.1 Zahtevi

Zahtevi u tabeli 5.1 proizlaze iz zaključaka poglavlja 2 i 3, a dva su praktična i potiču iz same teme rada (Z9 i Z10). Treća kolona kaže gde se zahtev ostvaruje; zahtev Z5 je ostvaren samo delimično, što je navedeno u odeljku 5.5.

Tabela 5.1. Zahtevi, njihov izvor i rešenje u sistemu

| Oznaka | Zahtev | Izvor | Rešenje |
| --- | --- | --- | --- |
| Z1 | Podaci studenta ostaju na njegovom računaru | odeljak 3.5 (privatnost) | lokalni model (Ollama), lokalna baza |
| Z2 | Sistem radi na običnom računaru bez grafičke kartice | odeljak 3.3 (mali lokalni modeli) | mali kvantizovani model, rad na procesoru |
| Z3 | Odgovor navodi izvor, a sistem ume da kaže „ne znam“ | odeljak 3.5 (odstupanje od izvora) | RAG, prag odbijanja, prikaz izvora |
| Z4 | Povratna informacija po koracima, ne samo konačan odgovor | odeljci 2.2 i 2.3 | lestvica nagoveštaja |
| Z5 | Viši nivo pomoći se otključava zahtevom studenta, a lestvicu je moguće preskočiti | odeljci 2.3 i 2.4 | dugmad i fraze za sledeći nagoveštaj i za rešenje |
| Z6 | Konačno rešenje nije podrazumevani odgovor | odeljak 2.5 | zadatak počinje nivoom 1 (plan) |
| Z7 | Tačan proračun za šablonske zadatke | odeljak 3.5 (računanje) | deterministički rešavači |
| Z8 | Poreklo odgovora je vidljivo (izračunato ili generisano) | odeljak 3.6 | oznaka porekla uz odgovor |
| Z9 | Više predmeta, jezgro opšte, posebnosti predmeta odvojene | tema rada | profil predmeta (`ISubjectProfile`) |
| Z10 | Student može da doda sopstveni materijal | tema rada | otpremanje fajlova (izvor `moje`) |
| Z11 | Ponašanje se može meriti i upoređivati | odeljak 3.5 (ocenjivanje) | režimi odgovora (`Rag`, `RagNoTools`, `Plain`) |

## 5.2 Arhitektura

Sistem se sastoji od četiri komponente (slika 5.1): web klijent koji poslužuje nginx, API napisan u ASP.NET Core, baza PostgreSQL sa ekstenzijom pgvector i Ollama koja izvršava jezičke modele. Prve tri komponente se pokreću u okviru Docker Compose-a, dok Ollama podrazumevano radi na samom računaru, pa može da koristi sve njegove resurse; opcioni profil omogućava da i ona radi u kontejneru. API je jedina komponenta koja komunicira sa bazom i sa Ollamom, a klijent komunicira samo sa API-jem preko nginx-a.

![Slika 5.1. Arhitektura sistema](figures/arhitektura.png){width=90%}

Serverski kod je podeljen na biblioteku `AITutor.Core` i API. U biblioteci su pretraga i odgovaranje (`RagQueryService`), pristup bazi, klijenti za Ollamu, unos materijala i moduli predmeta, a API sadrži samo kontrolere (razgovor, predmeti, profili, materijal) i podešavanje zavisnosti. Odvajanje biblioteke omogućava da se logika proverava bez mreže i baze: pretraga, model za generisanje i model za vektore zamenjuju se lažnim implementacijama, pa se 159 jediničnih testova izvršava za manje od jedne sekunde.

## 5.3 Tok jednog zahteva

Slika 5.2 prikazuje šta se dešava sa porukom studenta. Ako student radi na zadatku i poruka je zahtev za nagoveštaj ili rešenje („sledeći nagoveštaj“, „prikaži rešenje“, „ne znam“), ide se direktno na lestvicu nagoveštaja. U suprotnom jezički model razvrstava poruku u jednu od tri namere: ćaskanje (pozdrav, zahvalnost), pitanje iz gradiva ili zadatak. Za kratke pozdrave i zahvalnice razvrstavanje se preskače jer ih prepoznaje spisak reči, što štedi jedan poziv modela.

![Slika 5.2. Tok obrade poruke](figures/tok-zahteva.png){width=70%}

Ćaskanje dobija odgovor modela bez materijala. Pitanje iz gradiva ide kroz pretragu (odeljak 5.4), a zadatak kroz lestvicu nagoveštaja (odeljak 5.5). Sistem pamti kratku istoriju razgovora: poslednja četiri obrta ulaze u upit, a kratka potpitanja (do sedam reči, poput „a zašto?“) se najpre prepisuju u samostalno pitanje na osnovu istorije, da bi pretraga imala šta da traži. Pitanja sa više stavki (a, b, c...), kakva su česta na ispitima u obliku „Da/Ne + obrazloženje“, razlažu se na stavke, a svaka stavka dobija sopstvenu pretragu, jer pretraga celog pitanja često promaši pojedine stavke.

## 5.4 Pretraga i odbijanje pitanja

Za pitanja iz gradiva pretražuju se samo izvori koji sadrže znanje: predavanja, vežbe, rešeni blanketi, pitanja za usmeni i materijal koji je student sam dodao. Ispiti sa postavkama, ali bez rešenja, ne koriste se kao izvor znanja. Pretraga je hibridna (odeljak 4.3), ograničena na izabrani predmet, i vraća šest najboljih delova.

Pre poziva modela proverava se koliko je blizu najbliži deo materijala. Svaki predmet ima sopstveni prag razdaljine: ako je i najbliži deo dalji od praga, sistem odmah odgovara da nema dovoljno informacija, bez poziva modela. Prag se određuje merenjem, a ne pretpostavkom. Za Programske prevodioce najdalje pitanje iz predmeta je na razdaljini 0,535, a najbliže pitanje van predmeta na 0,467 (`eval/results/embedding_eval.txt`), pa se te dve grupe preklapaju i nijedan prag ih ne razdvaja savršeno; izabrano je 0,56. Za Operativne sisteme najdalje pitanje iz predmeta je na 0,578, a najbliže van predmeta na 0,496 (`eval/results/threshold_eval_operativni_sistemi.txt`), uz površinu ispod ROC krive 0,97. Prag u sistemu je 0,64, dakle iznad najdaljeg izmerenog pitanja iz predmeta, pa pitanja iz predmeta ne odbija program nego eventualno model. Pitanja koja prođu prag, ali su u sivoj zoni, prepuštaju se modelu: u upitu stoji da odgovori tačnom rečenicom „Nemam dovoljno informacija u materijalu“ ako kontekst ne sadrži odgovor. Rezultat je dvostepeno odbijanje, jeftino kada je pitanje očigledno van predmeta i oprezno kada nije.

## 5.5 Lestvica nagoveštaja

Zadatak, za razliku od pitanja, ne dobija odmah rešenje. Lestvica ima tri nivoa (slika 5.3): plan rešavanja u 3 do 5 koraka (nivo 1), razrada prva dva koraka plana (nivo 2) i kompletno rešenje (nivo 3). Student prelazi na sledeći nivo izričitim zahtevom, a može i da preskoči lestvicu zahtevom za rešenje u bilo kom trenutku. U klijentu to su dva dugmeta, „Sledeći nagoveštaj“ i „Prikaži rešenje“, a prepoznaju se i kratke fraze. Time je ispunjen zahtev da lestvica bude preskočiva (Z5), što prema odeljku 2.4 opravdava efekat obrnute stručnosti.

![Slika 5.3. Lestvica nagoveštaja](figures/lestvica.png){width=90%}

Prvi nivo se oslanja na najsličniji rešen primer iz materijala (vežbe ili rešeni blanketi) i na odgovarajuću teoriju iz predavanja; uz plan se ispisuje i koji je to primer, tako da student zna gde da potraži. Time se nagoveštaj vezuje za konkretan deo materijala, što je prema radu Xiaoa i saradnika [@xiao2024] uslov da apstraktan nagoveštaj bude upotrebljiv (odeljak 3.3). Drugi nivo razrađuje samo korake 1 i 2 prethodno datog plana, a model je izričito upućen da ne piše kod, tabele ni konačno rešenje. Treći nivo daje pokušaj rešenja uz napomenu da ga je generisao model, a iza njega se ispisuje doslovan rešen primer iz materijala, koji je pouzdan za razliku od izlaza modela.

Granica ovog dizajna treba da bude jasna. Student koji traži nagoveštaj dobija ga, ali sistem ne proverava da li je pokušao sam; pokušaj kao uslov za otključavanje nivoa, koji pominje odeljak 2.3, nije ostvaren. Zahtev Z5 je zato ostvaren samo u delu koji se tiče izričitog zahteva i mogućnosti preskakanja. Takođe, ne tvrdi se da lestvica poboljšava učenje; u poglavlju 7 meri se samo da li nagoveštaji otkrivaju rešenje i da li vode dalje.

## 5.6 Predmeti kao moduli

Jezgro sistema (pretraga, lestvica nagoveštaja, istorija) ne zna ništa o pojedinom predmetu. Sve što je specifično za predmet nalazi se iza interfejsa `ISubjectProfile` (slika 5.4): naziv, prag razdaljine, opcioni rešavač zadataka, opcione smernice za određenu vrstu zadatka i primeri za razvrstavanje namere. Registar predmeta pronalazi profil po nazivu, a za predmet bez profila (na primer, kada je samo ubačen materijal) koristi opšti profil sa podrazumevanim ponašanjem.

![Slika 5.4. Moduli predmeta](figures/moduli.png){width=95%}

Programski prevodioci imaju profil samo sa sopstvenim pragom i primerima za razvrstavanje. Operativni sistemi dodaju tri stvari: rešavače (odeljak 5.7), smernice za zadatke sa semaforima i monitorima (notacija iz predavanja i izbor proverenih obrazaca) i sopstveni prag. Smernice postoje jer se takvi zadaci na ispitu pišu u pseudo-kodu iz predavanja, dok su laboratorijske vežbe pisane u POSIX jeziku C, pa bi model bez uputstva prepisivao pogrešnu notaciju. Novi predmet se dodaje implementacijom interfejsa i registracijom u konfiguraciji; materijal se ubacuje bez izmene koda.

## 5.7 Granica između izračunatog i generisanog

Mali jezički model često greši u računanju, a za šablonske zadatke (raspoređivanje procesa, planiranje diska, zamena stranica, veličina i-nodea, format adrese, Bankerov algoritam, broj procesa pri pozivu `fork()`) tačno rešenje se može dobiti običnim programom. Zato je za takve zadatke uloga modela sužena: on samo prepoznaje tip zadatka i izvlači parametre iz teksta u strukturisan oblik (JSON), a računa deterministički kod. Isti princip, model razume zadatak a program računa, opisuje se i u radu CoTu [@tran2026]. Razlika je što su rešavači ovde ručno napisani za ponavljajuće šablone predmeta, što je robusnije, ali uže.

Pravilo je sledeće. Ako rešavač prepozna tip zadatka i parametri prođu proveru, nivoi 1 i 2 dobijaju unapred napisane nagoveštaje metode za taj tip zadatka (model ih ne generiše, pa ne može da zavede), a nivo 3 dobija rešenje izračunato programom. Provera parametara je ono što štiti od grešaka modela pri izvlačenju: na primer, algoritam raspoređivanja se prihvata samo ako ga zadatak zaista pominje, a faze procesa sa ulazno-izlaznim operacijama čitaju se direktno iz teksta zadatka, ne iz izlaza modela (poglavlje 6). Ako rešavač ne prepozna zadatak ili provera ne uspe, sistem se vraća na pretragu rešenih primera i generisanje modelom, uz izričitu napomenu da je rešenje generisano.

Poreklo svakog odgovora se čuva i prikazuje u klijentu (Z8): izračunato programski, unapred napisan nagoveštaj ili generisao model. Odbijanje i ćaskanje nemaju oznaku. Isti odgovor tako govori studentu koliko mu može verovati, a istovremeno omogućava da se u evaluaciji odvoje rezultati alata od rezultata modela. Za tu svrhu postoje tri režima odgovora (Z11): ceo sistem, pretraga bez rešavača (`RagNoTools`) i model bez pretrage (`Plain`).

## 5.8 Podaci

Baza ima tri tabele (slika 5.5). Tabela `chunks` čuva delove materijala sa predmetom, tipom izvora, nazivom fajla, tekstom i vektorom od 768 dimenzija. Tabele `profiles` i `messages` čuvaju lokalne profile (samo ime, bez lozinke) i razgovore: svaka poruka pripada profilu, predmetu i razgovoru, a uz odgovor se čuvaju izvori, aktivni zadatak i poreklo.

![Slika 5.5. Model podataka](figures/baza.png){width=85%}

Tip izvora određuje ulogu dela materijala (tabela 5.2). Tipovi `predavanja`, `vezbe`, `reseni_blanketi` i `usmeni` čine znanje iz kojeg se odgovara, `pismeni` su ispiti čuvani radi evaluacije, `obrasci` su proveren rešeni primeri koje je dodao autor (za zadatke sa semaforima i monitorima), a `moje` je materijal koji je student sam dodao. Materijal koji je dodao autor označen je u samom tekstu kao dodatak, a ne kao materijal kursa.

Tabela 5.2. Tipovi izvora materijala

| Tip izvora | Šta sadrži | Koristi se za |
| --- | --- | --- |
| `predavanja` | slajdovi predavanja | odgovore na pitanja i teoriju uz zadatke |
| `vezbe` | materijal sa vežbi i laboratorijskih vežbi | pitanja, rešeni primeri uz zadatke |
| `reseni_blanketi` | rešeni ispitni zadaci | pitanja, rešeni primeri uz zadatke |
| `usmeni` | pitanja i kratke činjenice za usmeni deo | pitanja |
| `obrasci` | provereni obrasci rešenja (dodatak autora) | primeri za zadatke sa semaforima i monitorima |
| `pismeni` | ispiti bez rešenja | samo evaluacija, ne i odgovori |
| `moje` | materijal koji student dodaje | pitanja i teorija uz zadatke |

## 5.9 Privatnost i isporuka

Pri normalnom radu nijedan podatak ne napušta računar: pitanja idu lokalnoj Ollami, a materijal i razgovori ostaju u lokalnoj bazi. Preuzimaju se samo modeli i slike pri instalaciji. Profil nema lozinku, pa svako ko ima pristup instanci vidi sve profile, što je u skladu sa namenom (instanca je lična ili za malu grupu na istom računaru). Materijal koji student doda zajednički je za instancu, a ne za profil, iz istog razloga. Potpun model pretnji, uključujući šta bi napustilo računar ako bi se kasnije dodao oblak, dat je u prilogu.

Isporuka je jedan `docker compose up`: kontejneri za bazu, API i web klijent, uz dokumentovan način za preuzimanje modela u Ollamu. Materijal kursa nije deo javnog repozitorijuma zbog autorskih prava. Zato postoje dve mogućnosti: student sam ubaci materijal (otpremanjem u aplikaciji ili komandom za unos), ili dobije izvezenu bazu znanja koja se pri prvom pokretanju učita u praznu bazu. Takva baza sadrži tekst materijala predmeta, pa se deli samo uz dozvolu autora materijala.
