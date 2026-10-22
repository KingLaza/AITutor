# 2. Učenje uz tutora

Ovo poglavlje daje pedagoške i istorijske osnove na kojima se gradi AITutor. Najpre se razmatra šta se zaista zna o efikasnosti individualnog tutoringa (odeljak 2.1), zatim kako rade klasični inteligentni tutorski sistemi i šta su njihovi dokazi i ograničenja (2.2). Odeljci 2.3 i 2.4 opisuju pedagoške principe koji opravdavaju lestvicu nagoveštaja: skelu, postepeno davanje pomoći, produktivnu borbu i kognitivno opterećenje. Odeljak 2.5 se bavi rizikom da alat učenika oslobodi napora koji je potreban da bi učio, a odeljak 2.6 sažima šta iz svega toga sledi za dizajn sistema.

## 2.1 Individualni tutoring i „problem dve sigme“

Polazna tačka gotovo svake rasprave o tutorskim sistemima je rad Benjamina Blooma iz 1984. godine [@bloom1984]. Bloom je poredio tri uslova učenja: uobičajenu nastavu u odeljenju, nastavu zasnovanu na ovladavanju gradivom (engl. *mastery learning*) i individualni tutoring uz isti princip ovladavanja. Prosečan učenik u poslednjem uslovu postigao je rezultat otprilike dve standardne devijacije iznad prosečnog učenika u odeljenju. Bloom je to nazvao „problemom dve sigme“: kako grupnu nastavu učiniti jednako delotvornom kao tutoring jedan na jedan, kada je takav tutoring preskup da bi bio dostupan svima.

Taj nalaz se danas ocenjuje znatno strože. Prema pregledu P. von Hippela [@vonhippel2024], Bloomov zaključak oslanjao se uglavnom na dve disertacije u kojima su učenici četvrtog, petog i osmog razreda tri nedelje učili verovatnoću, odnosno kartografiju. Efekat je meren uskim testovima na kraju jedinice, a tutoring je bio povezan sa dodatnim proverama znanja, povratnim informacijama, obukom tutora i dodatnim vremenom za nastavu. Kasnije metaanalize daju znatno manje vrednosti. Von Hippel navodi da je prosečan efekat tutoringa iznosio oko 0,33 standardne devijacije u metaanalizi Cohena, Kulika i Kulika iz 1982. godine, a 0,37 u metaanalizi 96 studija koju su 2020. godine objavili Nickow, Oreopoulos i Quan, pri čemu nijedna od tih studija nije dostigla dve standardne devijacije [@vonhippel2024]. On zaključuje da je oko trećine standardne devijacije tipičan efekat intenzivnog, dobro osmišljenog programa tutoringa meren na širokim testovima, i da je to realističan početni cilj i za tutore zasnovane na veštačkoj inteligenciji [@vonhippel2024].

Za ovaj rad iz toga slede dva zaključka. Prvo, cifra od dve sigme ne koristi se kao cilj, niti se tvrdi da AITutor može da je dostigne. Drugo, razlog za izgradnju sistema je dostupnost: tutoring jedan na jedan postoji, ali nije dostupan svakom studentu u svakom trenutku, dok softverski tutor može da bude. Zato je pitanje koliko je takva pomoć dobra i kada je bolje ne davati je, a ne samo da li postoji.

## 2.2 Inteligentni tutorski sistemi

Inteligentni tutorski sistemi (engl. *intelligent tutoring systems*, ITS) su računarski sistemi koji učeniku daju individualnu nastavu i povratnu informaciju, najčešće pri rešavanju zadataka, bez učešća nastavnika u svakom koraku. Pregled istorije i razvoja ovih sistema [@alkhatlan2018] opisuje standardnu arhitekturu sa četiri komponente: model domena (stručno znanje o onome što se uči), model studenta (trenutno stanje njegovog znanja), model tutora, odnosno pedagoški modul (znanje o tome kada i kako intervenisati) i korisnički interfejs. Ponašanje sistema obično se opisuje kroz dve petlje: spoljašnja bira sledeći zadatak, a unutrašnja prati korake studenta unutar jednog zadatka i daje povratnu informaciju ili nagoveštaje [@alkhatlan2018].

*\[Slika 2.1: arhitektura ITS-a sa četiri modula i dve petlje; crta se pri sastavljanju rada.\]*

Među klasičnim sistemima pregled [@alkhatlan2018] navodi Cognitive Tutors (zasnovane na kognitivnom modelu ACT-R, sa trenutnom povratnom informacijom u algebri, geometriji, fizici i programiranju), Andes (Newtonova fizika, Bayesove mreže u modelu studenta), AutoTutor (dijalog na prirodnom jeziku) i SQL-Tutor (projektovanje baza podataka). Zajedničko im je da je svaki napravljen za jedan domen, sa ručno izgrađenim modelom stručnog znanja. Takva izrada traži tim stručnjaka i mnogo vremena, zbog čega su razvijeni alati za pravljenje tutora, kao što je CTAT [@alkhatlan2018].

Dokazi o delotvornosti ovih sistema su ozbiljniji nego za većinu obrazovnih tehnologija, ali nisu jednoznačni. VanLehn [@vanlehn2011] je uporedio vrste tutoringa prema granularnosti povratne informacije. Prema pregledu [@alkhatlan2018], efekat u odnosu na izostanak tutoringa iznosio je 0,79 za ljudskog tutora, 0,76 za sisteme koji povratnu informaciju daju na nivou koraka rešavanja i 0,40 za one koji je daju na nivou delova koraka. Sistemi koji ocenjuju samo konačan odgovor imali su efekat od oko 0,31 [@vanlehn2011]. Vrednosti su prikazane u tabeli 1.

Tabela 1. Efekat vrsta tutoringa u odnosu na izostanak tutoringa, prema [@alkhatlan2018] i [@vanlehn2011]

| Vrsta tutoringa | Na čemu se daje povratna informacija | Efekat (d) |
| --- | --- | --- |
| Samo konačan odgovor | konačan odgovor | 0,31 |
| Delovi koraka | unutar jednog koraka | 0,40 |
| Korak po korak | svaki korak rešavanja | 0,76 |
| Ljudski tutor | dijalog sa čovekom | 0,79 |

Najvažnija poruka za ovaj rad nije razlika između računara i čoveka nego granularnost: tutor koji prati korake i odgovara na njih približava se ljudskom tutoru, a onaj koji ocenjuje samo konačan odgovor ne. Zato su nagoveštaji u AITutor-u organizovani po koracima postupka (plan, razrada prva dva koraka, kompletno rešenje), a ne kao jedna povratna informacija o konačnom odgovoru.

Pregled [@alkhatlan2018] navodi i druge metaanalize: efekat od 0,42 u odnosu na nastavu koju vodi nastavnik, ali samo 0,13 na standardizovanim testovima. Autori pregleda zaključuju da ne postoji opšta saglasnost o delotvornosti ITS-a [@alkhatlan2018]. Osnovno praktično ograničenje klasičnih sistema je cena izrade po domenu. Veliki jezički modeli tu cenu bitno smanjuju, ali otvaraju nova pitanja o tačnosti i o tome da li pomoć zaista vodi učenju; o tome govori poglavlje 3.

## 2.3 Skela i postepeno davanje pomoći

Pojam skele (engl. *scaffolding*) uveli su Wood, Bruner i Ross u radu o tome kako tutor pomaže detetu pri rešavanju problema [@wood1976]. Tutor preuzima one elemente zadatka koje učenik još ne može da savlada, tako da se učenik može usredsrediti na elemente koji su u granicama njegovih sposobnosti, a podrška se povlači kako njegova sposobnost raste [@wood1976]. Ideja se oslanja na Vygotskijev pojam zone narednog razvoja (engl. *zone of proximal development*), odnosno razlike između onoga što učenik može samostalno i onoga što može uz vođenje [@vygotsky1978].

Teškoća je u meri. Koedinger i Aleven [@koedinger2007] taj problem nazivaju dilemom pomoći (engl. *assistance dilemma*): treba odlučiti kada učeniku dati informaciju, a kada zahtevati da je sam proizvede. Više pomoći ubrzava napredak, ali može smanjiti trud koji je učenju potreban, dok manje pomoći podstiče trud, ali može dovesti do zastoja. Eksperimenti sa kognitivnim tutorima ne daju jedinstveno pravilo koje važi za sve zadatke i učenike [@koedinger2007]. Osim toga, učenici često ne koriste dostupnu pomoć na način koji im koristi. Pregled Alevena i saradnika [@aleven2003] bavi se ponašanjem pri traženju pomoći i dizajnom pomoći u interaktivnim okruženjima za učenje; tipični problemi su izbegavanje pomoći kada je potrebna i njeno korišćenje da bi se brzo došlo do odgovora.

Šta pomoć treba da sadrži, razmatraju Hattie i Timperley [@hattie2007]. Prema njima, povratna informacija je najkorisnija kada odgovara na tri pitanja: kuda učenik ide (cilj), kako mu ide (trenutni napredak) i šta je sledeće (kako dalje). Za nagoveštaje to znači da ne treba samo da kažu da je nešto pogrešno, nego i da usmere na sledeći korak.

Lestvica nagoveštaja u AITutor-u je pokušaj da se ove ideje primene na ispitne zadatke. Ima tri nivoa: plan rešavanja, razradu prva dva koraka i kompletno rešenje. Iz literature proizlaze i kriterijumi koje implementacija treba da ispuni, a koji se u poglavlju 5 prevode u zahteve: viši nivo se otključava pokušajem ili izričitim zahtevom studenta, a ne automatski; nagoveštaj upućuje na konkretan deo materijala i govori šta uraditi sledeće; student može da preskoči lestvicu. Poslednji kriterijum potkrepljuju nalazi iz odeljka 2.4.

## 2.4 Produktivna borba i kognitivno opterećenje

Skeli se suprotstavlja ideja da je izvesno naprezanje korisno. Kapur [@kapur2008] je pokazao da učenici koji prvo samostalno pokušaju da reše složen problem, iako najčešće ne dođu do ispravnog rešenja, kasnije bolje razumeju pojmove i bolje prenose znanje od učenika kojima se gradivo najpre direktno objasni. Taj efekat naziva produktivnim neuspehom (engl. *productive failure*). Kasniji rad [@kapur2016] precizira da neuspeh sam po sebi nije koristan: razlikuje produktivni i neproduktivni neuspeh, kao i produktivni i neproduktivni uspeh, a ishod zavisi od toga kako je aktivnost osmišljena i koliko je podrške dostupno.

Granice su jednako važne. Sweller [@sweller1988] tvrdi da samo rešavanje problema opterećuje radnu memoriju na način koji ne doprinosi sticanju šema, pa početnici često više uče iz proučavanja rešenih primera nego iz samostalnog rešavanja. Renkl [@renkl2014] razvija teoriju učenja iz primera: rešeni primeri su najkorisniji kada se kombinuju sa podsticanjem učenika da objasni korake i kada se delovi rešenja postepeno izostavljaju. Kalyuga i saradnici [@kalyuga2003] opisuju efekat obrnute stručnosti (engl. *expertise reversal effect*): vođenje koje pomaže početnicima postaje suvišno, pa čak i štetno, kada učenik stekne stručnost, tako da podrška mora biti prilagodljiva.

Konačno, Chi i Wylie [@chi2014] u okviru ICAP razvrstavaju aktivnosti učenja prema načinu angažovanja: pasivno (primanje), aktivno (manipulisanje), konstruktivno (stvaranje novog sadržaja, na primer objašnjenja ili sledećeg koraka) i interaktivno (dijalog). Prema tom okviru ishodi učenja rastu u tom redosledu, pa čitanje gotovog rešenja spada u pasivne ili aktivne aktivnosti, a samostalno formulisanje sledećeg koraka u konstruktivne.

Za lestvicu nagoveštaja iz toga slede praktična pravila: prvi nivo ne sme da sadrži rešenje, drugi nivo se ponaša kao delimično rešen primer, student se podstiče da sam napravi sledeći korak, a lestvicu mora biti moguće preskočiti jer se potreba za vođenjem menja sa stručnošću.

## 2.5 Kognitivno prepuštanje i veliki jezički modeli

Kognitivno prepuštanje (engl. *cognitive offloading*) označava korišćenje fizičke radnje radi smanjenja kognitivnih zahteva zadatka [@risko2016]. Ono olakšava zadatak u datom trenutku, ali može da smanji ono što osoba zapamti ili nauči o prepuštenom sadržaju [@risko2016]. Za učenje je to presudno razlikovanje: alat koji sam daje rešenje je ekstremni oblik prepuštanja. Sa velikim jezičkim modelima (engl. *large language models*, LLM) taj rizik postaje praktičan, jer model može da proizvede kompletno rešenje za nekoliko sekundi.

Empirijski nalazi su mešoviti i zavise od načina korišćenja. Fan i saradnici [@fan2025] su u randomizovanom eksperimentu sa 117 studenata i četiri uslova uočili da je grupa koja je pisala esej uz ChatGPT više poboljšala ocenu eseja od ostalih, ali da se znanje i prenos naučenog nisu značajno razlikovali; autori upozoravaju da takva zavisnost od alata može da izazove metakognitivnu lenjost. Lehmann i saradnici [@lehmann2024] u dva unapred registrovana laboratorijska eksperimenta nalaze da LLM u celini ne menja ishod učenja, ali da način upotrebe presuđuje: kada LLM zamenjuje samostalni rad, studenti obuhvate više tema uz slabije razumevanje, a kada ga koriste kao dopunu, razumevanje raste, uz veći jaz između studenata sa slabijim i jačim predznanjem. Gerlich [@gerlich2025] na osnovu ankete i intervjua sa 666 ispitanika izveštava o negativnoj korelaciji (r = −0,68) između korišćenja AI alata i kritičkog mišljenja. Mere su samoprijavljene, a dizajn ne omogućava zaključak o uzroku, pa se nalaz ne može čitati kao dokaz da AI alati smanjuju kritičko mišljenje.

Najjasniji dokaz iz obrazovne prakse, terenski eksperiment sa srednjoškolcima, razmatra se u odeljku 3.4. Ovde je dovoljno zaključiti da literatura ne tvrdi da je upotreba LLM-a sama po sebi štetna, nego da šteta zavisi od toga da li alat zamenjuje učenikov napor ili ga usmerava. To opravdava dizajn u kojem konačno rešenje nije podrazumevani odgovor.

## 2.6 Šta iz ovoga sledi za dizajn

Pedagoški nalazi iz ovog poglavlja svode se na šest tačaka koje poglavlje 5 prevodi u zahteve za sistem.

- **Realan cilj.** Efekat od oko trećine standardne devijacije tipičan je za dobro osmišljen tutoring [@vonhippel2024]. Rad zato ne tvrdi da AITutor poboljšava učenje, nego meri pokazatelje koji se mogu proveriti.
- **Povratna informacija po koracima.** Granularnost povratne informacije je važnija od toga da li je tutor računar [@vanlehn2011].
- **Postepena pomoć.** Skela, dilema pomoći i traženje pomoći opravdavaju lestvicu čiji se viši nivoi otključavaju pokušajem ili izričitim zahtevom [@wood1976; @koedinger2007; @aleven2003].
- **Sadržaj nivoa.** Prvi nivo ne sadrži rešenje [@kapur2008; @kapur2016], drugi je delimično rešen primer [@sweller1988; @renkl2014], a od studenta se traži sledeći korak [@hattie2007; @chi2014].
- **Prilagodljivost.** Lestvica se može preskočiti, jer se potreba za vođenjem menja sa stručnošću [@kalyuga2003].
- **Rizik prepuštanja.** Konačno rešenje nije podrazumevani odgovor [@risko2016; @fan2025; @lehmann2024].

U poglavlju 7 proverava se samo ono što se može izmeriti: da li nagoveštaji otkrivaju rešenje i da li vode dalje.
