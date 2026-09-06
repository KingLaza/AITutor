using Microsoft.AspNetCore.Mvc;
using AITutor.Core.Rag;

namespace AITutor_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly RagQueryService _rag;

    public ChatController(RagQueryService rag) => _rag = rag;

    public record ChatRequest(string Message);
    public record ChatResponse(string Reply, List<string> Sources);

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post(ChatRequest req)
    {
        var result = await _rag.AnswerAsync(req.Message);
        var sources = result.RetrievedChunks.Select(c => $"{c.SourceFile} (dist: {c.Distance:F3})").ToList();
        return Ok(new ChatResponse(result.Reply, sources));
    }
}