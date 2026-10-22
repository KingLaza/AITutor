using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using AITutor.Core.Data;
using AITutor.Core.Rag;

namespace AITutor_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    private const int MaxNameLength = 40;

    private readonly IProfileRepository _profiles;

    public ProfilesController(IProfileRepository profiles) => _profiles = profiles;

    public record ProfileRequest(string Name);
    public record MessageDto(string Role, string Content, string? Intent, List<string> Sources, TaskState? ActiveTask, string? Origin);

    [HttpGet]
    public async Task<ActionResult<List<Profile>>> List() => Ok(await _profiles.ListAsync());

    // Ime se skraćuje od razmaka na krajevima; ako profil već postoji, vraća se postojeći.
    [HttpPost]
    public async Task<ActionResult<Profile>> Create(ProfileRequest req)
    {
        var name = Normalize(req.Name);
        if (name == null) return BadRequest($"Ime mora imati 1-{MaxNameLength} znakova.");
        return Ok(await _profiles.GetOrCreateAsync(name));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Profile>> Rename(int id, ProfileRequest req)
    {
        var name = Normalize(req.Name);
        if (name == null) return BadRequest($"Ime mora imati 1-{MaxNameLength} znakova.");
        return await _profiles.RenameAsync(id, name)
            ? Ok(new Profile(id, name))
            : Conflict("Profil sa tim imenom već postoji.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _profiles.DeleteAsync(id);
        return NoContent();
    }

    // Razgovori profila, najskoriji prvi; bez parametra subject: iz svih predmeta.
    [HttpGet("{id:int}/conversations")]
    public async Task<ActionResult<List<ConversationInfo>>> Conversations(int id, [FromQuery] string? subject) =>
        Ok(await _profiles.ListConversationsAsync(id, subject));

    [HttpGet("{id:int}/messages")]
    public async Task<ActionResult<List<MessageDto>>> Messages(int id, [FromQuery] string subject, [FromQuery] int conversation = 1)
    {
        var stored = await _profiles.GetMessagesAsync(id, subject, conversation);
        return Ok(stored.Select(m => new MessageDto(
            m.Role, m.Content, m.Intent,
            m.SourcesJson is null ? new List<string>() : JsonSerializer.Deserialize<List<string>>(m.SourcesJson) ?? new(),
            m.ActiveTaskJson is null ? null : JsonSerializer.Deserialize<TaskState>(m.ActiveTaskJson, JsonOptions),
            m.Origin))
            .ToList());
    }

    [HttpDelete("{id:int}/messages")]
    public async Task<IActionResult> Clear(int id, [FromQuery] string subject, [FromQuery] int conversation = 1)
    {
        await _profiles.DeleteConversationAsync(id, subject, conversation);
        return NoContent();
    }

    private static string? Normalize(string? name)
    {
        var trimmed = (name ?? "").Trim();
        return trimmed.Length is > 0 and <= MaxNameLength ? trimmed : null;
    }

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
