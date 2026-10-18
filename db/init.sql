-- Izvršava se samo pri prvom pokretanju (prazan volumen baze).
-- Dimenzija vektora (768) odgovara embedding modelu embeddinggemma; za drugi model treba promeniti i ponovo ingestovati materijal.
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS chunks (
    id          SERIAL PRIMARY KEY,
    subject     TEXT NOT NULL,
    source_type TEXT NOT NULL,
    source_file TEXT,
    content     TEXT NOT NULL,
    embedding   vector(768),
    created_at  TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX IF NOT EXISTS chunks_embedding_hnsw ON chunks USING hnsw (embedding vector_cosine_ops);
CREATE INDEX IF NOT EXISTS chunks_subject_idx ON chunks (subject);
