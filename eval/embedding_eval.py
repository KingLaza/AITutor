"""Poredi embedding modele samo po kvalitetu pretrage (bez chat modela).
Metrike: hit@1, hit@4, MRR@10 za pitanja iz domena; AUC i najbolji prag za razdvajanje
pitanja iz domena od pitanja van domena po razdaljini najblizeg chunka."""
import re, sys, json, subprocess, urllib.request

IN_DOMAIN = [
    ("Sta je LL(1) gramatika?", r"LL\(1\)"),
    ("Sta znaci da je gramatika LL(1)?", r"LL\(1\)"),
    ("Koja je razlika izmedju kompilatora i interpretatora?", r"PP 01|100_pitanja|skripta"),
    ("Objasni mi razliku izmedju prevodioca i interpretera", r"PP 01|100_pitanja|skripta"),
    ("Sta je FIRST i FOLLOW skup?", r"FIRST|LL\(1\)"),
    ("Cemu sluzi tabela simbola?", r"PP 09|Tabele simbola|100_pitanja"),
    ("Sta je aktivacioni slog?", r"Upravljanje|Zadatak ?[1-4]|PP 12|100_pitanja"),
    ("Kako radi leksicka analiza?", r"PP 04|Leksick|JFlex|100_pitanja"),
    ("Sta je LR parser?", r"PP 07|LR|100_pitanja"),
    ("Sta je operatorska gramatika?", r"PP 08|Operatorske|100_pitanja"),
    ("Koje su faze kompajliranja?", r"PP 01|100_pitanja"),
    ("Sta je medjukod i zasto se koristi?", r"PP 10|edjukod|100_pitanja"),
    ("Sta je optimizacija koda?", r"PP 11|100_pitanja"),
    ("Sta je JFlex?", r"JFlex|Leksick|pecifikacija"),
    ("Sta je CUP?", r"cup"),
    ("Definisati klasu za while petlju u AST-u i napisati metodu translate()", r"While petlja|Generisanje medjukoda|AST u medjukod|PP 10"),
    ("Kako se generise medjukod za while petlju?", r"While petlja|Generisanje medjukoda|PP 10"),
    ("Transformisati datu gramatiku u LL(1) gramatiku i dokazati da je LL(1): S -> S a | b", r"LL\(1\)"),
    ("Napisati cup specifikaciju za sintaksni analizator jezika sa if-else naredbom", r"cup|PP08"),
    ("Za datu C funkciju nacrtati aktivacioni slog i napisati asembler za IA-32", r"Zadatak ?[1-4]|Upravljanje|PP_09|PP 12"),
]
IN_DOMAIN += [
    ("Sta je konacni automat?", r"PP01_Formalne|PP 03|PP 02|Uredjaji|100_pitanja"),
    ("Sta je regularni izraz?", r"PP 02|PP 03|PP 04|Leksick|JFlex|PP01_Formalne|PP02_Leksicka|100_pitanja"),
    ("Objasni sintaksnu analizu", r"PP 05|Sintaks|PP 06|PP 07|100_pitanja"),
    ("Sta je semanticka analiza?", r"PP 09|Semanticka|Tabele simbola|100_pitanja"),
    ("Kako se pravi tabela parsiranja?", r"LL\(1\)|LR|PP 06|PP 07|PP04|PP05"),
    ("Sta je generisanje izlaznog koda?", r"PP 12|Generisanje|100_pitanja"),
    ("Sta je rekurzivni spust?", r"PP 05|PP 06|Top-down|LL\(1\)|100_pitanja"),
]
OFF_HARD = [
    "Sta je TCP protokol?", "Kako radi SQL JOIN?", "Objasni deadlock u operativnim sistemima",
    "Sta je neuronska mreza?", "Kako se pise petlja u JavaScript-u?", "Sta je B-stablo?",
    "Sta je normalizacija baze podataka?", "Objasni Dijkstrin algoritam", "Sta je HTTP?",
    "Kako funkcionise virtuelna memorija?", "Sta je objektno orijentisano programiranje?",
]
OFF_TOPIC = [
    "Sta je nuklearna fizika?", "Koji je glavni grad Francuske?", "Ko je napisao Hamleta?",
    "Kako se prave palacinke?", "Ko je pobedio na svetskom prvenstvu 2018?", "Objasni fotosintezu",
    "Koliko je visok Mont Everest?", "Preporuci mi dobar film za vikend",
]

VARIANTS = [
    # naziv, tabela, model, endpoint, prefiks upita
    ("nomic-embed-text", "chunks_nomic", "nomic-embed-text", "old", "search_query: "),
    ("embeddinggemma", "chunks", "embeddinggemma", "new", "task: search result | query: "),
    ("bge-m3", "chunks_bge", "bge-m3", "new", ""),
]


def psql(sql):
    r = subprocess.run(["docker", "exec", "-i", "Postgres", "psql", "-U", "postgres", "-At", "-F", "\t"],
                       input=sql, capture_output=True, text=True, encoding="utf-8")
    if r.returncode != 0:
        raise RuntimeError(r.stderr)
    return r.stdout


def embed(model, endpoint, text):
    if endpoint == "old":
        req = urllib.request.Request("http://127.0.0.1:11434/api/embeddings",
                                     json.dumps({"model": model, "prompt": text}).encode(), {"Content-Type": "application/json"})
        return json.load(urllib.request.urlopen(req, timeout=600))["embedding"]
    req = urllib.request.Request("http://127.0.0.1:11434/api/embed",
                                 json.dumps({"model": model, "input": text}).encode(), {"Content-Type": "application/json"})
    return json.load(urllib.request.urlopen(req, timeout=600))["embeddings"][0]


def top(table, vec, k=10):
    v = "[" + ",".join(f"{x:.6f}" for x in vec) + "]"
    out = psql(f"SELECT source_file, embedding <=> '{v}' d FROM {table} ORDER BY embedding <=> '{v}' LIMIT {k};")
    return [(l.split("\t")[0], float(l.split("\t")[1])) for l in out.strip().splitlines()]


def auc(pos, neg):  # P(neg > pos): vece razdaljine kod pitanja van domena
    wins = sum((n > p) + 0.5 * (n == p) for p in pos for n in neg)
    return wins / (len(pos) * len(neg))


def best_threshold(pos, neg):
    best = (0, None)
    for t in sorted(pos + neg):
        bal = (sum(p <= t for p in pos) / len(pos) + sum(n > t for n in neg) / len(neg)) / 2
        if bal > best[0]:
            best = (bal, t)
    return best


summary, details, extra = [], {}, {}
for name, table, model, endpoint, qprefix in VARIANTS:
    h1 = h4 = mrr = 0
    in_min, off_min, rows = [], [], []
    for q, pat in IN_DOMAIN:
        res = top(table, embed(model, endpoint, qprefix + q))
        ranks = [i for i, (f, _) in enumerate(res, 1) if re.search(pat, f, re.I)]
        r = ranks[0] if ranks else None
        h1 += r == 1
        h4 += bool(r and r <= 4)
        mrr += 1 / r if r else 0
        in_min.append(res[0][1])
        rows.append((q[:55], r, round(res[0][1], 3), res[0][0][:32]))
    import time
    off_easy = [top(table, embed(model, endpoint, qprefix + q), 1)[0][1] for q in OFF_TOPIC]
    off_hard = [top(table, embed(model, endpoint, qprefix + q), 1)[0][1] for q in OFF_HARD]
    off_min = off_easy + off_hard
    t0 = time.time()
    for q in ("Sta je LL(1) gramatika?", "Koje su faze kompajliranja?", "Sta je tabela simbola?"):
        embed(model, endpoint, qprefix + q)
    qlat = (time.time() - t0) / 3
    n = len(IN_DOMAIN)
    bal, thr = best_threshold(in_min, off_min)
    margin = min(off_min) - max(in_min)
    extra[name] = dict(auc_hard=auc(in_min, off_hard), auc_easy=auc(in_min, off_easy), margin=margin,
                       mid=(min(off_min) + max(in_min)) / 2, qlat=qlat,
                       worst_in=sorted(zip(in_min, [x[0] for x in IN_DOMAIN]))[-2:], best_off=sorted(zip(off_hard + off_easy, OFF_HARD + OFF_TOPIC))[:3])
    summary.append((name, h1 / n, h4 / n, mrr / n, auc(in_min, off_min), bal, thr,
                    sum(in_min) / n, sum(off_min) / len(off_min), min(off_min), max(in_min)))
    details[name] = rows

print(f"{'model':18} {'hit@1':>6} {'hit@4':>6} {'MRR':>6} {'AUC':>6} {'balAcc':>7} {'prag':>6} {'avg_in':>7} {'avg_off':>8} {'min_off':>8} {'max_in':>7}")
for s in summary:
    print(f"{s[0]:18} {s[1]:6.2f} {s[2]:6.2f} {s[3]:6.2f} {s[4]:6.2f} {s[5]:7.2f} {s[6]:6.3f} {s[7]:7.3f} {s[8]:8.3f} {s[9]:8.3f} {s[10]:7.3f}")
print()
for name, e in extra.items():
    print(f"{name:18} AUC(tezi negativi)={e['auc_hard']:.2f} AUC(laksi)={e['auc_easy']:.2f}  razmak(min_van - max_in)={e['margin']:+.3f}  sredina={e['mid']:.3f}  upit={e['qlat']*1000:.0f}ms")
    print(f"   najveca razdaljina iz domena: {[(round(d,3), q[:40]) for d, q in e['worst_in']]}")
    print(f"   najbliza pitanja van domena:  {[(round(d,3), q[:40]) for d, q in e['best_off']]}")
print()
for name, rows in details.items():
    print(f"--- {name}: rang prvog pogotka (None = nema u top 10)")
    for r in rows:
        print("  ", r)
