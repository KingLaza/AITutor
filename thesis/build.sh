#!/usr/bin/env bash
# Sastavlja rad iz Markdown poglavlja u Word (.docx) po pravilima fakulteta.
# Potrebno: pandoc (>= 3), python3 + python-docx. Opciono: libreoffice za PDF i brojanje strana.
# Upotreba: bash build.sh            -> rad.docx
set -euo pipefail
cd "$(dirname "$0")"
PY="$(command -v python3 || command -v python)"
[ -f reference.docx ] || "$PY" make_reference_docx.py reference.docx
printf '# Literatura\n\n::: {#refs}\n:::\n' > draft/99-literatura.md
# lang=en-US: citeproc tada ispisuje IEEE termine na engleskom ("and", "Accessed:", "[Online]. Available:"), kako i trazi uputstvo.
pandoc draft/[0-9][0-9]-*.md \
  --citeproc --bibliography=literatura.bib --csl=ieee-svi-autori.csl \
  --reference-doc=reference.docx --metadata lang=en-US \
  -o rad.docx
echo "Napravljeno: rad.docx"
if command -v soffice >/dev/null; then
  soffice --headless --convert-to pdf rad.docx >/dev/null 2>&1 && \
  python3 -c "import pypdf;print('Broj strana (priblizno, LibreOffice):',len(pypdf.PdfReader('rad.pdf').pages))"
fi
