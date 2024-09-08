using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFlow.LlmClient;
using AgentFlow.LlmClients;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples.Endpoints;

internal static class TranscriptEndpointHandler
{
    public static async Task HandleAsync(
        HttpListenerResponse response,
        ChatRequestDiskLogger diskLogger,
        ILogger<OpenAIServer> logger)
    {
        logger.LogInformation("invoking: transcripts");

        response.ContentType = "application/json";

        // Cors allow all:
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
        }

        var files = await diskLogger.ReadRequestsFromDiskAsync();

        var llmRequests = new List<LlmRequest>();

        foreach (var file in files)
        {
            string sessionId;
            string messageId;
            string requestId;
            {
                var splits = file.FileName.Split(".");
                sessionId = splits[0];
                messageId = splits[1];
                requestId = splits[2];
            }

            if (requestId == "UserTranscript")
            {
                continue;
            }

            llmRequests.Add(
                new LlmRequest($"{sessionId}.{messageId}.{requestId}", file.FileContent, Output: string.Empty));
        }

        var messages = new List<Message>();
        foreach (var file in files)
        {
            string sessionId;
            string messageId;
            string requestId;
            {
                var splits = file.FileName.Split(".");
                sessionId = splits[0];
                messageId = splits[1];
                requestId = splits[2];
            }

            if (requestId != "UserTranscript")
            {
                continue;
            }

            messages.Add(new Message(
                $"{sessionId}.{messageId}",
                file.FileContent,
                llmRequests.Where(r => r.Id.StartsWith($"{sessionId}.{messageId}")).ToImmutableArray()));
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

    private sealed record LogFileParts(string SessionId, string MessageId, string RequestId);

    private static ImmutableArray<Session> CreateSessionsFromFileNames(IEnumerable<LogFileParts> parts)
    {
        var sessions = ImmutableArray.CreateBuilder<Session>();

        foreach (string sessionId in parts.Select(p => p.SessionId).Distinct())
        {
            var messages = CreateMessagesFromFileNames(sessionId, parts);

            sessions.Add(new Session(sessionId, messages));
        }

        return sessions.DrainToImmutable();
    }

    private static ImmutableArray<Message> CreateMessagesFromFileNames(string sessionId, IEnumerable<LogFileParts> parts)
    {
        // the user-level messages are in the file: <sessionId>.<messageId>.UserTranscript.log
        var messageIds = parts
            .Where(part => part.SessionId == sessionId)
            .Select(part => part.MessageId)
            .Distinct();

        foreach (string messageId in messageIds)
        {
            string transcriptLogName = $"{sessionId}.{messageId}.UserTranscript.log";

        }

        return ImmutableArray.Create<Message>();
    }

    private static ImmutableArray<LlmRequest> CreateRequestsFromFileNames(IEnumerable<LogFileParts> parts)
    {
        return ImmutableArray.Create<LlmRequest>();
    }

    private static string GetJsonBody()
    {
        var payload = new Response([
            new Session("someSession", [
                new Message("someMessage", "someMessageContent", [
                    new LlmRequest("someLlmRequest", "someLlmInput", "someLlmOutput")
                ])
            ])
        ]);

        return JsonSerializer.Serialize(payload);
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
