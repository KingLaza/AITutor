using Microsoft.AspNetCore.Mvc;
using AITutor.Core.Data;
using AITutor.Core.Ingestion;

namespace AITutor_api.Controllers;

// Studentski materijal: upload fajla za izabrani predmet (tip izvora "moje"), spisak i brisanje.
[ApiController]
[Route("api/[controller]")]
public class MaterialsController : ControllerBase
{
    public const string UserSourceType = "moje";
    private const long MaxBytes = 50L * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".txt", ".md", ".c", ".h", ".java", ".sh" };

    private readonly IChunkRepository _repository;
    private readonly UploadJobs _jobs;
    private readonly IServiceScopeFactory _scopes;

    public MaterialsController(IChunkRepository repository, UploadJobs jobs, IServiceScopeFactory scopes)
    {
        _repository = repository;
        _jobs = jobs;
        _scopes = scopes;
    }

    [HttpGet]
    public async Task<ActionResult<List<MaterialInfo>>> List([FromQuery] string subject) =>
        Ok(await _repository.ListMaterialsAsync(subject, UserSourceType));

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string subject, [FromQuery] string file) =>
        await _repository.DeleteMaterialAsync(subject, UserSourceType, file) > 0 ? NoContent() : NotFound();

    // Ingest traje (embedding svakog chunka na CPU-u), pa se radi u pozadini; stanje se čita preko jobs/{id}.
    [HttpPost("upload")]
    [RequestSizeLimit(MaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBytes)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string subject)
    {
        if (file is null || file.Length == 0) return BadRequest("Fajl nije poslat.");
        if (file.Length > MaxBytes) return BadRequest("Fajl je veći od 50 MB.");
        if (string.IsNullOrWhiteSpace(subject)) return BadRequest("Predmet nije izabran.");

        var fileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"Podržani formati: {string.Join(", ", AllowedExtensions.Order())}.");

        var directory = Path.Combine(Path.GetTempPath(), "aitutor-uploads");
        Directory.CreateDirectory(directory);
        var job = _jobs.Create(fileName, subject);
        var tempPath = Path.Combine(directory, job.Id + extension.ToLowerInvariant());
        await using (var stream = System.IO.File.Create(tempPath))
            await file.CopyToAsync(stream);

        _ = Task.Run(() => RunAsync(job, tempPath));
        return Accepted(new { jobId = job.Id });
    }

    [HttpGet("jobs/{id}")]
    public ActionResult<UploadJob> Job(string id) => _jobs.Get(id) is { } job ? Ok(job) : NotFound();

    private async Task RunAsync(UploadJob job, string tempPath)
    {
        using var scope = _scopes.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IChunkRepository>();
        try
        {
            // Ponovni upload istog fajla zamenjuje prethodnu verziju.
            await repository.DeleteMaterialAsync(job.Subject, UserSourceType, job.FileName);
            var chunks = await scope.ServiceProvider.GetRequiredService<IngestionService>()
                .IngestFileAsync(tempPath, job.FileName, job.Subject, UserSourceType, (done, total) =>
                {
                    job.Processed = done;
                    job.Total = total;
                });

            if (chunks == 0)
            {
                job.Error = "U fajlu nije pronađen tekst (možda je skeniran PDF).";
                job.State = "error";
                return;
            }
            job.Chunks = chunks;
            job.State = "done";
        }
        catch (Exception e)
        {
            await repository.DeleteMaterialAsync(job.Subject, UserSourceType, job.FileName); // ukloni delimično ubačeno
            job.Error = e.Message;
            job.State = "error";
        }
        finally
        {
            System.IO.File.Delete(tempPath);
        }
    }
}
