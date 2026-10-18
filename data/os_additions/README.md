# Dodaci za predmet Operativni sistemi

Sadržaj koji je napisao autor projekta (nije materijal kursa) i koji se ubacuje u bazu pored materijala kursa:

- `obrasci/` (source_type `obrasci`): 16 proverenih obrazaca za zadatke sa semaforima i monitorima u notaciji predavanja.
- `usmeni/` (source_type `usmeni`): 15 kratkih činjenica o modovima, stanjima procesa, nitima, straničenju.

U izvorima odgovora se vide kao `obrasci/...` i `usmeni/Kratke cinjenice (dodato u AITutor...)`.
Za poređenje "čist RAG" i "RAG sa dodacima" dovoljno je obrisati te redove iz tabele `chunks`
(`DELETE FROM chunks WHERE subject = 'Operativni sistemi' AND source_type IN ('obrasci', 'usmeni')`).

Ingest: `POST /api/ingest` sa putanjom do ovog foldera (podfolderi su nazivi tipova izvora) i subject `Operativni sistemi`.
