using System.Text;
using System.Text.Json;

namespace AITutor.Core.Ollama;

public class OllamaChatService : IChatService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly bool? _think;
    private readonly string _baseUrl;
    private readonly int _numCtx;

    public OllamaChatService(HttpClient http, OllamaOptions options)
    {
        _http = http;
        _baseUrl = options.BaseUrl;
        _numCtx = options.ChatNumCtx;
        _model = options.ChatModel;
        _think = options.Think;
    }

    public async Task<string> GenerateAsync(string prompt, ChatOptions? options = null)
    {
        options ??= new ChatOptions();
        var ollamaOptions = new Dictionary<string, object> { ["temperature"] = options.Temperature };
        if (options.MaxTokens is int max) ollamaOptions["num_predict"] = max;
        ollamaOptions["num_ctx"] = options.NumCtx ?? _numCtx;

        var request = new Dictionary<string, object> { ["model"] = _model, ["prompt"] = prompt, ["stream"] = false, ["options"] = ollamaOptions };
        if (_think is bool think) request["think"] = think;
        var body = JsonSerializer.Serialize(request);
        var resp = await _http.PostAsync($"{_baseUrl}/api/generate",
            new StringContent(body, Encoding.UTF8, "application/json"));
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("error", out var err))
            throw new InvalidOperationException($"Ollama chat greška: {err.GetString()}");

        return doc.RootElement.GetProperty("response").GetString() ?? "";
    }
}
