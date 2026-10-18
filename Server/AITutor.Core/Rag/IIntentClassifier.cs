namespace AITutor.Core.Rag;

public enum ChatIntent { Casual, Subject, Task }

public interface IIntentClassifier
{
    Task<ChatIntent> ClassifyAsync(string message, string? subject = null);
}
