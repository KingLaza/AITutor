namespace AITutor.Core.Data;

public class ChunkSearchResult
{
    public string Content { get; set; } = "";
    public int Id { get; set; }
    public bool IsNeighbor { get; set; }
    public string SourceType { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public double Distance { get; set; }
}
