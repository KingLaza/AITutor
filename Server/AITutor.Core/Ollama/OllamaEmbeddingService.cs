using System.Text;
using System.Text.Json;

namespace AITutor.Core.Ollama;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private const string Model = "nomic-embed-text";

    public OllamaEmbeddingService(HttpClient http) => _http = http;

    public async Task<float[]> GetEmbeddingAsync(string text, EmbeddingKind kind = EmbeddingKind.Document)
    {
        var prefix = kind == EmbeddingKind.Query ? "search_query: " : "search_document: ";
        var body = JsonSerializer.Serialize(new
        {
            model = Model,
            prompt = prefix + text,
            options = new { num_ctx = 8192 }
        });
        var resp = await _http.PostAsync("http://localhost:11434/api/embeddings",
            new StringContent(body, Encoding.UTF8, "application/json"));
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("error", out var errorProp))
            throw new InvalidOperationException($"Ollama embedding greška: {errorProp.GetString()}");

        return doc.RootElement.GetProperty("embedding")
            .EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }
}
