# Materijal za završni rad

Radni folder za pisanje rada (ne ulazi u build aplikacije).

| Fajl | Sadržaj |
|---|---|
| [outline.md](outline.md) | Struktura rada, istraživačka pitanja, šta svako poglavlje koristi iz koda i rezultata |
| [literature.md](literature.md) | Pregled literature sa oznakom pouzdanosti, lista za čitanje, šta se ne sme tvrditi |
| [evaluation-protocol.md](evaluation-protocol.md) | Plan evaluacije: razvojni i test skup, ponavljanja, metrike, poređenje sa ChatGPT/Claude, pretnje validnosti |
| [results-so-far.md](results-so-far.md) | Šta je već izmereno, gde su sirovi podaci i čime se ne sme tvrditi |
| [privacy-threat-model.md](privacy-threat-model.md) | Nacrt modela pretnji: šta ostaje lokalno |
| [mentor-questions.md](mentor-questions.md) | Otvorena pitanja za mentora |
| `draft/` | Nacrti poglavlja (Markdown): 02 i 03 iz claude.ai chata, 04-06 napisao Claude Code |
| `literatura.bib`, `ieee-svi-autori.csl` | Literatura (BibTeX) i IEEE stil koji ispisuje sve autore |
| `PROVERITI.md` | Šta u tekstu ne počiva na izvoru koji je neko otvorio; proveriti pre predaje |
| `diagrams/*.mmd`, `figures/` | Mermaid izvori dijagrama i slike za rad |
| `build.sh`, `make_reference_docx.py`, `count.sh` | Sastavljanje `rad.docx` i brojanje reči |

## Sastavljanje rada

Potrebno: pandoc (>= 3), Python sa `python-docx`. Dijagrami: Node.js i Chrome (`bash diagrams/render.sh`, putanja do Chrome-a u `diagrams/puppeteer.json`).

```
bash thesis/diagrams/render.sh   # .mmd -> figures/*.png (samo kad se dijagram menja)
bash thesis/build.sh             # draft/*.md -> rad.docx (stilovi po pravilima fakulteta)
bash thesis/count.sh             # reči po poglavlju
```

`rad.docx` i `reference.docx` su generisani i nisu u repou. Snimci ekrana `figures/preview-*.jpg` su privremeni (niska rezolucija) i zamenjuju se konačnim.

Izvor strukture i literature: istraživački izveštaj (claude.ai, 21.10.2026). Oznake pouzdanosti: **P** primarni izvor otvoren, **S** sekundarni ili samo naslov,
**M** po sećanju, proveriti pre citiranja. Sve što je M ili S mora se proveriti u originalu pre nego što uđe u rad.
