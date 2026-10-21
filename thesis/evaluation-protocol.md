# Protokol evaluacije

Cilj: ispraviti slabosti prvog merenja (in-sample, jedan pokušaj po ćeliji, tačna rešenja potencijalno kružna) pre nego što se piše poglavlje 7.

## 1. Podela podataka

- **Razvojni skup:** ispiti na osnovu kojih su pisani rešavači, obrasci, kartice i promptovi (ispiti 2022-2024, 25 ispita).
- **Test skup:** ispit koji nije viđen (npr. Septembar 2026, zip je pod lozinkom; ili zadaci od predmetnog profesora).
- Pre prvog pokretanja na test skupu: git tag (`eval-holdout-v1`), pokreće se jednom, bez naknadnih izmena sistema.
- Ako nema novog ispita: parametarske varijante poznatih zadataka (isti šablon, drugi brojevi). Mere izdvajanje parametara i rešavač, ne pokrivenost šablona; navesti to otvoreno.
- Zadaci van šablona se ne izbacuju iz rezultata: njihov pad na običan RAG je nalaz.

## 2. Šta se meri

| Šta | Metrika | Napomena |
|---|---|---|
| Računski zadaci | tačan konačni odgovor + bodovi po koracima | tačno rešenje mora biti **nezavisno** (profesor ili ručno), ne iz sopstvenog rešavača |
| Teorija i Da/Ne | tačnost naspram ključa | |
| Vernost izvoru | udeo tvrdnji podržanih preuzetim tekstom (duh RAGAS-a) | mali lokalni sudija je nepouzdan: izmeriti slaganje sa ručnom ocenom na 30-50 primera |
| Pretraga | recall@k i MRR na označenom skupu (~50 pitanja) | poredi embedding modele i hibrid naspram samo vektorske pretrage |
| Odbijanje | preciznost i odziv za pitanja iz i van predmeta, za više pragova | kriva pokazuje da je prag izmeren |
| Nagoveštaji | dimenzije iz Maurya i dr.: otkrivanje odgovora, pružanje vođenja, delotvornost, koherentnost; automatska provera "curenja" (sadrži li nivo 1 ili 2 konačni odgovor) | dva ocenjivača na uzorku, Cohenov kappa |
| Izdvajanje parametara (LLM) | tačnost po polju naspram ručno označenih parametara | odvaja grešku modela od greške rešavača |
| Pokrivenost šablona | udeo zadataka na test ispitu koji spadaju u neki šablon | pravi odgovor na "koliko sistem uopštava" |
| Tačnost rešavača | unit testovi | već postoje |
| Resursi | latencija (medijana i p95) po tipu zadatka, vršna RAM, tokeni u sekundi | isti laptop, isti uslovi |

Na test ispitu izveštavati **tri broja odvojeno**: pokrivenost šablona, tačnost izdvajanja parametara, tačnost rešavača.

## 3. Ponavljanja i statistika

- 3-5 ponavljanja po zadatku i konfiguraciji; jedinica analize je zadatak (prosek ponavljanja).
- Konfiguracije se porede na istim zadacima: egzaktni McNemar (ili znakovni test) i Wilsonov interval za udele.
- Snaga (egzaktni McNemar, alfa 0.05, simulacija iz izveštaja): 10 zadataka malo, 20 zadataka oko 33%, 30 oko 58%, 40 oko 74% za razliku "A pogađa 30% zadataka na kojima B greši, B 5%". Za male razlike (npr. nagoveštaji sa primerima i bez) treba 40 i više zadataka.
- Beležiti: digest modela (`ollama show`), verziju Ollame, embedding model, prag, hash commita, seed, temperaturu.

## 4. Poređenje sa ChatGPT i Claude

1. Isti tekst zadatka, bez dodatnih uputa, novi razgovor za svaki zadatak i ponavljanje.
2. Dva stanja po alatu: samo model, i sa istim uploadovanim materijalom (Project / custom GPT / NotebookLM). Proveriti da li se izvršavanje koda može isključiti i probati oba načina.
3. Beležiti: tačan naziv modela kako ga aplikacija prikazuje, plan, datum i vreme, da li je Study Mode / learning mode / Guided Learning uključen, da li su memorija i prilagođena uputstva isključeni; sačuvati ceo razgovor.
4. Rubrika napisana unapred; odgovori bez oznake sistema i nasumičnim redom; deo uzorka ocenjuje druga osoba.
5. Zaključak formulisati kao "u ovom protokolu, na dan X".

## 5. Pretnje validnosti (jedno mesto u radu, poglavlje 7.1)

- Isti ispiti za razvoj i test (rešeno hold-out ispitom).
- Mali broj zadataka i ponavljanja (rešeno ponavljanjima i parovima testova).
- Kružna tačna rešenja (rešeno nezavisnim rešenjima).
- Kontaminacija: javni ispiti su možda u podacima cloud modela; ne može se isključiti.
- Mali lokalni model kao sudija vernosti (izmeriti slaganje sa ručnom ocenom).
- Cloud alati se menjaju (protokol sa datumom).
- Ocenjivač je i autor sistema (slepo ocenjivanje, drugi ocenjivač na uzorku).

## 6. Korisnička studija (opciono)

Formativna provera upotrebljivosti, ne dokaz učenja. 5-10 učesnika, svaki rešava dva zadatka (jedan uz AITutor, jedan uz običan četbot, uravnotežen redosled), vreme, tačnost, SUS, 2-3 otvorena pitanja; izveštaj opisno.
Bez imena, e-pošte i IP adrese; dobrovoljno, uz kratko obaveštenje; logovi lokalno i uz pristanak. Pravila fakulteta proveriti sa mentorom.
