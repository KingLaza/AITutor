namespace AITutor.Core.Data;

public class ChunkSearchResult
{
    public string Content { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public double Distance { get; set; }
}
