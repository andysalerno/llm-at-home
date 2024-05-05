using System.Collections.Immutable;

namespace AgentFlow.LlmClient;

public interface IEmbeddingsClient
{
    Task<EmbeddingResponse> GetEmbeddingsAsync(string query, IEnumerable<Chunk> passages);

    Task<ScoresResponse> GetScoresAsync(string query, IEnumerable<Chunk> passages);
}

public record ScoresResponse(
    ImmutableArray<float> Scores);

public record EmbeddingResponse(
    ImmutableArray<EmbeddingData> Data,
    EmbeddingData? QueryData,
    string Model);

public record EmbeddingData(
    ImmutableArray<float> Embedding,
    int Index);
