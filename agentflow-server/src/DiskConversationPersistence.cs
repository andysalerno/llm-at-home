using System.Collections.Immutable;
using AgentFlow.Config;
using AgentFlow.WorkSpace;

namespace Agentflow.Server.Persistence;

/// <summary>
/// Persists conversations to disk,
/// in the following structure:
/// logs/
/// ├── conversations/
/// │   ├── conversation_123/
/// │   │   ├── metadata.json
/// │   │   ├── messages.json
/// │   │   └── llm_requests/
/// │   │       ├── req_001.json
/// │   │       ├── req_002.json
/// │   │       └── ...
/// │   └── conversation_124/
/// │       └── ...
/// </summary>
public sealed class DiskConversationPersistence : IConversationPersistenceWriter, IConversationPersistenceReader
{
    private readonly IChatRequestDiskLoggerConfig config;

    public DiskConversationPersistence(IChatRequestDiskLoggerConfig config)
    {
        this.config = config;
    }

    public Task<ImmutableArray<ConversationId>> ReadAllConversationIdsAsync() =>
        throw new NotImplementedException();

    public Task<ImmutableArray<StoredLlmRequest>> ReadLlmRequestsAsync(ConversationId conversationId)
        => throw new NotImplementedException();

    public Task<ImmutableArray<StoredMessage>> ReadUserMessagesAsync(ConversationId conversationId)
        => throw new NotImplementedException();

    public Task StoreLlmRequestAsync(ConversationId conversationId, IncomingRequestId requestId, StoredLlmRequest request)
        => throw new NotImplementedException();

    public Task StoreUserMessageAsync(ConversationId conversationId, IncomingRequestId requestId, StoredMessage message)
        => throw new NotImplementedException();

    internal sealed record Metadata(
        string ConversationId,
        int MessageCount,
        DateTimeOffset LastUpdated);

    /// <summary>
    /// A message from either the user or the agent.
    /// </summary>
    internal sealed record ConversationRequest(
        string ConversationId,
        string Role,
        string Message);

    /// <summary>
    /// A request to the LLM.
    /// One message from the user may result in multiple requests to the LLM,
    /// before the agent responds.
    /// </summary>
    internal sealed record LlmRequest(
        string ConversationId,
        string RequestId,
        string Input,
        string Output,
        int Milliseconds,
        int TokensIn = 0,
        int TokensOut = 0);
}
