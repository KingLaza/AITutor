using AITutor.Core.Rag;
using AITutor.Core.Subjects;

namespace AITutor.Tests;

public class IntentClassifierTests
{
    private static SubjectRegistry Registry(params string[] examples) =>
        new(new ISubjectProfile[] { new TestProfile("Test", null, 0.56, examples) });

    [Fact]
    public async Task Subject_specific_examples_are_added_to_the_prompt()
    {
        var chat = new FakeChat { Responder = _ => "TASK" };
        var classifier = new LlmIntentClassifier(chat, Registry("\"Primer specifican za predmet\" -> TASK"));

        await classifier.ClassifyAsync("Neko pitanje o zadatku", "Test");

        Assert.Contains("Primer specifican za predmet", chat.Prompts.Single());
    }

    [Theory]
    [InlineData("Cao")]
    [InlineData("cao!")]
    [InlineData("Dobro jutro")]
    [InlineData("hvala ti")]
    public async Task Greetings_are_casual_without_calling_the_model(string message)
    {
        var chat = new FakeChat();
        Assert.Equal(ChatIntent.Casual, await new LlmIntentClassifier(chat, Registry()).ClassifyAsync(message));
        Assert.Empty(chat.Prompts);
    }

    [Theory]
    [InlineData("CASUAL", ChatIntent.Casual)]
    [InlineData("  task\n", ChatIntent.Task)]
    [InlineData("SUBJECT", ChatIntent.Subject)]
    [InlineData("nešto neprepoznatljivo", ChatIntent.Subject)]
    public async Task Parses_model_answer_and_defaults_to_subject(string modelAnswer, ChatIntent expected)
    {
        var chat = new FakeChat { Responder = _ => modelAnswer };
        Assert.Equal(expected, await new LlmIntentClassifier(chat, Registry()).ClassifyAsync("Sta je LL(1) gramatika?"));
        Assert.Single(chat.Prompts);
    }
}
