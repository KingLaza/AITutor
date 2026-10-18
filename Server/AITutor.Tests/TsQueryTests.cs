using AITutor.Core.Data;

namespace AITutor.Tests;

public class TsQueryTests
{
    [Fact]
    public void Truncates_to_five_letters_and_drops_stopwords_and_short_words()
    {
        var q = PgvectorChunkRepository.BuildTsQuery("Definisati klasu za while petlju u AST-u i napisati metodu translate()");
        var terms = q.Split(" | ");
        Assert.Contains("klasu:*", terms);
        Assert.Contains("while:*", terms);
        Assert.Contains("petlj:*", terms);
        Assert.Contains("trans:*", terms);
        Assert.DoesNotContain(terms, t => t.StartsWith("defin"));
        Assert.DoesNotContain(terms, t => t.StartsWith("ast"));
    }

    [Fact]
    public void Diacritics_and_dj_are_normalized_the_same_way()
    {
        Assert.Equal(PgvectorChunkRepository.BuildTsQuery("međukod"), PgvectorChunkRepository.BuildTsQuery("medjukod"));
        Assert.Equal("sinta:*", PgvectorChunkRepository.BuildTsQuery("sintaksna"));
        Assert.Equal(PgvectorChunkRepository.BuildTsQuery("šema"), PgvectorChunkRepository.BuildTsQuery("sema"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?!")]
    [InlineData("a je u")]
    public void Returns_empty_when_nothing_searchable(string text) =>
        Assert.Equal("", PgvectorChunkRepository.BuildTsQuery(text));

    [Fact]
    public void Output_contains_only_safe_characters()
    {
        var q = PgvectorChunkRepository.BuildTsQuery("x'); DROP TABLE chunks; -- & | ! ( ) :* test");
        Assert.Matches(@"^[a-z0-9:\* |]*$", q);
    }
}
