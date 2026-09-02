using System.Text;
using System.Text.Json;

namespace AITutor.Core.Ollama;

public class OllamaChatService : IChatService
{
    private readonly HttpClient _http;
    private const string Model = "llama3.1:8b";

    public OllamaChatService(HttpClient http) => _http = http;

    public async Task<string> GenerateAsync(string prompt)
    {
        var body = JsonSerializer.Serialize(new { model = Model, prompt, stream = false });
        var resp = await _http.PostAsync("http://localhost:11434/api/generate",
            new StringContent(body, Encoding.UTF8, "application/json"));
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("response").GetString() ?? "";
    }
}
