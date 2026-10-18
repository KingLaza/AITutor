namespace AITutor.Core.Subjects;

// Profil za predmete bez posebnih dodataka: samo opšte ponašanje jezgra.
public class GenericSubjectProfile : ISubjectProfile
{
    public GenericSubjectProfile(string name) => Name = name;

    public string Name { get; }
    public double MaxDistance => 0.56;
    public ITaskSolver? Solver => null;
    public TaskGuidance? GetGuidance(string task) => null;
    public IReadOnlyList<string> ClassifierExamples => Array.Empty<string>();
}

public class SubjectRegistry
{
    private readonly Dictionary<string, ISubjectProfile> _profiles;

    public SubjectRegistry(IEnumerable<ISubjectProfile> profiles, string? defaultName = null)
    {
        _profiles = profiles.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        DefaultName = defaultName is { Length: > 0 } ? defaultName : _profiles.Keys.FirstOrDefault() ?? "";
    }

    // Predmet koji se koristi kada zahtev ne navede nijedan.
    public string DefaultName { get; }

    // Nepoznat predmet (npr. samo ingestovan materijal) dobija opšti profil.
    public ISubjectProfile Get(string name) =>
        _profiles.TryGetValue(name, out var profile) ? profile : new GenericSubjectProfile(name);
}
