using System.Collections.Concurrent;

namespace AITutor_api;

public class UploadJob
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string FileName { get; init; } = "";
    public string Subject { get; init; } = "";
    public string State { get; set; } = "running"; // running | done | error
    public int Processed { get; set; }
    public int Total { get; set; }
    public int Chunks { get; set; }
    public string? Error { get; set; }
}

// Pozadinski poslovi ingesta studentskog materijala (u memoriji; gube se pri restartu API-ja).
public class UploadJobs
{
    private readonly ConcurrentDictionary<string, UploadJob> _jobs = new();

    public UploadJob Create(string fileName, string subject)
    {
        var job = new UploadJob { FileName = fileName, Subject = subject };
        _jobs[job.Id] = job;
        return job;
    }

    public UploadJob? Get(string id) => _jobs.TryGetValue(id, out var job) ? job : null;
}
