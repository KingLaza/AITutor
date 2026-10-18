"""Benchmark celog sistema kroz /api/chat.
Obicne poruke: jedan poziv. Zadaci: tri poziva (nagovestaj 1, nagovestaj 2, resenje) uz istoriju i activeTask.
Pitanja sa istorijom: niz poruka u istom razgovoru.
Upotreba: chat_bench.py <izlazni.json>"""
import sys, json, time, urllib.request

URL = "http://127.0.0.1:5000/api/chat"

SINGLE = [
    ("casual", "Cao"),
    ("casual", "Sta sve umes i kako mi mozes pomoci oko ispita?"),
    ("subject", "Sta je LL(1) gramatika?"),
    ("subject", "Koja je razlika izmedju kompilatora i interpretatora?"),
    ("subject_border", "Koje su faze kompajliranja?"),
    ("subject_border", "Sta je rekurzivni spust?"),
    ("offtopic", "Sta je nuklearna fizika?"),
    ("offtopic", "Koji je glavni grad Francuske?"),
    ("offtopic_hard", "Sta je TCP protokol?"),
    ("offtopic_hard", "Kako radi SQL JOIN?"),
]
CONVERSATIONS = [
    ("followup", ["Sta je FIRST skup?", "a FOLLOW?", "hvala"]),
]
TASKS = [
    ("task_in_material", "Definisati klasu za while petlju u AST-u i napisati metodu translate()"),
    ("task_variant", "Definisati klasu za predstavljanje do-while petlje u AST-u, definisana smenom DoWhileStatement -> DO Statement WHILE Expression, i implementirati translate"),
    ("task_ll1", "Transformisati datu gramatiku u LL(1) gramatiku i dokazati da je LL(1): S -> S a | b"),
    ("task_cup", "Napisati cup specifikaciju za sintaksni analizator jezika sa if-else naredbom"),
    ("task_ar", "Za datu C funkciju int f(int n) { if (n<2) return 1; return n*f(n-1); } nacrtati aktivacioni slog i napisati asembler za IA-32"),
]

results = []


def call(message, history, active):
    t = time.time()
    try:
        req = urllib.request.Request(URL, json.dumps({"message": message, "history": history[-6:], "activeTask": active}).encode(),
                                     {"Content-Type": "application/json"})
        d = json.load(urllib.request.urlopen(req, timeout=900))
        return d, round(time.time() - t, 1), None
    except Exception as e:
        return None, round(time.time() - t, 1), str(e)


def record(kind, message, d, sec, err):
    r = {"kind": kind, "message": message, "seconds": sec}
    if err:
        r["error"] = err
    else:
        r.update(intent=d["intent"], reply=d["reply"], sources=d["sources"],
                 hintLevel=(d["activeTask"] or {}).get("level"))
    results.append(r)
    json.dump(results, open(out_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(f"{kind:22} {sec:7}s  {r.get('intent', 'ERR')}", flush=True)


out_path = sys.argv[1]

for kind, msg in SINGLE:
    d, sec, err = call(msg, [], None)
    record(kind, msg, d, sec, err)

for kind, msgs in CONVERSATIONS:
    history, active = [], None
    for i, msg in enumerate(msgs):
        d, sec, err = call(msg, history, active)
        record(f"{kind}_{i + 1}", msg, d, sec, err)
        if err:
            break
        history += [{"role": "user", "content": msg}, {"role": "assistant", "content": d["reply"]}]
        active = d["activeTask"]

for kind, msg in TASKS:
    history, active = [], None
    for label, text in (("L1", msg), ("L2", "Sledeći nagoveštaj"), ("L3", "Prikaži rešenje")):
        d, sec, err = call(text, history, active)
        record(f"{kind}_{label}", text, d, sec, err)
        if err:
            break
        history += [{"role": "user", "content": text}, {"role": "assistant", "content": d["reply"]}]
        active = d["activeTask"]
