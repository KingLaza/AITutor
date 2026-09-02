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

    public async Task<int> IngestFolderAsync(string materialsRoot, string subject)
    {
        int totalChunks = 0;

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

                foreach (var chunkText in chunks)
                {
                    if (string.IsNullOrWhiteSpace(chunkText)) continue;

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

        return totalChunks;
    }
}
