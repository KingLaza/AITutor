using Microsoft.AspNetCore.Mvc;
using AITutor.Core.Ingestion;

namespace AITutor_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestController : ControllerBase
{
    private readonly IngestionService _ingestion;

    public IngestController(IngestionService ingestion) => _ingestion = ingestion;

    public record IngestRequest(string MaterialsPath, string Subject);
    public record IngestResponse(int ChunksInserted);

    [HttpPost]
    public async Task<ActionResult<IngestResponse>> Post(IngestRequest req)
    {
        var count = await _ingestion.IngestFolderAsync(req.MaterialsPath, req.Subject);
        return Ok(new IngestResponse(count));
    }
}
