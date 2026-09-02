using UglyToad.PdfPig;

namespace AITutor.Core.Parsing;

public class PdfDocumentParser : IDocumentParser
{
    public Task<List<string>> ExtractPagesAsync(string filePath)
    {
        var pages = new List<string>();
        using var pdf = PdfDocument.Open(filePath);
        foreach (var page in pdf.GetPages())
            pages.Add(page.Text);
        return Task.FromResult(pages);
    }
}
