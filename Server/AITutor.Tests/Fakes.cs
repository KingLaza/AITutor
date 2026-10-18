using AITutor.Core;
using AITutor.Core.Data;
using AITutor.Core.Rag;
using AITutor.Core.Subjects;

namespace AITutor.Tests;

public class FakeEmbeddings : IEmbeddingService
{
    public List<string> Texts { get; } = new();
    public Task<float[]> GetEmbeddingAsync(string text, EmbeddingKind kind = EmbeddingKind.Document)
    {
        Texts.Add(text);
        return Task.FromResult(new[] { 1f });
    }
}

public class FakeChat : IChatService
{
    public List<string> Prompts { get; } = new();
    public List<ChatOptions?> Options { get; } = new();
    public Func<string, string> Responder { get; set; } = _ => "odgovor";
    public Task<string> GenerateAsync(string prompt, ChatOptions? options = null)
    {
        Prompts.Add(prompt);
        Options.Add(options);
        return Task.FromResult(Responder(prompt));
    }
}

public class FakeIntent : IIntentClassifier
{
    public ChatIntent Result { get; set; } = ChatIntent.Subject;
    public int Calls { get; private set; }
    public Task<ChatIntent> ClassifyAsync(string message, string? subject = null)
    {
        Calls++;
        return Task.FromResult(Result);
    }
}

public class FakeRepo : IChunkRepository
{
    public List<ChunkSearchResult> Dense { get; set; } = new();
    public List<ChunkSearchResult> Hybrid { get; set; } = new();
    public Dictionary<int, ChunkSearchResult> Next { get; } = new();
    public List<string?> Subjects { get; } = new();
    public string[]? LastDenseTypes { get; private set; }

    public List<ChunkRecord> Inserted { get; } = new();
    public List<(string Subject, string SourceType, string SourceFile)> Deleted { get; } = new();

    public Task InsertAsync(ChunkRecord chunk)
    {
        Inserted.Add(chunk);
        return Task.CompletedTask;
    }

    public Task<List<MaterialInfo>> ListMaterialsAsync(string subject, string sourceType) =>
        Task.FromResult(Inserted.Where(c => c.Subject == subject && c.SourceType == sourceType)
            .GroupBy(c => c.SourceFile ?? "").Select(g => new MaterialInfo(g.Key, g.Count())).ToList());

    public Task<int> DeleteMaterialAsync(string subject, string sourceType, string sourceFile)
    {
        Deleted.Add((subject, sourceType, sourceFile));
        return Task.FromResult(Inserted.RemoveAll(c => c.Subject == subject && c.SourceType == sourceType && c.SourceFile == sourceFile));
    }

    public Task<List<string>> GetSubjectsAsync() => Task.FromResult(new List<string>());

    public Task<List<ChunkSearchResult>> SearchAsync(float[] queryEmbedding, int topK, string[]? sourceTypes = null, string? subject = null)
    {
        Subjects.Add(subject);
        LastDenseTypes = sourceTypes;
        return Task.FromResult(Dense.Take(topK).ToList());
    }

    public Task<List<ChunkSearchResult>> SearchHybridAsync(float[] queryEmbedding, string queryText, int topK, string[]? sourceTypes = null, string? subject = null)
    {
        Subjects.Add(subject);
        return Task.FromResult(Hybrid.Where(c => sourceTypes == null || sourceTypes.Contains(c.SourceType)).Take(topK).ToList());
    }

    public Task<ChunkSearchResult?> GetNextChunkAsync(int id) =>
        Task.FromResult(Next.TryGetValue(id, out var n) ? n : null);
}

public class FakeSolver : ITaskSolver
{
    public SolverResult? Result { get; set; }
    public int Calls { get; private set; }
    public Task<SolverResult?> TrySolveAsync(string task)
    {
        Calls++;
        return Task.FromResult(Result);
    }
}

public class TestProfile : ISubjectProfile
{
    public TestProfile(string name, ITaskSolver? solver = null, double maxDistance = 0.56, params string[] classifierExamples)
    {
        Name = name;
        Solver = solver;
        MaxDistance = maxDistance;
        ClassifierExamples = classifierExamples;
    }

    public string Name { get; }
    public double MaxDistance { get; }
    public ITaskSolver? Solver { get; }
    public TaskGuidance? GetGuidance(string task) => null;
    public IReadOnlyList<string> ClassifierExamples { get; }
}
