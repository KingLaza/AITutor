#!/usr/bin/env bash
# Broj reči po poglavlju nacrta; za grubu procenu obima (oko 450-500 reči po strani uz tabele i slike).
cd "$(dirname "$0")"
total=0
for f in draft/0[0-9]-*.md; do
  n=$(wc -w < "$f"); total=$((total + n)); printf "%-36s %6d\n" "$f" "$n"
done
printf "%-36s %6d\n" "ukupno" "$total"
