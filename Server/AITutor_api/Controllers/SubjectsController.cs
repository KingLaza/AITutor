using Microsoft.AspNetCore.Mvc;
using AITutor.Core.Data;
using AITutor.Core.Subjects;

namespace AITutor_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly IChunkRepository _repository;
    private readonly SubjectRegistry _subjects;

    public SubjectsController(IChunkRepository repository, SubjectRegistry subjects)
    {
        _repository = repository;
        _subjects = subjects;
    }

    // Predmeti za koje postoji materijal; podrazumevani predmet je prvi, da klijent njega izabere na početku.
    [HttpGet]
    public async Task<ActionResult<List<string>>> Get()
    {
        var subjects = await _repository.GetSubjectsAsync();
        return Ok(subjects
            .OrderBy(s => s.Equals(_subjects.DefaultName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }
}
