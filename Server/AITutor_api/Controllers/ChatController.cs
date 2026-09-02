using Microsoft.AspNetCore.Mvc;
using AITutor.Core;

namespace AITutor_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chat;

    public ChatController(IChatService chat) => _chat = chat;

    public record ChatRequest(string Message);
    public record ChatResponse(string Reply);

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post(ChatRequest req)
    {
        var reply = await _chat.GenerateAsync(req.Message);
        return Ok(new ChatResponse(reply));
    }
}
