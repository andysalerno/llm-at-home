using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AgentFlow.WorkSpace;

namespace Agentflow.Server.Handler;

internal sealed class TranscriptHandler : IHandler<TranscriptRequest, TranscriptResponse>
{
    private readonly IConversationPersistenceReader conversationReader;

    private readonly ILogger<TranscriptHandler> logger;

    public TranscriptHandler(IConversationPersistenceReader conversationReader, ILogger<TranscriptHandler> logger)
    {
        // this.diskLogger = diskLogger;
        this.conversationReader = conversationReader;
        this.logger = logger;
    }

    public async Task<TranscriptResponse> HandleAsync(TranscriptRequest payload)
    {
        var allConversationIds = await this.conversationReader.ReadAllConversationIdsAsync();

        var sessions = new List<Conversation>();

        foreach (var conversationId in allConversationIds)
        {
            var llmRequests = await this.conversationReader.ReadLlmRequestsAsync(conversationId);
            this.logger.LogInformation(
                "Loading llmRequests for conversationId {ConversationId}: {LlmRequests}", conversationId, llmRequests);

            var conversationMessages = await this.conversationReader.ReadUserMessagesAsync(conversationId);

            var requestsByIncomingRequestId = llmRequests
                .GroupBy(r => r.RequestId)
                .ToDictionary(g => g.Key, g => g.ToImmutableArray());

            var session = new Conversation(
                ConversationId: conversationId.Value,
                Messages: conversationMessages.Select(
                    message => new TranscriptMessage(
                        IncomingRequestId: message.RequestId.Value,
                        ConversationId: conversationId.Value,
                        Content: message.Content,
                        LlmRequests: message.Role.Equals("user", StringComparison.OrdinalIgnoreCase) switch
                        {
                            true => [],
                            false => requestsByIncomingRequestId.TryGetValue(message.RequestId, out var requests)
                                ? requests.Select(r =>
                                    new LlmRequest(
                                        r.RequestId.Value,
                                        r.Input.Select(i => new LlmRequestInput(i.Role, i.Content)).ToImmutableArray(),
                                        r.Output.Content)).ToImmutableArray()
                                : ImmutableArray<LlmRequest>.Empty,
                        }))
                    .ToImmutableArray());

            sessions.Add(session);
        }

        return new TranscriptResponse(sessions.ToImmutableArray());
    }

    // public async Task<TranscriptResponse> HandleAsync(
    //     TranscriptRequest payload)
    // {
    //     this.logger.LogInformation("Handling transcript request...");
    //     var files = await this.diskLogger.ReadRequestsFromDiskAsync();
    //     var filesSplit = files
    //         .Select(f =>
    //         {
    //             var nameSplits = f.FileName.Split(".");
    //             return new LogFileInfo(
    //                 nameSplits[0],
    //                 nameSplits[1],
    //                 f.RequestData.CorrelationId,
    //                 nameSplits[2],
    //                 f.RequestData.FullTranscript);
    //         })
    //         .ToImmutableArray();
    //     var llmRequests = new List<LlmRequest>();
    //     foreach (var file in filesSplit.Where(f => f.RequestId != "UserTranscript"))
    //     {
    //         llmRequests.Add(
    //             new LlmRequest(
    //                 file.FullRequestId,
    //                 file.Content,
    //                 Output: string.Empty));
    //     }
    //     var messages = new List<TranscriptMessage>();
    //     foreach (var file in filesSplit.Where(f => f.RequestId == "UserTranscript"))
    //     {
    //         messages.Add(new TranscriptMessage(
    //             $"{file.SessionId}.{file.MessageId}",
    //             file.CorrelationId,
    //             file.Content,
    //             llmRequests.Where(r => r.Id.StartsWith(file.FullMessageId)).ToImmutableArray()));
    //     }
    //     var sessions = messages
    //         .GroupBy(m => m.Id.Split(".")[0])
    //         .Select(group => new Session(group.Key, group.ToImmutableArray()));
    //     return new TranscriptResponse(sessions.ToImmutableArray());
    //     // string serialized = JsonSerializer.Serialize(sessionsResponse);
    //     // Get the response output stream
    //     // await using var output = response.OutputStream;
    //     // byte[] buffer = Encoding.UTF8.GetBytes(serialized);
    //     // await output.WriteAsync(buffer, 0, buffer.Length);
    // }
}

internal sealed record LogFileInfo(
    string SessionId,
    string MessageId,
    string CorrelationId,
    string RequestId,
    string Content)
{
    public string FullMessageId => $"{this.SessionId}.{this.MessageId}";

    public string FullRequestId => $"{this.FullMessageId}.{this.RequestId}";
}

internal sealed record TranscriptRequest();

internal sealed record TranscriptResponse(
    [property: JsonPropertyName("conversations")]
    ImmutableArray<Conversation> Conversations);

internal sealed record Conversation(
    [property: JsonPropertyName("conversationId")]
    string ConversationId,
    [property: JsonPropertyName("messages")]
    ImmutableArray<TranscriptMessage> Messages);

internal sealed record TranscriptMessage(
    [property: JsonPropertyName("incomingRequestId")]
    string IncomingRequestId,
    [property: JsonPropertyName("conversationId")]
    string ConversationId,
    [property: JsonPropertyName("content")]
    string Content,
    [property: JsonPropertyName("llmRequests")]
    ImmutableArray<LlmRequest> LlmRequests);

internal sealed record LlmRequest(
    [property: JsonPropertyName("parentIncomingRequestId")]
    string ParentIncomingRequestId,
    [property: JsonPropertyName("input")]
    ImmutableArray<LlmRequestInput> Input,
    [property: JsonPropertyName("output")]
    string Output);

internal sealed record LlmRequestInput(
    [property: JsonPropertyName("role")]
    string Role,
    [property: JsonPropertyName("content")]
    string Content);
