namespace AITutor.Core.Ollama;

// Think=false gasi "thinking" režim (npr. Gemma 4): inače model potroši ceo limit tokena na skriveno razmišljanje.
// ChatNumCtx je isti za sve chat pozive: promena num_ctx između poziva tera Ollamu da ponovo učita model (~20-30 s).
// BaseUrl koristi 127.0.0.1 jer "localhost" na ovoj mašini dodaje ~2 s po zahtevu (IPv6 pokušaj).
public record OllamaOptions(
    string ChatModel = "gemma4:e4b",
    string EmbeddingModel = "embeddinggemma",
    string BaseUrl = "http://127.0.0.1:11434",
    bool? Think = false,
    int ChatNumCtx = 6144);
