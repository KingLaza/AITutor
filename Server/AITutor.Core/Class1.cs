namespace AITutor.Core;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
}

public interface IChatService
{
    Task<string> GenerateAsync(string prompt);
}
