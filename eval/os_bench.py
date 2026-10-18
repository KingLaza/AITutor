"""Test sistema na pravim ispitima iz Operativnih sistema (pitanja bez rešenja; tačne odgovore je izračunao čovek).
Svako pitanje ide kroz /api/chat sa subject=Operativni sistemi. Ako sistem pokrene lestvicu nagoveštaja (Task),
šalje se i "Prikaži rešenje" da bismo videli ceo pokušaj. Upotreba: os_bench.py <izlazni.json>"""
import sys, json, time, urllib.request

URL = "http://127.0.0.1:5000/api/chat"
SUBJECT = "Operativni sistemi"

from os_questions import QUESTIONS

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
