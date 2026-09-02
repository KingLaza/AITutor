namespace AITutor.Core.Data;

public class ChunkRecord
{
    public string Subject { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string? SourceFile { get; set; }
    public string Content { get; set; } = "";
    public float[] Embedding { get; set; } = Array.Empty<float>();
}
