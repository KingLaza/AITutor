#!/bin/bash
# Izvozi bazu znanja (već ugrađeni vektori) u db/seed/knowledge.sql.gz; ko je dobije, ima gotov materijal pri prvom pokretanju.
# Ispiti ("pismeni") i studentski upload ("moje") se ne izvoze. Upotreba: scripts/export-knowledge.sh [kontejner] [korisnik] [baza]
# Pažnja: dump sadrži tekst materijala kursa; deli ga samo uz dozvolu autora (vidi README).
set -euo pipefail
CONTAINER="${1:-aitutor-stack-db-1}"
DB_USER="${2:-postgres}"
DB_NAME="${3:-postgres}"
OUT="$(dirname "$0")/../db/seed/knowledge.sql.gz"
{
  echo "COPY chunks (subject, source_type, source_file, content, embedding) FROM stdin;"
  docker exec "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -c \
    "COPY (SELECT subject, source_type, source_file, content, embedding FROM chunks WHERE source_type NOT IN ('pismeni','moje') ORDER BY id) TO STDOUT"
  echo '\.'
} | gzip > "$OUT"
echo "Izvezeno u $OUT ($(du -h "$OUT" | cut -f1))"
