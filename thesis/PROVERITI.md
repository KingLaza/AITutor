# Za proveru pre predaje

Ovo nije deo rada. Lista kaže šta u tekstu ne počiva na izvoru koji sam otvorio, da se to proveri pre nego što nacrt ode mentoru.

1. **VanLehn [4], vrednost 0,31.** Vrednosti 0,79, 0,76 i 0,40 potvrđene su u pregledu [3]. Vrednost 0,31 (samo konačan odgovor) uzeta je sa VanLehnovih slajdova; originalni članak izdavač ne dozvoljava da se otvori. Proveriti u članku.
2. **Von Hippel [2].** Brojevi iz metaanaliza (0,33 i 0,37) uzeti su iz njegovog članka, ne iz originala (Cohen, Kulik i Kulik 1982; Nickow, Oreopoulos i Quan 2020).
3. **Klasični radovi [5]–[16].** Bibliografski podaci su potvrđeni preko Crossref-a. Sadržaj je opisan prema opštem poznavanju tih radova, jer ih izdavači ne daju na čitanje. Posebno proveriti sažetke: tvrdnju o dilemi pomoći [7], o ponašanju pri traženju pomoći [8], o produktivnom neuspehu [10], [11] i o ICAP okviru [15].
4. **Vygotsky [6].** Knjiga, navedena iz sećanja; proveriti izdavača i godinu u biblioteci.
5. **Bastani i dr. [35].** Brojevi (48%, 127%, −17%, skoro 1000 učenika, Turska) potvrđeni su u sažetku na SSRN-u i u članku Wharton School; stranica PNAS-a nije bila dostupna. Proveriti u samom radu, naročito formulaciju „oslonac“.
6. **Jill Watson [26].** Brojevi 76,7% i 31,3% su iz tabele 1 rada, a tekstualni izvod tabele je nejasan. Proveriti u PDF-u koja kolona je „prošlo“.
7. **SortingHat [34].** Pročitan je samo sažetak. Podaci na arXiv stranici se ne slažu (broj 2606.00015, datum predaje 13. 4. 2026, DOI iz ACM-a), pa u PDF-u treba naći stvarnu konferenciju i godinu. U odeljku 3.3 ostala je oznaka \[DOPUNITI\].
8. **LearnLM [20].** Spisak autora je kako ga ispisuje arXiv stranica; proveriti da je potpun, jer fakultet traži sve autore.
9. **Lewis [21].** Rad je objavljen na NeurIPS-u 2020; ovde je navedena arXiv verzija, pa može da se zameni zvaničnim podacima konferencije.
10. **Metaanalize u pregledu [3].** Brojevi 0,42 i 0,13 preuzeti su iz pregleda, ne iz izvornih radova.
11. **Kestin [36].** Nije potvrđeno da su detaljna rešenja bila upisana u sistemsko uputstvo; rad navodi da su „obogatili uputstva“ rešenjima, pa tako piše i u tekstu (tabela 3).
12. **Pretraga nije sistematska.** Obavljena je pretraživačem u oktobru 2026. Pre odbrane ponoviti pretragu (Google Scholar, Semantic Scholar, ACM, IEEE) za pojmove iz odeljka 3.6.
13. **Brojevi tabela i slika.** Ovde su numerisani po redu (tabele 1–3, slika 2.1); pri sastavljanju celog rada uskladiti prema uputstvu fakulteta.

## Poglavlja 4-6 (Claude Code)

14. **Ollama [@ollama].** Navedena je početna stranica projekta, nisam je otvarao; tvrdnje o Ollami u tekstu su opšte (lokalno poslužuje modele preko HTTP-a). Proveriti ili zameniti zvaničnom dokumentacijom.
15. **EmbeddingGemma [@embeddinggemma2025].** Rad ima 89 autora; u .bib je naveden kao „EmbeddingGemma Team“. Proveriti da li fakultet prihvata timskog autora ili traži sve autore.
16. **Skladišta vektora (tabela 4.3).** Svaka ćelija je iz dokumentacije koju sam otvorio 22.10.2026. („nije navedeno“ znači da stranica to ne pominje, ne da mogućnost ne postoji). Nije otvorena stranica o licencama; licence nisu navedene. Nema sopstvenih merenja performansi.
17. **RRF, konstanta k = 60 [@cormack2009].** Vrednost je preuzeta iz opisa rada u pretrazi, nisam otvorio sam rad. Proveriti u originalu.
18. **HNSW [@malkov2016].** Navedena je arXiv verzija; rad je kasnije objavljen u časopisu (IEEE TPAMI), podatke proveriti i eventualno zameniti.
19. **Ponašanje Gemma 4 (`think`) i ponovno učitavanje modela pri promeni veličine konteksta (odeljak 4.1).** To su zapažanja iz razvoja, nisu iz dokumentacije ni posebno mereni; ako ostanu u radu, proveriti u dokumentaciji Ollame ili ih formulisati kao zapažanja.
20. **Prag odbijanja za Operativne sisteme (odeljak 5.4).** Merenje (`eval/results/threshold_eval_operativni_sistemi.txt`) daje najdalje pitanje iz predmeta 0,578; prag u kodu je 0,64, a razlog nije zabeležen. Ako se želi tvrdnja o razlogu, treba je dopuniti dodatnim merenjem.
21. **Brojevi o kodu (odeljak 6.1).** Linije koda su prebrojane 22.10.2026. (`wc -l` nad `.cs` i `.ts*`/`.css` fajlovima bez generisanih foldera); promeniće se. Ponoviti pre predaje.
