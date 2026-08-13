using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace AITutor_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    // private readonly HttpClient _http = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(3) };

    public record ChatRequest(string Message);
    public record ChatResponse(string Reply);

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post(ChatRequest req)
    {
        var body = JsonSerializer.Serialize(new { model = "llama3.1:8b", prompt = req.Message, stream = false });
        var resp = await _http.PostAsync("http://localhost:11434/api/generate",
            new StringContent(body, Encoding.UTF8, "application/json"));
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var reply = doc.RootElement.GetProperty("response").GetString();
        return Ok(new ChatResponse(reply ?? ""));
    }
}
