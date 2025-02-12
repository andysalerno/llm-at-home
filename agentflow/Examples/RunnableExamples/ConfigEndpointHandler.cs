using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFlow.LlmClients;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples.Endpoints;

internal static class ConfigEngpointHandler
{
    public static async Task HandleAsync(
        HttpListenerResponse response,
        ChatRequestDiskLogger diskLogger,
        ILogger<OpenAIServer> logger)
    {
        logger.LogInformation("invoking: config");

        response.ContentType = "application/json";

        // Cors allow all:
        response.SetCorsAllowAllOrigins();

        var files = await diskLogger.ReadRequestsFromDiskAsync();

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

        var messages = new List<Message>();
        foreach (var file in filesSplit.Where(f => f.RequestId == "UserTranscript"))
        {
            messages.Add(new Message(
                $"{file.SessionId}.{file.MessageId}",
                file.CorrelationId,
                file.Content,
                llmRequests.Where(r => r.Id.StartsWith(file.FullMessageId)).ToImmutableArray()));
        }

        var sessions = messages
            .GroupBy(m => m.Id.Split(".")[0])
            .Select(group => new Session(group.Key, group.ToImmutableArray()));
        var sessionsResponse = new Response(sessions.ToImmutableArray());

        string serialized = JsonSerializer.Serialize(sessionsResponse);

        // Get the response output stream
        await using var output = response.OutputStream;
        byte[] buffer = Encoding.UTF8.GetBytes(serialized);
        await output.WriteAsync(buffer, 0, buffer.Length);
    }

    private sealed record LogFileInfo(
        string SessionId,
        string MessageId,
        string CorrelationId,
        string RequestId,
        string Content)
    {
        public string FullMessageId => $"{this.SessionId}.{this.MessageId}";

        public string FullRequestId => $"{this.FullMessageId}.{this.RequestId}";
    }

    private sealed record Response(
        [property: JsonPropertyName("sessions")]
        ImmutableArray<Session> Sessions);

    private sealed record Session(
        [property: JsonPropertyName("id")]
        string Id,
        [property: JsonPropertyName("messages")]
        ImmutableArray<Message> Messages);

    private sealed record Message(
        [property: JsonPropertyName("id")]
        string Id,
        [property: JsonPropertyName("correlationId")]
        string CorrelationId,
        [property: JsonPropertyName("content")]
        string Content,
        [property: JsonPropertyName("llmRequests")]
        ImmutableArray<LlmRequest> LlmRequests);

    private sealed record LlmRequest(
        [property: JsonPropertyName("id")]
        string Id,
        [property: JsonPropertyName("input")]
        string Input,
        [property: JsonPropertyName("output")]
        string Output);
}
