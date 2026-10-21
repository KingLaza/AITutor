# Model pretnji za privatnost (nacrt)

Tvrdnja "podaci ostaju na uređaju" je osobina dizajna, ne dokaz; ovde se navodi šta tačno važi.

## Šta ostaje na računaru studenta

- **Modeli:** Ollama na domaćinu (ili u kontejneru); upiti idu na `127.0.0.1`/Docker mrežu, bez spoljnog servisa.
- **Baza:** PostgreSQL u lokalnom Docker volumenu: delovi materijala i vektori, profili (samo ime), poruke razgovora (tekst, izvori, aktivni zadatak).
- **Upload:** fajl se privremeno upisuje u privremeni folder API kontejnera, obrađuje se i briše; u bazi ostaju delovi teksta i vektori sa tipom izvora `moje`.
- **Web klijent:** u pregledaču se pamte samo izabrani profil i predmet (localStorage).
- Nema naloga, lozinki, telemetrije ni analitike u kodu.

## Šta napušta uređaj

- Pri normalnom radu ništa, osim preuzimanja modela i Docker slika pri instalaciji.
- Baza je dostupna samo na `127.0.0.1` (port mapiran na loopback); web na `WEB_PORT` (podrazumevano 8080) je dostupan i drugim uređajima u mreži ako to firewall dozvoljava: napomenuti u uputstvu.
- Ako se doda cloud LLM sloj (budući rad): tekst pitanja i preuzeti delovi materijala bi napustili uređaj; tada treba izričit pristanak, podešavanje po predmetu i oznaka u UI.

## Rizici i ograničenja

- **Profil bez lozinke:** svako ko ima pristup instanci vidi sve profile i razgovore (po dizajnu: instanca je lična ili za malu grupu na istom računaru).
- **Dodat materijal nije privatan po profilu** u trenutnoj implementaciji: vidljiv je svim profilima na instanci (planirano: privatan po profilu).
- **Autorska prava:** materijal kursa nije u javnom repou; gotova baza znanja (dump) deli se samo uz dozvolu autora materijala.
- **Logovi razgovora:** u bazi su u čistom tekstu; enkripcija diska je odgovornost korisnika.
- **Neprovereni PDF-ovi:** upload prolazi kroz parser (PdfPig); veličina ograničena na 50 MB; nema skeniranja sadržaja.
