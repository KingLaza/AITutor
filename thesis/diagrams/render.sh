#!/usr/bin/env bash
# Pretvara Mermaid dijagrame (*.mmd) u PNG za rad (../figures). Potrebno: Node.js i instaliran Chrome.
# Putanja do Chrome-a se zadaje u puppeteer.json. Upotreba: bash render.sh
set -euo pipefail
cd "$(dirname "$0")"
export PUPPETEER_SKIP_DOWNLOAD=1
for f in *.mmd; do
  npx --yes @mermaid-js/mermaid-cli@11 -i "$f" -o "../figures/${f%.mmd}.png" -p puppeteer.json -b white -t neutral -s 2
done
