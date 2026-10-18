namespace AITutor.Core.Rag;

// Rag: odgovor iz materijala predmeta (podrazumevano). Plain: model odgovara samo iz opšteg znanja, bez pretrage;
// to je mesto za kasnije poređenje odgovora bez RAG-a i sa RAG-om.
public enum AnswerMode { Rag, Plain }
