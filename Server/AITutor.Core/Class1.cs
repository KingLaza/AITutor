namespace AITutor.Core;

public enum EmbeddingKind { Document, Query }
public record ChatOptions(double Temperature = 0.2, int? MaxTokens = null, int? NumCtx = null);
public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, EmbeddingKind kind = EmbeddingKind.Document);
}

public interface IChatService
{
    Task<string> GenerateAsync(string prompt, ChatOptions? options = null);
}
