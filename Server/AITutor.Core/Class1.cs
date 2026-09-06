namespace AITutor.Core;

public enum EmbeddingKind { Document, Query }
public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, EmbeddingKind kind = EmbeddingKind.Document);
}

public interface IChatService
{
    Task<string> GenerateAsync(string prompt);
}
