namespace AITutor.Core.Chunking;

public static class ChunkingStrategyFactory
{
    public static IChunkingStrategy GetStrategy(string sourceType) => sourceType switch
    {
        "predavanja" or "vezbe" => new PerPageChunkingStrategy(),
        "pismeni" => new PerTaskChunkingStrategy(),
        "usmeni" => new SequentialNumberedChunkingStrategy(),
        "reseni_blanketi" => new WholeDocumentChunkingStrategy(),
        _ => throw new NotSupportedException($"Nepoznat source_type: {sourceType}")
    };
}
