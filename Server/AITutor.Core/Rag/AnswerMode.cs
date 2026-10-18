namespace AITutor.Core.Rag;

// Rag: odgovor iz materijala predmeta uz programske alate za zadatke (podrazumevano, ceo sistem).
// RagNoTools: isto, ali bez alata (model sam rešava zadatke); Plain: samo opšte znanje modela, bez pretrage.
// Tri režima služe za poređenje u evaluaciji (plain / RAG / RAG + alati).
public enum AnswerMode { Rag, Plain, RagNoTools }
