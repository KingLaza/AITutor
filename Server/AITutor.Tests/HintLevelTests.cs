using AITutor.Core.Rag;

namespace AITutor.Tests;

public class HintLevelTests
{
    [Theory]
    [InlineData("Sledeći nagoveštaj", 1, 2)]
    [InlineData("sledeci nagovestaj", 2, 3)]
    [InlineData("Sledeći nagoveštaj", 3, 3)]
    [InlineData("Prikaži rešenje", 1, 3)]
    [InlineData("pokazi resenje", 2, 3)]
    [InlineData("ne znam", 1, 2)]
    [InlineData("Daj mi još jedan nagoveštaj!", 1, 2)]
    public void Recognizes_hint_requests(string message, int current, int expected) =>
        Assert.Equal(expected, RagQueryService.TryGetHintLevel(message, current));

    [Theory]
    [InlineData("Šta je tabela simbola?")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Transformisati datu gramatiku u LL(1) gramatiku i dokazati da je gramatika LL(1) uz resenje")]
    public void Ignores_other_messages(string message) =>
        Assert.Null(RagQueryService.TryGetHintLevel(message, 1));
}
