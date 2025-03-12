using System.Collections.Immutable;
using System.Text.Json;
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

    public async Task<ImmutableArray<ConversationId>> ReadAllConversationIdsAsync()
    {
        var conversationsDir = Path.Combine(this.config.DiskLoggingPath, "conversations");
        if (!Directory.Exists(conversationsDir))
        {
            return ImmutableArray<ConversationId>.Empty;
        }

        var directories = Directory.GetDirectories(conversationsDir);
        var conversationIds = new List<ConversationId>();

        foreach (var dir in directories)
        {
            var dirName = Path.GetFileName(dir);
            conversationIds.Add(new ConversationId(dirName));
        }

        return conversationIds.ToImmutableArray();
    }

    public async Task<ImmutableArray<StoredLlmRequest>> ReadLlmRequestsAsync(ConversationId conversationId)
    {
        var llmRequestsDir = Path.Combine(
            this.config.DiskLoggingPath,
            "conversations",
            conversationId.Value,
            "llm_requests");

        if (!Directory.Exists(llmRequestsDir))
        {
            return ImmutableArray<StoredLlmRequest>.Empty;
        }

        var files = Directory.GetFiles(llmRequestsDir, "*.json");
        var requests = new List<StoredLlmRequest>();

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var llmRequest = JsonSerializer.Deserialize<LlmRequest>(content);

            if (llmRequest == null)
            {
                throw new InvalidOperationException("Failed to deserialize LLM request.");
            }

            var storedRequest = new StoredLlmRequest(
                llmRequest.Input.Select(m => new StoredMessage(m.Role, m.Content)).ToImmutableArray(),
                new StoredMessage(llmRequest.Output.Role, llmRequest.Output.Content));

            requests.Add(storedRequest);
        }

        return requests.ToImmutableArray();
    }

    public async Task<ImmutableArray<StoredMessage>> ReadUserMessagesAsync(ConversationId conversationId)
    {
        var messagesFile = Path.Combine(
            this.config.DiskLoggingPath, "conversations", conversationId.Value, "messages.json");

        if (!File.Exists(messagesFile))
        {
            return ImmutableArray<StoredMessage>.Empty;
        }

        var content = await File.ReadAllTextAsync(messagesFile);
        var requests = JsonSerializer.Deserialize<List<ConversationRequest>>(content);

        if (requests == null)
        {
            throw new InvalidOperationException("Failed to deserialize messages.");
        }

        var messages = new List<StoredMessage>();

        foreach (var request in requests)
        {
            var message = new StoredMessage(request.Role, request.Message);
            messages.Add(message);
        }

        return messages.ToImmutableArray();
    }

    public async Task StoreLlmRequestAsync(
        ConversationId conversationId,
        IncomingRequestId requestId,
        StoredLlmRequest request)
    {
        var conversationDir = Path.Combine(this.config.DiskLoggingPath, "conversations", conversationId.Value);
        var llmRequestsDir = Path.Combine(conversationDir, "llm_requests");

        Directory.CreateDirectory(llmRequestsDir);
        int countOfExistingRequests = Directory.GetFiles(llmRequestsDir, "*.json").Length;

        Directory.CreateDirectory(llmRequestsDir);

        var llmRequest = new LlmRequest(
            conversationId.Value,
            requestId.Value,
            request.Input.Select(m => new LlmRequestMessage(m.Role, m.Content)).ToImmutableArray(),
            new LlmRequestMessage(request.Output.Role, request.Output.Content),
            Milliseconds: 0);

        var json = JsonSerializer.Serialize(llmRequest);
        var filePath = Path.Combine(llmRequestsDir, $"{countOfExistingRequests}_req_{requestId.Value[..8]}.json");

        await File.WriteAllTextAsync(filePath, json);

        await this.UpdateMetadataAsync(conversationId);
    }

    public async Task StoreUserMessageAsync(
        ConversationId conversationId,
        IncomingRequestId requestId,
        StoredMessage message)
    {
        var conversationDir = Path.Combine(this.config.DiskLoggingPath, "conversations", conversationId.Value);
        Directory.CreateDirectory(conversationDir);

        var messagesFile = Path.Combine(conversationDir, "messages.json");

        List<ConversationRequest> messages;

        if (File.Exists(messagesFile))
        {
            var content = await File.ReadAllTextAsync(messagesFile);
            messages = JsonSerializer.Deserialize<List<ConversationRequest>>(content)
                ?? new List<ConversationRequest>();
        }
        else
        {
            messages = new List<ConversationRequest>();
        }

        messages.Add(new ConversationRequest(
            conversationId.Value,
            message.Role,
            message.Content,
            requestId));

        var json = JsonSerializer.Serialize(messages);
        await File.WriteAllTextAsync(messagesFile, json);

        await this.UpdateMetadataAsync(conversationId);
    }

    private async Task UpdateMetadataAsync(ConversationId conversationId)
    {
        var conversationDir = Path.Combine(this.config.DiskLoggingPath, "conversations", conversationId.Value);
        var messagesFile = Path.Combine(conversationDir, "messages.json");
        var metadataFile = Path.Combine(conversationDir, "metadata.json");

        int messageCount = 0;

        if (File.Exists(messagesFile))
        {
            var content = await File.ReadAllTextAsync(messagesFile);
            var messages = JsonSerializer.Deserialize<List<ConversationRequest>>(content);
            messageCount = messages?.Count ?? 0;
        }

        var metadata = new Metadata(
            conversationId.Value,
            messageCount,
            DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(metadataFile, json);
    }

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
        string Message,
        IncomingRequestId? RequestId = null);

    /// <summary>
    /// A request to the LLM.
    /// One message from the user may result in multiple requests to the LLM,
    /// before the agent responds.
    /// </summary>
    internal sealed record LlmRequest(
        string ConversationId,
        string RequestId,
        ImmutableArray<LlmRequestMessage> Input,
        LlmRequestMessage Output,
        int Milliseconds,
        int TokensIn = 0,
        int TokensOut = 0);

    internal sealed record LlmRequestMessage(string Role, string Content);
}
