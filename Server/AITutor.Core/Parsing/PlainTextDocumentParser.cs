namespace AITutor.Core.Parsing;

public class PlainTextDocumentParser : IDocumentParser
{
    public async Task<List<string>> ExtractPagesAsync(string filePath)
    {
        var text = await File.ReadAllTextAsync(filePath);
        return new List<string> { text };
    }
}
