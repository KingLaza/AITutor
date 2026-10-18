namespace AITutor.Core.Subjects.ProgramskiPrevodioci;

public class ProgramskiPrevodiociProfile : ISubjectProfile
{
    public string Name => "Programski prevodioci";

    // Izmereno na 38 pitanja (embeddinggemma): najdalje pitanje iz domena 0.535, najbliže van domena 0.467.
    public double MaxDistance => 0.56;

    public ITaskSolver? Solver => null;

    public TaskGuidance? GetGuidance(string task) => null;

    public IReadOnlyList<string> ClassifierExamples { get; } = new[]
    {
        "\"Razlika između kompilatora i interpretatora?\" -> SUBJECT",
        "\"Objasni FIRST i FOLLOW\" -> SUBJECT",
        "\"Transformisati datu gramatiku u LL(1) gramatiku i dokazati\" -> TASK",
        "\"Definisati klasu za while petlju u AST-u i napisati translate()\" -> TASK",
    };
}
