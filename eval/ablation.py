"""Poređenje tri režima na istim pitanjima iz Operativnih sistema (pravi ispitni zadaci, tačni odgovori izračunati ručno):
  plain       - model bez pretrage (opšte znanje)
  ragnotools  - RAG (pretraga po materijalu), ali model sam rešava zadatke, bez programskih alata
  rag         - ceo sistem: RAG + programski alati (solveri) za ponavljajuće tipove zadataka
Za zadatke se u RAG režimima odmah traži rešenje ("Prikaži rešenje"), jer nas zanima kvalitet rešenja, a ne lestvica nagoveštaja.
Automatske provere su tolerantni regexi nad ključnim činjenicama; rezultate treba i ručno pregledati.
Upotreba: ablation.py <izlazni.json> [mode1,mode2,...] [id1,id2,...]"""
import sys, json, time, re, urllib.request

sys.path.insert(0, __file__.rsplit("eval", 1)[0] + "eval")
from os_questions import QUESTIONS  # noqa: E402

URL = "http://127.0.0.1:5000/api/chat"
SUBJECT = "Operativni sistemi"
MODES = ["plain", "ragnotools", "rag"]
TASK_IDS = {"jan24_q6_rasporedjivanje", "okt23_q9_disk", "okt23_q10_inode", "okt23_q8_adresa", "okt23_q7_lru",
            "jan24_q8_adresa2", "jan24_q4_monitor"}


def verdicts(reply, letters):
    out = {}
    for l in letters:
        m = re.search(rf"(?mi)^\W*{l}[\).:]\W*(da|ne)\b", reply)
        out[l] = m.group(1).capitalize() if m else None
    return out


def verdict_check(expected):
    def check(reply):
        got = verdicts(reply, expected.keys())
        return [(f"{l}={v}", got[l] == v) for l, v in expected.items()]
    return check


def regex_check(*pairs, strip_index_labels=False):
    def check(reply):
        if strip_index_labels:  # "P1 15 | P2 15" -> "15 | 15"
            reply = re.sub(r"\bP[12]\b", "", reply)
        return [(label, bool(re.search(p, reply, re.I))) for label, p in pairs]
    return check


def sched_check(reply):
    def first_letter(label):
        m = re.search(rf"{label}[^\n]*\n?[^\n]{{0,120}}?\b([ABCD])\b(?!\w)", reply)
        return m.group(1) if m else None
    exp = [("SJF", "B"), ("SRTF", "A"), ("PRIORITY:", "B"), ("PRIORITY_PREEMPTIVE", "D"), ("RR", "A")]
    # tolerantno: u slobodnom tekstu algoritmi nose različite nazive, pa se traže po nekoliko varijanti
    alt = {"SJF": r"SJF", "SRTF": r"SRTF", "PRIORITY:": r"prioritet(?:u)? bez prekid|PRIORITY:", "PRIORITY_PREEMPTIVE": r"prioritet(?:u)? sa prekid|PRIORITY_PREEMPTIVE", "RR": r"Round ?Robin|RR"}
    return [(f"{k}->{v}", first_letter(alt[k]) == v) for k, v in exp]


def lru_check(reply):
    """Konačan broj grešaka = poslednji broj u redu koji kaže 'ukupan broj grešaka'/'LRU: N grešaka'; okviri = poslednja grupa u zagradama."""
    totals = re.findall(r"(?i)(?:ukupan broj gre\w+[^\n\d]{0,40}|LRU:\s*)(\d+)", reply)
    groups = re.findall(r"[\[{]([A-Z\-_ ,]+)[\]}]", reply)
    final = {c for c in (groups[-1] if groups else "") if c.isalpha()}
    return [("9 gresaka", bool(totals) and totals[-1] == "9"), ("okviri E,B,G", final == {"E", "B", "G"})]


CHECKS = {
    "jan24_q6_rasporedjivanje": sched_check,
    "okt23_q9_disk": regex_check(("64000", r"64\.?000"), ("SSTF", r"125\D+222\D+360\D+25\b"), ("SCAN", r"(?<!C-)SCAN[^\n]*?25\D+125\D+222\D+360"),
                                 ("C-SCAN", r"C-SCAN[^\n]*?25\D+360\D+222\D+125")),
    "okt23_q10_inode": regex_check(("a:10+0", r"\b10\b"), ("b:1536", r"1536"), ("b:4 indeksna", r"1536\D{1,40}\b4\b"),
                                   ("c:262144", r"262[ .,]?144"), ("c:513", r"\b513\b")),
    "okt23_q8_adresa": regex_check(("16 neiskoriscenih", r"\b16\b"), ("15|15|18", r"15\D{1,12}15\D{1,12}18"), strip_index_labels=True),
    "okt23_q7_lru": lru_check,
    "jan24_q8_adresa2": regex_check(("9|9|9|12", r"9\D{1,12}9\D{1,12}9\D{1,12}12"), ("512 GB", r"512 ?GB|2\^39|2\*\*39"), strip_index_labels=True),
    "okt23_q2_daNe": verdict_check({"a": "Ne", "b": "Da", "c": "Da", "d": "Ne", "e": "Ne"}),
    "jan24_q7_daNe": verdict_check({"a": "Da", "b": "Ne", "c": "Da"}),
    "jun24_q3_teorija": lambda reply: [("bar 3 od: stanje/PC/SP/registri",
                                         sum(bool(re.search(p, reply, re.I)) for p in (r"stanje|state", r"brojač|brojac|counter|\bPC\b", r"stek|stack|\bSP\b", r"registr")) >= 3)],
}


def call(message, history, active, mode):
    body = {"message": message, "history": history[-6:], "activeTask": active, "subject": SUBJECT, "mode": mode}
    req = urllib.request.Request(URL, json.dumps(body).encode(), {"Content-Type": "application/json"})
    t = time.time()
    d = json.load(urllib.request.urlopen(req, timeout=900))
    return d, round(time.time() - t, 1)


def run(q, mode):
    text = q["text"]
    if mode != "plain" and q["id"] in TASK_IDS:
        d, sec = call("Prikaži rešenje", [{"role": "user", "content": text}], {"text": text, "level": 1}, mode)
    else:
        d, sec = call(text, [], None, mode)
    return d["reply"], d["intent"], sec


if __name__ == "__main__":
    out = sys.argv[1]
    modes = sys.argv[2].split(",") if len(sys.argv) > 2 and sys.argv[2] else MODES
    ids = set(sys.argv[3].split(",")) if len(sys.argv) > 3 else None
    results = []
    for mode in modes:
        for q in QUESTIONS:
            if ids and q["id"] not in ids:
                continue
            try:
                reply, intent, sec = run(q, mode)
                err = None
            except Exception as e:  # noqa: BLE001
                reply, intent, sec, err = "", None, 0, str(e)
            checks = CHECKS[q["id"]](reply) if q["id"] in CHECKS and not err else []
            results.append({"mode": mode, "id": q["id"], "seconds": sec, "intent": intent, "expected": q["expected"],
                            "checks": [{"name": n, "ok": ok} for n, ok in checks], "reply": reply, "error": err})
            json.dump(results, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
            passed = sum(ok for _, ok in checks)
            print(f"{mode:10} {q['id']:26} {sec:6}s  {passed}/{len(checks)}", flush=True)
