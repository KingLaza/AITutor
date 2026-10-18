"""Meri razdaljinu najbližeg chunka za pitanja iz domena i van domena za dati predmet i predlaže prag.
Upotreba: threshold_eval.py "Operativni sistemi" """
import sys, json, subprocess, urllib.request

SUBJECT = sys.argv[1] if len(sys.argv) > 1 else "Operativni sistemi"
KNOWLEDGE = ("predavanja", "vezbe", "usmeni", "reseni_blanketi")

IN_DOMAIN = {
    "Operativni sistemi": [
        "Sta je proces i koja su njegova stanja?", "Koja je razlika izmedju procesa i niti?", "Sta je semafor?",
        "Kako radi monitor?", "Sta je uzajamno blokiranje (deadlock)?", "Sta je Bankerov algoritam?",
        "Kako radi Round Robin raspoređivanje?", "Sta je virtuelna memorija?", "Sta je stranicenje?",
        "Sta je zamena stranica LRU?", "Sta je i-node?", "Sta je sistemski poziv?", "Sta radi fork()?",
        "Sta je datavod (pipe)?", "Sta je deljena memorija?", "Sta je context switch?",
        "Koliko pristupa disku je potrebno za citanje bloka datoteke u Unix fajl sistemu?",
        "Sta je SCAN algoritam za raspoređivanje diska?",
        "Napisati shell skriptu koja prebroji linije u svim .c fajlovima",
        "U sistemu se izvrsava 4 procesa A, B, C, D. Koji od procesa ce se izvrsavati u periodu 60-70ms po SJF i SRTF?",
        "Fajl sistem Unix System V ima blokove velicine 4KB i i-node sa 10 direktnih adresa. Koliko blokova je potrebno za datoteku od 6MB?",
        "Procesu su dodeljena 3 stranicna okvira. Odrediti broj gresaka stranica za LRU: EDHBDEDAEBEDEBG",
        "Nacrtati format virtuelne adrese u sistemu sa segmentacijom i stranicenjem u dva nivoa",
        "Napisati monitor koji procesima P1 i P2 obezbedjuje naizmenicno setovanje promenljive",
    ],
}
OFF_TOPIC = [
    "Sta je nuklearna fizika?", "Koji je glavni grad Francuske?", "Ko je napisao Hamleta?", "Kako se prave palacinke?",
    "Ko je pobedio na svetskom prvenstvu 2018?", "Objasni fotosintezu", "Koliko je visok Mont Everest?",
    "Preporuci mi dobar film za vikend", "Sta je TCP protokol?", "Kako radi SQL JOIN?", "Sta je neuronska mreza?",
    "Objasni Dijkstrin algoritam", "Sta je HTTP?", "Sta je objektno orijentisano programiranje?",
    "Sta je B-stablo?", "Kako se pise petlja u JavaScript-u?", "Sta je kompajler?", "Sta je LL(1) gramatika?",
]


def psql(sql):
    r = subprocess.run(["docker", "exec", "-i", "Postgres", "psql", "-U", "postgres", "-At"], input=sql,
                       capture_output=True, text=True, encoding="utf-8")
    if r.returncode:
        raise RuntimeError(r.stderr)
    return r.stdout


def embed(text):
    body = {"model": "embeddinggemma", "input": "task: search result | query: " + text}
    req = urllib.request.Request("http://127.0.0.1:11434/api/embed", json.dumps(body).encode(), {"Content-Type": "application/json"})
    return json.load(urllib.request.urlopen(req, timeout=300))["embeddings"][0]


def min_dist(text):
    v = "[" + ",".join(f"{x:.6f}" for x in embed(text)) + "]"
    types = ",".join(f"'{t}'" for t in KNOWLEDGE)
    out = psql(f"SELECT min(embedding <=> '{v}') FROM chunks WHERE lower(subject) = lower('{SUBJECT}') AND source_type IN ({types});")
    return float(out.strip())


def auc(pos, neg):
    return sum((n > p) + 0.5 * (n == p) for p in pos for n in neg) / (len(pos) * len(neg))


pos = [(q, min_dist(q)) for q in IN_DOMAIN[SUBJECT]]
neg = [(q, min_dist(q)) for q in OFF_TOPIC]
pd, nd = [d for _, d in pos], [d for _, d in neg]
print(f"{SUBJECT}: iz domena n={len(pd)} prosek={sum(pd)/len(pd):.3f} max={max(pd):.3f} | van domena n={len(nd)} prosek={sum(nd)/len(nd):.3f} min={min(nd):.3f} | AUC={auc(pd, nd):.3f}")
print("najdalje iz domena:", [(round(d, 3), q[:50]) for q, d in sorted(pos, key=lambda x: -x[1])[:4]])
print("najblize van domena:", [(round(d, 3), q[:50]) for q, d in sorted(neg, key=lambda x: x[1])[:4]])
best = max(((sum(p <= t for p in pd) / len(pd) + sum(n > t for n in nd) / len(nd)) / 2, t) for t in sorted(pd + nd))
print(f"najbolji prag (balansirana tacnost {best[0]:.2f}): {best[1]:.3f}; sredina izmedju max_in i min_off: {(max(pd) + min(nd)) / 2:.3f}")
