"""Pravi izveštaj iz sirovog izlaza ablation.py: ponovo računa automatske provere, dodaje ručne ocene (MANUAL) i čuva
očišćenu verziju (bez doslovnih izvoda iz materijala kursa) u results/. Upotreba: ablation_report.py <sirovo.json> <izlaz.json> [ponovljeno.json]"""
import sys, json, re
sys.path.insert(0, __file__.rsplit("ablation_report", 1)[0])
import ablation

# Ručne ocene gde regex ne može da oceni (kod monitora, teorija, raspoređivanje u slobodnom tekstu): ok | partial | wrong.
# Dopunjuje se posle čitanja svakog odgovora; ostalo se ocenjuje automatski.
MANUAL: dict[tuple[str, str], str] = {
    # monitor Switch: oba RAG režima dobijaju proveren obrazac iz kartica (korektno rešenje); plain ima ideju (promenljiva "turn"),
    # ali koristi izmišljene identifikatore (ID_za_ovaj_proces) i notaciju koja nije iz kursa.
    ("rag", "jan24_q4_monitor"): "ok",
    ("ragnotools", "jan24_q4_monitor"): "ok",
    ("plain", "jan24_q4_monitor"): "partial",
    # teorija: plain navodi registre, PC i adresni prostor (deo tačnih stavki, nema stanja/steka)
    ("plain", "jun24_q3_teorija"): "partial",
}

MODE_NAMES = {"plain": "plain", "ragnotools": "RAG", "rag": "RAG + alati"}


def redact(reply: str) -> str:
    """Odgovori sa verbatim primerom iz materijala: ostavlja se samo deo koji je napisao sistem."""
    return re.split(r"\n+---\nRešen primer iz materijala", reply)[0].strip()


def auto_grade(checks):
    if not checks:
        return None
    ok = sum(c["ok"] for c in checks)
    return "ok" if ok == len(checks) else ("partial" if ok else "wrong")


def main(raw_path, out_path, fix_path=None):
    raw = json.load(open(raw_path, encoding="utf-8"))
    if fix_path:  # ponovljeni zadaci posle ispravke (zamenjuju originalne rezultate istog režima i zadatka)
        fixed = {(x["mode"], x["id"]): x for x in json.load(open(fix_path, encoding="utf-8"))}
        raw = [fixed.get((x["mode"], x["id"]), x) for x in raw]
    out = []
    for x in raw:
        checks = ablation.CHECKS[x["id"]](x["reply"]) if x["id"] in ablation.CHECKS and not x["error"] else []
        x["checks"] = [{"name": n, "ok": ok} for n, ok in checks]
        x["grade"] = MANUAL.get((x["mode"], x["id"])) or auto_grade(x["checks"])
        x["reply"] = redact(x["reply"])
        out.append(x)
    json.dump(out, open(out_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)

    ids = list(dict.fromkeys(x["id"] for x in out))
    modes = [m for m in MODE_NAMES if any(x["mode"] == m for x in out)]
    print(f"{'zadatak':28}" + "".join(f"{MODE_NAMES[m]:>16}" for m in modes))
    totals = {m: [0, 0.0, 0] for m in modes}
    for i in ids:
        row = f"{i:28}"
        for m in modes:
            x = next((r for r in out if r['mode'] == m and r['id'] == i), None)
            if not x:
                row += f"{'-':>16}"
                continue
            c = x["checks"]
            cell = f"{x['grade'] or '?'}" + (f" {sum(k['ok'] for k in c)}/{len(c)}" if c else "")
            row += f"{cell:>16}"
            totals[m][2] += 1
            totals[m][0] += x["grade"] == "ok"
            totals[m][1] += x["seconds"]
        print(row)
    print()
    for m in modes:
        ok, sec, n = totals[m]
        parts = sum(x["grade"] == "partial" for x in out if x["mode"] == m)
        print(f"{MODE_NAMES[m]:12} tačno {ok}/{n}, delimično {parts}, prosečno vreme {sec / max(n, 1):.0f}s")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None)
