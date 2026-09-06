using AITutor.Core.Parsing;
using AITutor.Core.Chunking;
using AITutor.Core.Data;

namespace AITutor.Core.Ingestion;

public class IngestionService
{
    private readonly IEmbeddingService _embeddings;
    private readonly IChunkRepository _repository;

    public IngestionService(IEmbeddingService embeddings, IChunkRepository repository)
    {
        _embeddings = embeddings;
        _repository = repository;
    }

    private const int MaxChunkChars = 3000;

    private static List<string> SplitIfTooLarge(string chunk)
    {
        if (chunk.Length <= MaxChunkChars)
            return new List<string> { chunk };

        var parts = new List<string>();
        for (int i = 0; i < chunk.Length; i += MaxChunkChars)
            parts.Add(chunk.Substring(i, Math.Min(MaxChunkChars, chunk.Length - i)));
        return parts;
    }

    private static bool LooksLikeGarbledFormula(string text)
    {
        var words = text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return true;
        var longWordRatio = words.Count(w => w.Length > 25) / (double)words.Length;
        return longWordRatio > 0.3; // baci samo ako je bar trećina sadržaja ovako "garblovana"
    }

    public async Task<int> IngestFolderAsync(string materialsRoot, string subject)
    {
        int totalChunks = 0;

        Console.WriteLine($"Root: {materialsRoot}");
        foreach (var dir in Directory.GetDirectories(materialsRoot))
        {
            Console.WriteLine($"  Folder: {Path.GetFileName(dir)} -> {Directory.GetFiles(dir).Length} fajlova: {string.Join(", ", Directory.GetFiles(dir).Select(Path.GetFileName))}");
        }

        foreach (var sourceTypeDir in Directory.GetDirectories(materialsRoot))
        {
            var sourceType = Path.GetFileName(sourceTypeDir);
            var strategy = ChunkingStrategyFactory.GetStrategy(sourceType);
            

            foreach (var filePath in Directory.GetFiles(sourceTypeDir))
            {
                var parser = DocumentParserFactory.GetParser(filePath);
                var pages = await parser.ExtractPagesAsync(filePath);
                var chunks = strategy.Chunk(pages);

                Console.WriteLine($"[{sourceType}] {Path.GetFileName(filePath)} -> {chunks.Count} chunkova");

                var fullLength = string.Join("", pages).Length;
                Console.WriteLine($"[{sourceType}] {Path.GetFileName(filePath)} -> {pages.Count} strana, {fullLength} karaktera, {chunks.Count} chunkova");

                foreach (var rawChunk in chunks)
                {
                    foreach (var chunkText in SplitIfTooLarge(rawChunk))
                    {
                        if (string.IsNullOrWhiteSpace(chunkText)) continue;

                        if (LooksLikeGarbledFormula(chunkText))
                        {
                            Console.WriteLine($"  [preskočeno - formula] {chunkText[..Math.Min(40, chunkText.Length)]}...");
                            continue;
                        }

                        var embedding = await _embeddings.GetEmbeddingAsync(chunkText);
                        await _repository.InsertAsync(new ChunkRecord
                        {
                            Subject = subject,
                            SourceType = sourceType,
                            SourceFile = Path.GetFileName(filePath),
                            Content = chunkText,
                            Embedding = embedding
                        });
                        totalChunks++;
                    }
                }
            }
        }

        return totalChunks;
    }
}
