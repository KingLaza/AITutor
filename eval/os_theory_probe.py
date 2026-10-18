"""Brza provera teorijskih/Da-Ne pitanja i fork() zadatka kroz normalan tok (bez unapred zadatog nivoa).
Ako sistem pokrene lestvicu nagoveštaja, traži se i rešenje. Ocena je ručna prema očekivanim odgovorima.
Upotreba: os_theory_probe.py <izlazni.json>"""
import sys, json, time, urllib.request

QUESTIONS = [
    ("okt23_q2_daNe",
     "Na sledeca pitanja odgovoriti sa Da, ili Ne + objasnjenje: a) Ako proces ima vise niti one dele programski kod, podatke i stek. b) Prilikom startovanja C programa, lokalne promenljive funkcije main() se smestaju na stek procesa. c) Programski kod malloc() funkcije nalazi se u adresnom prostoru procesa. d) U sistemu sa 3 nivoa stranicenja (bez kesa i TLB) pribavljanje i izvrsenje instrukcije koja sabira vrednost registra sa sadrzajem memorijske lokacije zahteva 3 pristupa memoriji. e) Sa 32-bitnim fizickim adresama i velicinom stranice od 4KB broj (adresa) virtuelne stranice uvek iznosi 20 bitova.",
     "a) Ne; b) Da; c) Da; d) Ne (8 pristupa); e) Ne"),
    ("jan24_q7_daNe",
     "Na sledeca pitanja odgovoriti sa Da, ili Ne + obrazlozenje u jednoj recenici: a. U sistemu sa stranicenjem u vise nivoa, samo stavka u tabeli stranica poslednjeg nivoa sadrzi fizicku adresu stranice (stranicnog okvira). b. Svaka stavka invertovane tabele stranica pored kontrolnih bitova sadrzi adresu fizicke stranice (stranicnog okvira) koja odgovara virtuelnoj adresi stranice. c. Prilikom obrade greske stranica mora da se izvrsi azuriranje tabele stranica.",
     "a) Da; b) Ne; c) Da"),
    ("jan24_q1_modovi",
     "U kom modu je sistem (kernel, korisnicki) nakon sto aktivni proces pozove neku od funkcija, i u kom stanju se nakon toga nalazi proces: a) pthread_join() b) strcmp(c,s) c) semWait(s) d) exchange(a,b) e) my_func(a, b)",
     "a) kernel, blokiran (ako nit nije zavrsena); b) korisnicki, izvrsava se; c) kernel; d) korisnicki, izvrsava se; e) korisnicki, izvrsava se"),
    ("fork_brojanje",
     "Koliko procesa se generise sledecim programskim kodom. Obrazloziti ukratko! #include <stdio.h> #include <unistd.h> int main() { if(fork()) if(fork()) if(fork()); return 0; }",
     "4 procesa"),
    ("jun24_q2_stanja",
     "U kom je stanju bio i u koje stanje prelazi proces kada: a) izvrsi read() funkciju b) izvrsi test&set() instrukciju c) izvrsi exit instrukciju d) izvrsi semWait(s) na semaforu cija je vrednost 0",
     "a) Izvrsava se -> Blokiran; b) ostaje Izvrsava se; c) Izvrsava se -> Zavrsen; d) Izvrsava se -> Blokiran"),
]

if len(sys.argv) > 2:  # opcioni filter po delu identifikatora pitanja, npr. "daNe"
    QUESTIONS = [q for q in QUESTIONS if sys.argv[2] in q[0]]

out_path = sys.argv[1]
results = []


def call(message, history, active):
    t = time.time()
    req = urllib.request.Request("http://127.0.0.1:5000/api/chat",
        json.dumps({"message": message, "history": history[-6:], "activeTask": active, "subject": "Operativni sistemi"}).encode(),
        {"Content-Type": "application/json"})
    d = json.load(urllib.request.urlopen(req, timeout=900))
    return d, round(time.time() - t, 1)


for qid, text, expected in QUESTIONS:
    d, sec = call(text, [], None)
    step = "direktno"
    if d["activeTask"]:
        history = [{"role": "user", "content": text}, {"role": "assistant", "content": d["reply"]}]
        d, sec2 = call("Prikaži rešenje", history, d["activeTask"])
        sec += sec2
        step = "L1+L3"
    results.append({"id": qid, "intent": d["intent"], "path": step, "seconds": sec, "expected": expected, "reply": d["reply"], "sources": d["sources"]})
    json.dump(results, open(out_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(f"{qid:18} {d['intent']:8} {step:9} {sec:6}s", flush=True)
