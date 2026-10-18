using System.Text;
using System.Text.Json;

namespace AITutor.Core.Ollama;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaEmbeddingService(HttpClient http, OllamaOptions options)
    {
        _http = http;
        _options = options;
    }

    // embeddinggemma očekuje različit oblik za dokument i za upit.
    private static string Format(string text, EmbeddingKind kind) =>
        kind == EmbeddingKind.Query ? $"task: search result | query: {text}" : $"title: none | text: {text}";

    public async Task<float[]> GetEmbeddingAsync(string text, EmbeddingKind kind = EmbeddingKind.Document)
    {
        var body = JsonSerializer.Serialize(new { model = _options.EmbeddingModel, input = Format(text, kind) });
        var resp = await _http.PostAsync($"{_options.BaseUrl}/api/embed",
            new StringContent(body, Encoding.UTF8, "application/json"));
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("error", out var errorProp))
            throw new InvalidOperationException($"Ollama embedding greška: {errorProp.GetString()}");

        return doc.RootElement.GetProperty("embeddings")[0]
            .EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }
}
