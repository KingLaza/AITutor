using AITutor.Core.Subjects;

namespace AITutor.Core.Rag;

public class LlmIntentClassifier : IIntentClassifier
{
    private readonly IChatService _chat;
    private readonly SubjectRegistry _subjects;

    private static readonly HashSet<string> SocialMessages = new()
    {
        "cao", "ćao", "zdravo", "pozdrav", "hej", "hello", "hi", "dobar dan", "dobro jutro",
        "dobro veče", "dobro vece", "hvala", "hvala ti", "hvala puno", "thanks", "ok", "okej", "super"
    };

    public LlmIntentClassifier(IChatService chat, SubjectRegistry subjects)
    {
        _chat = chat;
        _subjects = subjects;
    }

    public async Task<ChatIntent> ClassifyAsync(string message, string? subject = null)
    {
        var subjectName = string.IsNullOrWhiteSpace(subject) ? "ovog predmeta" : subject;
        var normalized = message.Trim().ToLowerInvariant().TrimEnd('!', '.', '?', ' ');
        if (SocialMessages.Contains(normalized)) return ChatIntent.Casual;

        var snippet = message.Length > 600 ? message[..600] : message;
        var subjectExamples = string.Join("\n", _subjects.Get(subjectName).ClassifierExamples);
        var prompt = $"""
            Klasifikuj poruku studenta u TAČNO jednu kategoriju i odgovori samo jednom rečju.

            CASUAL - pozdrav, zahvalnost, kratak razgovor, pitanje o tebi ili o učenju uopšte. NIJE činjenično pitanje iz bilo koje oblasti.
            SUBJECT - bilo koje stručno ili činjenično pitanje o nekom pojmu (čak i ako izgleda da je van predmeta {subjectName}).
            TASK - ispitni zadatak ili zahtev da se nešto uradi, reši, napiše ili transformiše.

            Primeri:
            "Ćao!" -> CASUAL
            "Kako mi možeš pomoći?" -> CASUAL
            "Hvala, to mi je jasno" -> CASUAL
            "Šta je nuklearna fizika?" -> SUBJECT
            "Napiši program koji sortira niz" -> TASK
            {subjectExamples}

            Poruka: "{snippet}"
            Kategorija:
            """;

        var answer = (await _chat.GenerateAsync(prompt, new ChatOptions(Temperature: 0, MaxTokens: 5))).ToUpperInvariant();

        if (answer.Contains("CASUAL")) return ChatIntent.Casual;
        if (answer.Contains("TASK")) return ChatIntent.Task;
        return ChatIntent.Subject; // podrazumevano = postojeće ponašanje
    }
}
