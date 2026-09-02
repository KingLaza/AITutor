namespace AITutor.Core.Chunking;

public static class ChunkingStrategyFactory
{
    public static IChunkingStrategy GetStrategy(string sourceType) => sourceType switch
    {
        "predavanje" or "vezba" => new PerPageChunkingStrategy(),
        "pismeni" or "usmeni" => new PerTaskChunkingStrategy(),
        "resen_blanket" or "okruzenje" => new WholeDocumentChunkingStrategy(),
        _ => throw new NotSupportedException($"Nepoznat source_type: {sourceType}")
    };
}
