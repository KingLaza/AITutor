"""Test sistema na pravim ispitima iz Operativnih sistema (pitanja bez rešenja; tačne odgovore je izračunao čovek).
Svako pitanje ide kroz /api/chat sa subject=Operativni sistemi. Ako sistem pokrene lestvicu nagoveštaja (Task),
šalje se i "Prikaži rešenje" da bismo videli ceo pokušaj. Upotreba: os_bench.py <izlazni.json>"""
import sys, json, time, urllib.request

URL = "http://127.0.0.1:5000/api/chat"
SUBJECT = "Operativni sistemi"

QUESTIONS = [
    {"id": "jan24_q6_rasporedjivanje", "exam": "Januar 2024, zadatak 6",
     "text": "U sistemu se izvrsava 4 procesa A, B, C, D koji su dati svojim vremenom aktiviranja, prioritetom i duzinom CPU faze. Koji od procesa ce se izvrsavati u periodu 60-70ms, ako se rasporedjivanje obavlja po: SJF, SRTF, Prioritetu bez prekidanja, Prioritetu sa prekidanjem (manji broj, veci prioritet), RoundRobin (vremenski kvant = 30ms)? Proces A: CPU faza 50ms, prioritet 4, vreme aktiviranja 0ms. Proces B: 20ms, prioritet 1, aktiviranje 20ms. Proces C: 100ms, prioritet 3, aktiviranje 40ms. Proces D: 40ms, prioritet 2, aktiviranje 60ms.",
     "expected": "SJF: B; SRTF: A; Prioritet bez prekidanja: B; Prioritet sa prekidanjem: D; RR(30): A"},
    {"id": "okt23_q9_disk", "exam": "Oktobar 2023, zadatak 9",
     "text": "Koliko staza ima disk koji ima 2 tanjira (4 strane) i 16000 cilindara? U datom trenutku red I/O zahteva za cilindrima na disku sadrzi sledece zahteve [25, 100, 360, 77, 125, 222]. Upravo su usluzeni zahtevi [100] i [77] redom. Kojim cilindrima ce sledece biti pristupano prema algoritmima: SSTF, C-SCAN, SCAN?",
     "expected": "64000 staza; glava ide ka nizim cilindrima: SSTF 125,222,360,25; SCAN 25,125,222,360; C-SCAN 25,360,222,125"},
    {"id": "okt23_q10_inode", "exam": "Oktobar 2023, zadatak 10",
     "text": "Fajl sistem Unix System V ima blokove velicine 4KB i i-node koji sadrzi 10 direktnih adresa, jednu adresu jednostruko indirektnog, jednu dvostruko indirektnog i jednu trostruko indirektnog bloka. Disk adrese su velicine 8B. Koliko disk blokova, indeksnih i sa podacima je potrebno za smestanje sadrzaja datoteke cija je velicina a) 40KB b) 6MB c) 1GB.",
     "expected": "512 adresa po bloku. a) 10 podataka, 0 indeksnih; b) 1536 podataka, 4 indeksna; c) 262144 podataka, 513 indeksnih"},
    {"id": "okt23_q8_adresa", "exam": "Oktobar 2023, zadatak 8",
     "text": "Razmotriti memorijski podsistem OS sa 64-bitnim virtuelnim adresnim prostorom, 52-bitnim fizickim adresama, 256KB velicine stranice i tabelama stranica u 2 nivoa, sa istim brojem bitova kao indeks u obe tabele stranica. Svaka stavka (ulaz) u tabelu stranica je 8B. a) Koliko bitova u virtuelnoj adresi se ne koristi za adresiranje? b) koliko bitova se koristi za indeksiranje tabele stranica? c) kako izgleda format virtuelne adrese samo sa korisnim bitovima?",
     "expected": "offset 18 bita, indeks 15+15; a) 16 neiskoriscenih; b) 15 po tabeli (30 ukupno); c) 15|15|18"},
    {"id": "okt23_q7_lru", "exam": "Oktobar 2023, zadatak 7",
     "text": "Procesu su dodeljena 3 stranicna okvira. Za sledeci niz stranicnih referenci odrediti broj gresaka stranica, i sadrzaj stranicnih okvira nakon zamene stranice, ako je implementiran LRU algoritam. EDHBDEDAEBEDEBG",
     "expected": "9 gresaka stranica; na kraju okviri {E, B, G}"},
    {"id": "okt23_q2_daNe", "exam": "Oktobar 2023, zadatak 2",
     "text": "Na sledeca pitanja odgovoriti sa Da, ili Ne + objasnjenje: a) Ako proces ima vise niti one dele programski kod, podatke i stek. b) Prilikom startovanja C programa, lokalne promenljive funkcije main() se smestaju na stek procesa. c) Programski kod malloc() funkcije nalazi se u adresnom prostoru procesa. d) U sistemu sa 3 nivoa stranicenja (bez kesa i TLB) pribavljanje i izvrsenje instrukcije koja sabira vrednost registra sa sadrzajem memorijske lokacije zahteva 3 pristupa memoriji. e) Sa 32-bitnim fizickim adresama i velicinom stranice od 4KB broj (adresa) virtuelne stranice uvek iznosi 20 bitova.",
     "expected": "a) Ne (stek nije zajednicki); b) Da; c) Da; d) Ne (ukupno 8); e) Ne (zavisi od virtuelne adrese)"},
    {"id": "jan24_q7_daNe", "exam": "Januar 2024, zadatak 7",
     "text": "Na sledeca pitanja odgovoriti sa Da, ili Ne + obrazlozenje u jednoj recenici: a. U sistemu sa stranicenjem u vise nivoa, samo stavka u tabeli stranica poslednjeg nivoa sadrzi fizicku adresu stranice (stranicnog okvira). b. Svaka stavka invertovane tabele stranica pored kontrolnih bitova sadrzi adresu fizicke stranice (stranicnog okvira) koja odgovara virtuelnoj adresi stranice. c. Prilikom obrade greske stranica mora da se izvrsi azuriranje tabele stranica.",
     "expected": "a) Da; b) Ne (sadrzi virtuelnu stranicu/pid, indeksirana okvirom); c) Da"},
    {"id": "jan24_q8_adresa2", "exam": "Januar 2024, zadatak 8",
     "text": "Nacrtati format virtuelne adrese u sistemu koji implementira segmentaciju sa stranicenjem u dva nivoa, ukoliko proces moze imati maksimalno 512 segmenata, velicina stranice je 4KB, a stavka u tabeli stranice je 8B. Koliko iznosi velicina virtuelnog adresnog prostora?",
     "expected": "9 (segment) | 9 | 9 | 12 (pomeraj) = 39 bita; 2^39 B = 512 GB"},
    {"id": "jan24_q4_monitor", "exam": "Januar 2024, zadatak 4",
     "text": "Napisati monitor (Switch) koji procesima P1 i P2 obezbedjuje funkcije za pristup jednoj zajednickoj promenljivoj u memoriji koji treba da je naizmenicno setuju (postavljaju na 1) i resetuju (na 0) koriscenjem funkcija monitora set() i reset().",
     "expected": "monitor sa uslovnom promenljivom/stanjem koje forsira naizmenicnost set()/reset() (npr. bool next_is_set; cwait/csignal)"},
    {"id": "jun24_q3_teorija", "exam": "Jun 2024, zadatak 3",
     "text": "Navesti tri stavke u upravljackom bloku niti koje se menjaju kada dodje do zamene niti (context switch-a)?",
     "expected": "npr. stanje niti, programski brojac (PC), pokazivac steka (SP) i drugi registri"},
]

import re

# Automatska provera tačnosti za zadatke koje rešavaju alati (regex nad odgovorom sistema).
CHECKS = {
    "jan24_q6_rasporedjivanje": [
        r"SJF[^\n]*\n\s*U periodu 60-70ms izvršava se: B\n",
        r"SRTF[^\n]*\n\s*U periodu 60-70ms izvršava se: A\n",
        r"PRIORITY:[^\n]*\n\s*U periodu 60-70ms izvršava se: B\n",
        r"PRIORITY_PREEMPTIVE[^\n]*\n\s*U periodu 60-70ms izvršava se: D\n",
        r"RR \(q=30ms\)[^\n]*\n\s*U periodu 60-70ms izvršava se: A\n",
    ],
    "okt23_q9_disk": [r"= 64000", r"SSTF: 125 -> 222 -> 360 -> 25", r"- SCAN: 25 -> 125 -> 222 -> 360", r"C-SCAN: 25 -> 360 -> 222 -> 125"],
    "okt23_q10_inode": [r"Fajl 40KB: 10 blokova sa podacima \+ 0 indeksnih", r"Fajl 6MB: 1536 blokova sa podacima \+ 4 indeksnih", r"Fajl 1GB: 262144 blokova sa podacima \+ 513 indeksnih"],
    "okt23_q8_adresa": [r"neiskorišćenih od 64: 16", r"P1 15 \| P2 15 \| pomeraj 18"],
    "okt23_q7_lru": [r"LRU: 9 grešaka"],
    "jan24_q8_adresa2": [r"segment 9 \| P1 9 \| P2 9 \| pomeraj 12", r"512 GB"],
}

fast = len(sys.argv) > 2 and sys.argv[2] == "fast"
results = []
out_path = sys.argv[1]


def call(message, history, active):
    t = time.time()
    try:
        req = urllib.request.Request(URL, json.dumps({"message": message, "history": history[-6:], "activeTask": active, "subject": SUBJECT}).encode(),
                                     {"Content-Type": "application/json"})
        d = json.load(urllib.request.urlopen(req, timeout=900))
        return d, round(time.time() - t, 1), None
    except Exception as e:
        return None, round(time.time() - t, 1), str(e)


def record(q, label, text, d, sec, err):
    r = {"id": q["id"], "exam": q["exam"], "step": label, "seconds": sec, "expected": q["expected"]}
    if err:
        r["error"] = err
    else:
        r.update(intent=d["intent"], reply=d["reply"], sources=d["sources"], hintLevel=(d["activeTask"] or {}).get("level"))
    results.append(r)
    json.dump(results, open(out_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(f"{q['id']:28} {label:3} {sec:7}s  {r.get('intent', 'ERR')}", flush=True)
    return r


if fast:
    # Brzi režim: samo zadaci za alate; preskače se plan (L1) i odmah traži rešenje (L3). Traje par minuta.
    passed = total = 0
    for q in QUESTIONS:
        if q["id"] not in CHECKS:
            continue
        history = [{"role": "user", "content": q["text"]}]
        d, sec, err = call("Prikaži rešenje", history, {"text": q["text"], "level": 1})
        r = record(q, "L3", "Prikaži rešenje", d, sec, err)
        reply = r.get("reply", "")
        solved_by_tool = "izračunato programski" in reply
        hits = [bool(re.search(p, reply)) for p in CHECKS[q["id"]]]
        passed += sum(hits)
        total += len(hits)
        print(f"   alat: {'DA' if solved_by_tool else 'NE'}  provere: {sum(hits)}/{len(hits)}", flush=True)
    print(f"\nUKUPNO tacnih provera: {passed}/{total}")
    sys.exit(0)

for q in QUESTIONS:
    d, sec, err = call(q["text"], [], None)
    record(q, "L1", q["text"], d, sec, err)
    if err or not d["activeTask"]:
        continue
    history = [{"role": "user", "content": q["text"]}, {"role": "assistant", "content": d["reply"]}]
    d2, sec2, err2 = call("Prikaži rešenje", history, d["activeTask"])
    record(q, "L3", "Prikaži rešenje", d2, sec2, err2)
