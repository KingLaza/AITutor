using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using AITutor.Core.Data;
using AITutor.Core.Rag;

namespace AITutor_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly RagQueryService _rag;
    private readonly IProfileRepository _profiles;

    public ChatController(RagQueryService rag, IProfileRepository profiles)
    {
        _rag = rag;
        _profiles = profiles;
    }

    // History: raniji razgovor (klijent šalje zadnjih par poruka). ActiveTask: zadatak na kome student radi i nivo nagoveštaja.
    public record ChatRequest(string Message, List<ChatTurn>? History = null, TaskState? ActiveTask = null, string? Subject = null, string? Mode = null, int? ProfileId = null);
    public record ChatResponse(string Reply, List<string> Sources, string Intent, TaskState? ActiveTask);

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post(ChatRequest req)
    {
        var result = await _rag.AnswerAsync(req.Message, req.History, req.ActiveTask, req.Subject,
            Enum.TryParse<AnswerMode>(req.Mode, ignoreCase: true, out var mode) ? mode : AnswerMode.Rag);
        var sources = result.RetrievedChunks.Select(c => $"{c.SourceType}/{c.SourceFile} " + (c.IsNeighbor ? "(nastavak)" : $"(dist: {c.Distance:F3})")).ToList();
        // Dok student radi na zadatku, aktivni zadatak se čuva; novo pitanje van zadatka ga zatvara.
        var active = result.Intent == ChatIntent.Task ? result.Task : null;

        // Razgovor se čuva uz profil (ako je izabran), da bi se posle zatvaranja aplikacije mogao nastaviti.
        if (req.ProfileId is int profileId)
        {
            var subject = req.Subject ?? "";
            await _profiles.AddMessageAsync(profileId, subject, new StoredMessage("user", req.Message, null, null, null));
            await _profiles.AddMessageAsync(profileId, subject, new StoredMessage("assistant", result.Reply, result.Intent.ToString(),
                JsonSerializer.Serialize(sources),
                active is null ? null : JsonSerializer.Serialize(active, ProfilesController.JsonOptions)));
        }

        return Ok(new ChatResponse(result.Reply, sources, result.Intent.ToString(), active));
    }
}
