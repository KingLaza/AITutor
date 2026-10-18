using AITutor.Core.Ingestion;

namespace AITutor.Tests;

public class IngestionServiceTests : IDisposable
{
    private readonly FakeEmbeddings _emb = new();
    private readonly FakeRepo _repo = new();
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aitutor-tests-" + Guid.NewGuid().ToString("N"));

    public IngestionServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, true);

    private string TempFile(string name, string content)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task Ingests_a_text_file_for_a_subject_with_progress()
    {
        var path = TempFile("beleske.txt", "Semafor je promenljiva za sinhronizaciju procesa.");
        var progress = new List<(int, int)>();

        var chunks = await new IngestionService(_emb, _repo).IngestFileAsync(path, "beleske.txt", "Operativni sistemi", "moje",
            (done, total) => progress.Add((done, total)));

        Assert.Equal(1, chunks);
        var record = Assert.Single(_repo.Inserted);
        Assert.Equal("Operativni sistemi", record.Subject);
        Assert.Equal("moje", record.SourceType);
        Assert.Equal("beleske.txt", record.SourceFile);
        Assert.Equal(new[] { (0, 1), (1, 1) }, progress);
    }

    [Fact]
    public async Task Large_text_is_split_into_chunks_of_bounded_size()
    {
        var path = TempFile("dugacko.txt", string.Concat(Enumerable.Repeat("proces ", 1000)));

        var chunks = await new IngestionService(_emb, _repo).IngestFileAsync(path, "dugacko.txt", "S", "moje");

        Assert.Equal(3, chunks);
        Assert.All(_repo.Inserted, r => Assert.True(r.Content.Length <= 3000));
    }

    [Fact]
    public async Task Empty_file_produces_no_chunks()
    {
        var path = TempFile("prazno.txt", "   \n  ");

        Assert.Equal(0, await new IngestionService(_emb, _repo).IngestFileAsync(path, "prazno.txt", "S", "moje"));
        Assert.Empty(_repo.Inserted);
    }

    [Fact]
    public async Task Unsupported_format_is_rejected()
    {
        var path = TempFile("slika.png", "x");

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            new IngestionService(_emb, _repo).IngestFileAsync(path, "slika.png", "S", "moje"));
    }
}
