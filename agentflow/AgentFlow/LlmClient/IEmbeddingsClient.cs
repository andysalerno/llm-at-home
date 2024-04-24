using System.Collections.Immutable;

namespace AgentFlow.LlmClient;

public interface IEmbeddingsClient
{
    Task<EmbeddingResponse> GetEmbeddingsAsync(string query, IEnumerable<string> passages);
}

public record EmbeddingResponse(
    ImmutableArray<EmbeddingData> Data,
    EmbeddingData? QueryData,
    string Model);

public record EmbeddingData(
    ImmutableArray<float> Embedding,
    int Index);
