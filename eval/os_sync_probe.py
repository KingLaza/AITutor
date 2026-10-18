"""Brza provera zadataka sa semaforima i monitorima (pseudo-kod iz predavanja). Odmah traži rešenje (nivo 3).
Ocena je ručna; skripta samo štampa odgovor i automatski proverava da li je korišćena notacija iz predavanja.
Upotreba: os_sync_probe.py <izlazni.json>"""
import sys, json, time, re, urllib.request

QUESTIONS = [
    ("jan24_q4_monitor",
     "Napisati monitor (Switch) koji procesima P1 i P2 obezbedjuje funkcije za pristup jednoj zajednickoj promenljivoj u memoriji koji treba da je naizmenicno setuju (postavljaju na 1) i resetuju (na 0) koriscenjem funkcija monitora set() i reset().",
     "monitor sa stanjem koje forsira naizmenicnost (npr. bool next_set) i uslovnim promenljivama sa cwait/csignal"),
    ("okt23_q4_sastanak",
     "Napisati monitor cije se funkcije mogu koristiti za sinhronizaciju dva procesa u vremenu, tako da proces koji prvi stigne do odredjene instrukcije u programu mora da saceka drugi proces, da stigne do odgovarajuce instrukcije, nakon cega nastavljaju zajedno (konkurentno ili paralelno). Ilustrovati koriscenje funkcija ovog monitora u okviru ova dva procesa?",
     "monitor sa brojacem dolazaka i jednom uslovnom promenljivom; prvi proces cwait, drugi csignal; prikaz poziva u P1 i P2"),
    ("jun24_q4_semafori",
     "Dva procesa-proizvodjaca P1 i P2 citaju naizmenicno poruke iz bafera koje upisuje proces-potrosac P3. Napisati programski kod kojim se koriscenjem semafora obezbedjuje da P1 i P2 naizmenicno citaju poruke poslate od P3.",
     "semafori: jedan po procesu citaocu (inicijalno 1 i 0) plus semafor za poruku; semWait/semSignal"),
]

NEW_QUESTIONS = [
    ("sinhro_monitor_semafori",
     "Napisati monitor Sinhro koji se moze koristiti da obezbedi da se prvi deo procesa P izvrsi uvek pre drugog dela procesa Q. Prikazati resenje istog problema koriscenjem semafora.",
     "monitor sa logickom promenljivom gotovo + cwait/csignal; semafor inicijalno 0: P signalizira posle prvog dela, Q ceka"),
    ("monitor_binarni_semafor",
     "Napisati programski kod monitora kojim se implementira binarni semafor.",
     "monitor sa bool slobodan, semWait: if(!slobodan) cwait; slobodan=false; semSignal: slobodan=true; csignal"),
    ("monitor_opsti_semafor",
     "Napisati programski kod monitora MojSemafor kojim se implementira n-arni (opsti) semafor.",
     "monitor sa brojacem vrednost i brojem blokiranih; semWait/semSignal"),
    ("monitor_comunicator",
     "Napisati monitor Comunicator za komunikaciju dva ili vise procesa putem jedne zajednicke promenljive tipa string.",
     "monitor sa jednom promenljivom i flagom puna; uslovne promenljive za slanje i prijem"),
    ("zastoj_proizvodjac_potrosac",
     "Prikazati programski kod pogresnim resenjem problema proizvodjac-potrosac koriscenjem semafora u kome dolazi do uzajamnog blokiranja.",
     "zamenjen redosled semWait(mutex) i semWait(empty) kod proizvodjaca; objasnjena sekvenca zastoja"),
]
if len(sys.argv) > 2 and sys.argv[2] == "new":
    QUESTIONS = NEW_QUESTIONS

out_path = sys.argv[1]
results = []
for qid, text, expected in QUESTIONS:
    t = time.time()
    req = urllib.request.Request("http://127.0.0.1:5000/api/chat",
        json.dumps({"message": "Prikaži rešenje", "history": [{"role": "user", "content": text}],
                    "activeTask": {"text": text, "level": 1}, "subject": "Operativni sistemi"}).encode(),
        {"Content-Type": "application/json"})
    d = json.load(urllib.request.urlopen(req, timeout=900))
    sec = round(time.time() - t, 1)
    reply = d["reply"]
    uses_course_notation = bool(re.search(r"semWait|semSignal|cwait|csignal", reply))
    uses_posix_junk = bool(re.search(r"printf|pthread|malloc|#include", reply))
    results.append({"id": qid, "seconds": sec, "expected": expected, "reply": reply, "sources": d["sources"],
                    "course_notation": uses_course_notation, "posix_junk": uses_posix_junk})
    json.dump(results, open(out_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(f"{qid:20} {sec:6}s notacija iz predavanja: {uses_course_notation}  POSIX/printf: {uses_posix_junk}", flush=True)
