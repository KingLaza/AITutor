namespace AITutor.Core.Parsing;

public static class DocumentParserFactory
{
    public static IDocumentParser GetParser(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => new PdfDocumentParser(),
            ".cup" or ".java" or ".flex" or ".txt" or ".md" or ".c" or ".h" or ".sh" => new PlainTextDocumentParser(),
            _ => throw new NotSupportedException($"Nepodržan format: {ext}")
        };
    }
}
