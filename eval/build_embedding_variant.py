"""Kopira chunkove iz `chunks` u novu tabelu i racuna embeddinge zadatim modelom.
Upotreba: build_variant.py <model> <tabela> <dim> <none|egemma>
Ne menja tabelu `chunks`. Moze da se prekine i nastavi (preskace vec upisane id-jeve)."""
import sys, json, subprocess, time, urllib.request

model, table, dim, style = sys.argv[1], sys.argv[2], int(sys.argv[3]), sys.argv[4]
assert table in ("chunks_bge", "chunks_egemma", "chunks_nomic2"), "nedozvoljeno ime tabele"


def psql(sql):
    r = subprocess.run(["docker", "exec", "-i", "Postgres", "psql", "-U", "postgres", "-At", "-v", "ON_ERROR_STOP=1"],
                       input=sql, capture_output=True, text=True, encoding="utf-8")
    if r.returncode != 0:
        raise RuntimeError(r.stderr)
    return r.stdout


def doc_text(content):
    return f"title: none | text: {content}" if style == "egemma" else content


def embed(text):
    req = urllib.request.Request("http://127.0.0.1:11434/api/embed",
                                 json.dumps({"model": model, "input": text}).encode(),
                                 {"Content-Type": "application/json"})
    return json.load(urllib.request.urlopen(req, timeout=600))["embeddings"][0]


psql(f"CREATE TABLE IF NOT EXISTS {table} (id int PRIMARY KEY, subject text, source_type text, source_file text, content text, embedding vector({dim}));")
rows = json.loads(psql("SELECT json_agg(json_build_object('id',id,'subject',subject,'source_type',source_type,'source_file',source_file,'content',content) ORDER BY id) FROM chunks;"))
done = {int(x) for x in psql(f"SELECT id FROM {table};").split()}
todo = [r for r in rows if r["id"] not in done]
print(f"{model} -> {table}: ukupno {len(rows)}, preostalo {len(todo)}", flush=True)

t0 = time.time()
batch = []


def flush():
    global batch
    if not batch:
        return
    psql("\n".join(batch))
    batch = []


def q(s):  # dollar-quote sa nasumicnom oznakom
    tag = "$qq$"
    assert tag not in s
    return f"{tag}{s}{tag}"


for i, r in enumerate(todo, 1):
    v = embed(doc_text(r["content"]))
    assert len(v) == dim
    vec = "[" + ",".join(f"{x:.6f}" for x in v) + "]"
    batch.append(f"INSERT INTO {table} (id, subject, source_type, source_file, content, embedding) VALUES "
                 f"({r['id']}, {q(r['subject'] or '')}, {q(r['source_type'])}, {q(r['source_file'] or '')}, {q(r['content'])}, '{vec}');")
    if len(batch) >= 20:
        flush()
    if i % 40 == 0:
        print(f"  {i}/{len(todo)}  {time.time() - t0:.0f}s", flush=True)
flush()
psql(f"CREATE INDEX IF NOT EXISTS {table}_hnsw ON {table} USING hnsw (embedding vector_cosine_ops);")
print(f"gotovo za {time.time() - t0:.0f}s, redova u {table}: {psql(f'SELECT count(*) FROM {table};').strip()}", flush=True)
