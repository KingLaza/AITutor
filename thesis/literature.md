# Literatura

Oznake: **P** primarni izvor otvoren (stranica članka ili zvanična najava), **S** sekundarni ili samo naslov, **M** po sećanju: **proveriti u originalu pre citiranja**.
Preuzeto iz istraživačkog izveštaja (21.10.2026); `P (sažetak)` znači da je pročitan samo sažetak, ne ceo rad.

## 1. Temelji: efikasnost tutoringa i ITS

| Rad | God. | Šta tvrdi / koristi se za | Prov. |
|---|---|---|---|
| Bloom, "The 2 Sigma Problem", Educ. Researcher 13(6) | 1984 | individualni tutoring + mastery learning, ~2 SD; **preuveličano** | M |
| von Hippel, "Two-Sigma Tutoring: Separating Science Fiction from Science Fact", Education Next | 2024 | kritika: realni efekti ~0.3-0.4 SD (Nickow, Oreopoulos i Quan 2020: ~0.37 SD u 96 studija; Cohen, Kulik i Kulik 1982: ~0.33 SD) | P |
| VanLehn, "Relative Effectiveness of Human Tutoring, ITS, and Other Tutoring Systems", Educ. Psychologist 46(4) | 2011 | meta-analiza po granularnosti povratne informacije (po sećanju: ljudski d≈0.79, ITS na nivou koraka d≈0.76, pod-koraka 0.40, samo odgovor 0.31) | M |
| Pregled ITS, arXiv:1812.09628 | 2018 | polazna lista klasičnih sistema (Cognitive Tutor, AutoTutor, ASSISTments) | S |

## 2. Komercijalni LLM tutori (zvanične najave, bez izmerenih ishoda učenja)

| Sistem | God. | Pristup | Dokaz | Prov. |
|---|---|---|---|---|
| ChatGPT Study Mode | 29.7.2025 | prilagođena sistemska uputstva sa nastavnicima; sokratska pitanja, skele | OpenAI sam navodi "nekonzistentno ponašanje" | P |
| Claude for Education, learning mode | 2.4.2025 | sokratsko ispitivanje u Projects | nema u najavi | P |
| Gemini Guided Learning (LearnLM) | 6.8.2025 | pitanja, korak po korak, kvizovi | nema izmerenih ishoda u najavi | P |
| Khanmigo, Oreopoulos i Low, "One Click Away", NBER w35620 | 2026 | tutor koji vodi, ne daje odgovore | klaster RCT, 18 škola, dve godine: efekat ~0.06-0.08 SD godišnje, sličan kontroli; korišćen u 17% sesija sa greškom | P |
| NotebookLM | 2023+ | odgovori iz uploadovanih izvora | vidi odeljak 5 | S |

## 3. Akademski LLM i RAG tutori

| Rad | God. | Pristup | Predmet / dokaz | Prov. |
|---|---|---|---|---|
| Bastani i dr., PNAS 122(26) e2422633122 | 2025 | GPT Base prema GPT Tutor (zaštitni mehanizmi) | matematika, ~1000 učenika: tokom vežbe +48% / +127%; na ispitu bez AI-ja Base −17%, Tutor bez veće štete | P |
| Kestin i dr., Sci. Rep. 15, 17458 | 2025 | RCT, AI tutor sa unapred napisanim rešenjima u promptu | fizika, Harvard, 194 studenta: bolje od aktivnog učenja (~0.63 SD); jedan kurs, dve lekcije | P |
| Liffiton i dr., CodeHelp | 2023 | LLM sa zaštitom koja ne otkriva rešenja | uvodno programiranje, 52 studenta, 12 nedelja: percepcije, nema merenja učenja | P |
| Taneja i dr., Jill Watson | 2024 | RAG, gpt-3.5-turbo, provera entailment-a, citira dokument i stranu | 150 pitanja: 76.7% prošlo prema 31.3% za OpenAI Assistants | P |
| Nazar i dr., CourseGPT | 2024 | otvoreni modeli (Mixtral-8x7b) | bezbednost IS: tačnost 88.0%, vernost izvoru 66.6% | P |
| Reza i dr., "Small Models, Big Support" | 2025 | lokalni LLM 3-7B, RAG + CAG, verifikator | alat za nastavnike (fizika); metrike nisu u sažetku | P |
| Xiao, Hou i Stamper, LLM Hint Factory | 2024 | četiri nivoa GPT nagoveštaja | programiranje, 12 početnika: apstraktni nagoveštaji često nedovoljni | P |
| LearnLM, Google | 2024-25 | post-trening za pedagoško pridržavanje uputstava | eksperti preferiraju +31% prema GPT-4o | P |
| Fan i dr., "Beware of Metacognitive Laziness", BJET (10.1111/bjet.13544) | 2024 | RCT, 4 uslova, 117 studenata | ChatGPT: bolji esej, isto znanje i transfer | P |
| Maurya i dr., "Unifying AI Tutor Evaluation", MRBench | 2024-25 | taksonomija od 8 pedagoških dimenzija | okvir za ocenjivanje | P |
| Lehmann i dr. | 2024-25 | zamena naspram dopune učenja LLM-om | više tema, slabije razumevanje kad LLM generiše rešenja | P |
| Gerlich | 2025 | N=666, korelacija upotrebe AI i kritičkog mišljenja (r=−0.68) | korelaciono i samoprijavljeno | P |

## 4. Pedagoške osnove (časopisni podaci provereni preko Crossref-a, sadržaj po sećanju)

| Okvir | Ključni rad | Veza sa AITutor |
|---|---|---|
| Skela, ZPD | Wood, Bruner i Ross, J. Child Psychol. Psychiatry 17(2), 1976; Vygotsky 1978 | lestvica nagoveštaja kao formalizacija skele |
| Dilema o pomoći | Koedinger i Aleven, Educ. Psychol. Rev. 19(3), 2007 | kada se otključava nivo 2 i 3 |
| Traženje pomoći | Aleven i dr., Rev. Educ. Res. 73(3), 2003 | podsticati pokušaj pre pomoći |
| Produktivna borba | Kapur, Cogn. Instr. 26(3), 2008; Educ. Psychol. 51(2), 2016 | opravdava da nivo 1 ne sadrži rešenje (ne preterivati) |
| Povratna informacija | Hattie i Timperley, Rev. Educ. Res. 77(1), 2007 | nagoveštaj treba da kaže šta uraditi sledeće |
| Kognitivno opterećenje, rešeni primeri | Sweller, Cogn. Sci. 12(2), 1988; Renkl, Cogn. Sci. 38(1), 2014 | vezivanje nagoveštaja za rešene primere iz materijala |
| Efekat obrnute stručnosti | Kalyuga i dr., Educ. Psychol. 38(1), 2003 | lestvica mora moći da se preskoči |
| ICAP | Chi i Wylie, Educ. Psychol. 49(4), 2014 | traženje koraka od studenta vredi više od prikaza rešenja |
| Kognitivno prepuštanje | Risko i Gilbert, Trends Cogn. Sci. 20(9), 2016 | pojam; koristiti uz empirijske radove iz odeljka 3 |

Praktična pravila za lestvicu: nivo 1 treba da upućuje na konkretan deo materijala; lestvica se otključava pokušajem ili izričitim zahtevom, ne automatski; korisnik treba da može da je preskoči.
**Ne tvrditi** da lestvica poboljšava učenje: mogu se meriti samo posrednici (otkriva li odgovor, pruža li vođenje, delotvornost).

## 5. Uploadovani materijal u komercijalnim alatima (zvanična dokumentacija)

| Alat | Kako koristi materijal | Limiti | Prov. |
|---|---|---|---|
| ChatGPT custom GPT i Projects | tekst se izvlači iz fajlova, slike se odbacuju (osim vizuelne pretrage PDF-ova na Enterprise); nije izričito rečeno da li je pretraga RAG | custom GPT do 10 fajlova, do 2M tokena po tekstualnom fajlu; Projects: 5 (Free), 25 (Go, Plus), 40 (Edu, Pro, Business, Enterprise) | P |
| Claude Projects | RAG se uključuje automatski kad znanje projekta dostigne granicu konteksta | kapacitet do 10x veći; plaćeni planovi | P |
| NotebookLM / Gemini Notebook | odgovori iz izvora, obično sa citatima | do 500.000 reči po izvoru, do 200 MB po lokalnom fajlu | P |

## 6. Radovi najbliži temi (pozicioniranje; moraju se pročitati u celini)

| Rad | Zašto | Prov. |
|---|---|---|
| **SortingHat**, Zhang i dr., arXiv 2606.00015 (2026) | digitalni asistent za predmet Operativni sistemi: RAG, višeagentno RL, 3D lik, generisanje vežbi i ocenjivanje; najbliži temi, mora se citirati | P (sažetak) |
| Katharakis i dr., 2025 | osam malih modela (7-17B) + RAG prema GPT-4o na računarima za potrošače; najbliži hipotezi (a) | P (sažetak) |
| CoTu / Tran i dr., 2026 | neuro-simbolički pristup: mali model (4B) piše Z3/Python koji računa; 3. mesto EXACT 2026; najbliži hipotezi (b) | P (sažetak) |
| PAL, Program of Thoughts | 2022 | princip "LLM piše program, rešavač računa" | M |
| GraySim (SIGCSE 2025 demo) | simulatori za OS vežbe | S (samo naslov) |
| Serbian SuperGLUE (2026), EQ-Bench-Serbian, YugoGPT | benchmarkovi i modeli za srpski, ne obrazovanje | M |

## 7. Evaluacija

| Rad | Za šta | Prov. |
|---|---|---|
| Miller, "Adding Error Bars to Evals", 2024 | statistika evaluacije LLM-ova | P (sažetak) |
| Zheng i dr., LLM-as-a-Judge, 2023 | granice automatskog ocenjivanja (pristrasnosti, slaganje >80% za jake sudije; ne važi automatski za mali model) | P |
| Bangor i dr., SUS, 2008 | prosek oko 68 (po sećanju) | M |

## Prioritetno čitanje (15)

1 Bastani 2025, 2 Kestin 2025, 3 Oreopoulos i Low 2026, 4 VanLehn 2011, 5 von Hippel 2024 i Bloom 1984, 6 Lehmann 2024-25, 7 Koedinger i Aleven 2007, 8 Xiao i dr. 2024, 9 Maurya i dr., 10 Katharakis i dr. 2025, 11 CoTu 2026, 12 SortingHat 2026, 13 Taneja (Jill Watson) 2024, 14 Miller 2024, 15 Zheng 2023.

## Nije proveravano (otvoreno)

- Nije mogao da se pročita: arXiv 2605.16207 ("Confirming Correct, Missing the Rest: LLM Tutoring Agents Struggle Where Feedback Matters Most"), ScienceDirect rad o "rule-integrated" LLM tutorima za matematiku, originalni VanLehn. Prva dva su relevantna za hipotezu (b).
- Po sećanju, nepotvrđeno: VanLehnove veličine efekta, Bloomove brojke, sadržaj radova Aleven, Hattie i Timperley, Sweller, Renkl, Kalyuga, Chi i Wylie, to da ChatGPT i Claude mogu da izvrše kod, PAL, GraySim.
- Tvrdnje "nisam našao" o novosti znače samo da nisu nađene ovim pretraživačem u toj sesiji. Pre pisanja pozicioniranja ponoviti pretragu (Google Scholar, Semantic Scholar, ACM, IEEE) za pojmove iz hipoteza (a)-(e).
