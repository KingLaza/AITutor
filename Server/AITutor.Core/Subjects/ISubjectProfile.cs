namespace AITutor.Core.Subjects;

// Hints: kratki nagoveštaji metode za nivoe 1 i 2 (bez konačnog odgovora); pišu se unapred, pa ih ne generiše model.
public record SolverResult(string Type, string Text, IReadOnlyList<string>? Hints = null);

public interface ITaskSolver
{
    // Vraća tačno, programski izračunato rešenje ili null ako zadatak nije podržanog tipa (ili parametri nisu izvučeni).
    Task<SolverResult?> TrySolveAsync(string task);
}

// Smernice za određenu vrstu zadatka: notacija koja ide u prompt i izvori iz kojih se biraju primeri.
// Primarni izvori (npr. provereni obrasci) uvek ulaze u kontekst, uz nekoliko primera iz sekundarnih izvora.
public record TaskGuidance(string Notation, string[] PrimaryExampleTypes, int PrimaryCount, string[] SecondaryExampleTypes, int SecondaryCount);

// Sve što je specifično za jedan predmet. Jezgro (pretraga, lestvica nagoveštaja, istorija) ne zna ništa o pojedinom predmetu;
// novi predmet se dodaje implementacijom ovog interfejsa i registracijom u Program.cs.
public interface ISubjectProfile
{
    string Name { get; }

    // Razdaljina iznad koje se pitanje odbija bez poziva modela (zavisi od materijala predmeta).
    double MaxDistance { get; }

    // Alat za tačno rešavanje ponavljajućih zadataka (npr. proračuni); null ako predmet nema takve zadatke.
    ITaskSolver? Solver { get; }

    // Posebne smernice za određenu vrstu zadatka; null za sve ostale.
    TaskGuidance? GetGuidance(string task);

    // Dodatni primeri za klasifikator namere, u obliku: "tekst poruke" -> TASK | SUBJECT | CASUAL
    IReadOnlyList<string> ClassifierExamples { get; }
}
