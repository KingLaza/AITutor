namespace AITutor.Core.Rag;

using AITutor.Core.Data;

public class RagQueryService
{
    private readonly IEmbeddingService _embeddings;
    private readonly IChunkRepository _repository;
    private readonly IChatService _chat;

    private const int TopK = 4;
    private const double MaxDistance = 0.5; // starting tačka — podesi kad vidiš stvarne vrednosti

    public RagQueryService(IEmbeddingService embeddings, IChunkRepository repository, IChatService chat)
    {
        _embeddings = embeddings;
        _repository = repository;
        _chat = chat;
    }

    public class RagAnswer
    {
        public string Reply { get; set; } = "";
        public List<ChunkSearchResult> RetrievedChunks { get; set; } = new();
    }

    public async Task<RagAnswer> AnswerAsync(string question)
    {
        var queryEmbedding = await _embeddings.GetEmbeddingAsync(question, EmbeddingKind.Query);
        var results = await _repository.SearchAsync(queryEmbedding, TopK);

        if (results.Count == 0 || results.Min(r => r.Distance) > MaxDistance)
            return new RagAnswer { Reply = "Nemam dovoljno informacija u materijalu da odgovorim na ovo pitanje.", RetrievedChunks = results };

        var context = string.Join("\n\n---\n\n", results.Select(r => r.Content));
        var prompt = $"""
            Odgovaraj SAMO na osnovu sledećeg konteksta iz nastavnog materijala.
            Daj direktan, jasan odgovor u par rečenica. NEMOJ komentarisati kvalitet ili potpunost konteksta.
            Ako kontekst zaista ne sadrži dovoljno informacija za pouzdan odgovor, odgovori TAČNO sa:
            "Nemam dovoljno informacija u materijalu da odgovorim na ovo pitanje." i ništa više.

            KONTEKST:
            {context}

            PITANJE: {question}
            """;

        var reply = await _chat.GenerateAsync(prompt);
        return new RagAnswer { Reply = reply, RetrievedChunks = results };
    }
}
