using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AgentFlow.LlmClients;

namespace Agentflow.Server.Handler;

internal sealed class TranscriptHandler : IHandler<TranscriptRequest, TranscriptResponse>
{
    private readonly ChatRequestDiskLogger diskLogger;
    private readonly ILogger<TranscriptHandler> logger;

    public TranscriptHandler(ChatRequestDiskLogger diskLogger, ILogger<TranscriptHandler> logger)
    {
        this.diskLogger = diskLogger;
        this.logger = logger;
    }

    public async Task<TranscriptResponse> HandleAsync(
        TranscriptRequest payload)
    {
        this.logger.LogInformation("Handling transcript request...");
        var files = await this.diskLogger.ReadRequestsFromDiskAsync();

        var filesSplit = files
            .Select(f =>
            {
                var nameSplits = f.FileName.Split(".");

                return new LogFileInfo(
                    nameSplits[0],
                    nameSplits[1],
                    f.RequestData.CorrelationId,
                    nameSplits[2],
                    f.RequestData.FullTranscript);
            })
            .ToImmutableArray();

        var llmRequests = new List<LlmRequest>();

        foreach (var file in filesSplit.Where(f => f.RequestId != "UserTranscript"))
        {
            llmRequests.Add(
                new LlmRequest(
                    file.FullRequestId,
                    file.Content,
                    Output: string.Empty));
        }

        var messages = new List<TranscriptMessage>();
        foreach (var file in filesSplit.Where(f => f.RequestId == "UserTranscript"))
        {
            messages.Add(new TranscriptMessage(
                $"{file.SessionId}.{file.MessageId}",
                file.CorrelationId,
                file.Content,
                llmRequests.Where(r => r.Id.StartsWith(file.FullMessageId)).ToImmutableArray()));
        }

        var sessions = messages
            .GroupBy(m => m.Id.Split(".")[0])
            .Select(group => new Session(group.Key, group.ToImmutableArray()));

        return new TranscriptResponse(sessions.ToImmutableArray());

        // string serialized = JsonSerializer.Serialize(sessionsResponse);
        // Get the response output stream
        // await using var output = response.OutputStream;
        // byte[] buffer = Encoding.UTF8.GetBytes(serialized);
        // await output.WriteAsync(buffer, 0, buffer.Length);
    }
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
    [property: JsonPropertyName("sessions")]
    ImmutableArray<Session> Sessions);

internal sealed record Session(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("messages")]
    ImmutableArray<TranscriptMessage> Messages);

internal sealed record TranscriptMessage(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("correlationId")]
    string CorrelationId,
    [property: JsonPropertyName("content")]
    string Content,
    [property: JsonPropertyName("llmRequests")]
    ImmutableArray<LlmRequest> LlmRequests);

internal sealed record LlmRequest(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("input")]
    string Input,
    [property: JsonPropertyName("output")]
    string Output);
