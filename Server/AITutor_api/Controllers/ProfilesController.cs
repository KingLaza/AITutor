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

    public record CreateProfileRequest(string Name);
    public record MessageDto(string Role, string Content, string? Intent, List<string> Sources, TaskState? ActiveTask);

    [HttpGet]
    public async Task<ActionResult<List<Profile>>> List() => Ok(await _profiles.ListAsync());

    // Ime se skraćuje od razmaka na krajevima; ako profil već postoji, vraća se postojeći.
    [HttpPost]
    public async Task<ActionResult<Profile>> Create(CreateProfileRequest req)
    {
        var name = (req.Name ?? "").Trim();
        if (name.Length == 0 || name.Length > MaxNameLength)
            return BadRequest($"Ime mora imati 1-{MaxNameLength} znakova.");
        return Ok(await _profiles.GetOrCreateAsync(name));
    }

    [HttpGet("{id:int}/messages")]
    public async Task<ActionResult<List<MessageDto>>> Messages(int id, [FromQuery] string subject)
    {
        var stored = await _profiles.GetMessagesAsync(id, subject);
        return Ok(stored.Select(m => new MessageDto(
            m.Role, m.Content, m.Intent,
            m.SourcesJson is null ? new List<string>() : JsonSerializer.Deserialize<List<string>>(m.SourcesJson) ?? new(),
            m.ActiveTaskJson is null ? null : JsonSerializer.Deserialize<TaskState>(m.ActiveTaskJson, JsonOptions)))
            .ToList());
    }

    [HttpDelete("{id:int}/messages")]
    public async Task<IActionResult> Clear(int id, [FromQuery] string subject)
    {
        await _profiles.ClearMessagesAsync(id, subject);
        return NoContent();
    }

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
