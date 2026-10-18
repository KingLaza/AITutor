using AITutor.Core.Chunking;

namespace AITutor.Tests;

public class ChunkingTests
{
    [Fact]
    public void Sequential_ignores_nested_lists_that_restart_at_one()
    {
        var text = "1. Prvo pitanje?\nOdgovor\n1. stavka\n2. Drugo pitanje?\nOdgovor\n3. Trece pitanje?\nOdgovor";
        var chunks = new SequentialNumberedChunkingStrategy().Chunk(new List<string> { text });
        Assert.Equal(3, chunks.Count);
        Assert.Contains("1. stavka", chunks[0]);
        Assert.StartsWith("2. Drugo", chunks[1]);
    }

    [Fact]
    public void Sequential_without_numbering_returns_whole_text()
    {
        var chunks = new SequentialNumberedChunkingStrategy().Chunk(new List<string> { "bez numeracije" });
        Assert.Single(chunks);
    }

    [Fact]
    public void PerTask_splits_on_task_numbers_and_ignores_year_in_header()
    {
        var text = "Ispit 2020. godine\n1. (6 poena) Zadatak jedan\n2. (6 poena) Zadatak dva";
        var chunks = new PerTaskChunkingStrategy().Chunk(new List<string> { text });
        Assert.Equal(2, chunks.Count);
        Assert.StartsWith("1.", chunks[0]);
        Assert.Contains("Zadatak dva", chunks[1]);
    }

    [Fact]
    public void Section_strategy_splits_on_headers_and_drops_the_preamble()
    {
        var text = "uvodna napomena\n### Obrazac 1: prvi\ntelo 1\nvise redova\n### Obrazac 2: drugi\ntelo 2";
        var chunks = new SectionChunkingStrategy().Chunk(new List<string> { text });

        Assert.Equal(2, chunks.Count);
        Assert.StartsWith("Obrazac 1: prvi", chunks[0]);
        Assert.Contains("vise redova", chunks[0]);
        Assert.DoesNotContain("uvodna", string.Join(" ", chunks));
        Assert.StartsWith("Obrazac 2: drugi", chunks[1]);
    }

    [Fact]
    public void WholeDocument_returns_single_chunk()
    {
        var chunks = new WholeDocumentChunkingStrategy().Chunk(new List<string> { "a", "b" });
        Assert.Single(chunks);
    }

    [Theory]
    [InlineData("predavanja")]
    [InlineData("vezbe")]
    [InlineData("pismeni")]
    [InlineData("usmeni")]
    [InlineData("reseni_blanketi")]
    [InlineData("obrasci")]
    [InlineData("moje")]
    public void Factory_knows_all_source_types(string sourceType) =>
        Assert.NotNull(ChunkingStrategyFactory.GetStrategy(sourceType));

    [Fact]
    public void Factory_rejects_unknown_source_type() =>
        Assert.Throws<NotSupportedException>(() => ChunkingStrategyFactory.GetStrategy("nepoznato"));
}
