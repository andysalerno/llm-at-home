using System.Collections.Immutable;

namespace AgentFlow.WorkSpace;

public interface IConversationPersistenceWriter
{
    Task StoreUserMessageAsync(ConversationId conversationId, IncomingRequestId requestId, StoredMessage message);

    Task StoreLlmRequestAsync(ConversationId conversationId, IncomingRequestId requestId, StoredLlmRequest request);
}

public interface IConversationPersistenceReader
{
    Task<ImmutableArray<StoredMessage>> ReadUserMessagesAsync(ConversationId conversationId);

    Task<ImmutableArray<StoredLlmRequest>> ReadLlmRequestsAsync(ConversationId conversationId);

    Task<ImmutableArray<ConversationId>> ReadAllConversationIdsAsync();
}

public sealed record StoredMessage(string Role, string Content);

/// <summary>
/// Represents a request to the LLM.
/// </summary>
/// <param name="Input">The input messages to the LLM.</param>
/// <param name="Output">The output response from the LLM.</param>
public sealed record StoredLlmRequest(ImmutableArray<StoredMessage> Input, StoredMessage Output);

public sealed record ConversationId(string Value);

public sealed record IncomingRequestId(string Value);
