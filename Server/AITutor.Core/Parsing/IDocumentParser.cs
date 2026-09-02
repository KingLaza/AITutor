namespace AITutor.Core.Parsing;

public interface IDocumentParser
{
    Task<List<string>> ExtractPagesAsync(string filePath);
}
