#!/bin/bash
# Izvršava se samo pri prvom pokretanju (prazan volumen), posle 00-init.sql: učitava izvezenu bazu znanja iz /seed, ako postoji.
shopt -s nullglob
for f in /seed/*.sql.gz; do
  echo "AITutor: učitavam bazu znanja $f"
  gunzip -c "$f" | psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"
done
