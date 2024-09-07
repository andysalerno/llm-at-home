using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples.Endpoints;

internal static class TranscriptEndpointHandler
{
    public static async Task HandleAsync(HttpListenerResponse response, ILogger<OpenAIServer> logger)
    {
        logger.LogInformation("invoking: transcripts");

        response.ContentType = "application/json";

        // Cors allow all:
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
        }

        // Get the response output stream
        await using var output = response.OutputStream;
        byte[] buffer = Encoding.UTF8.GetBytes(GetJsonBody());
        await output.WriteAsync(buffer, 0, buffer.Length);
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

    private const string HardcodedJsonResponse =
    """
    {
    "sessions": [
        {
        "id": "session1",
        "messages": [
            {
            "id": "msg1",
            "content": "Hello",
            "llmRequests": [
                {
                "id": "req1",
                "prompt": "User said: Hello",
                "response": "Hello! How can I assist you today?"
                },
                {
                "id": "req2",
                "prompt": "Analyze sentiment: Hello",
                "response": "Neutral"
                }
            ]
            },
            {
            "id": "msg2",
            "content": "How are you?",
            "llmRequests": [
                {
                "id": "req3",
                "prompt": "User said: How are you?",
                "response": "I'm an AI assistant, so I don't have feelings, but I'm functioning well and ready to help!"
                },
                {
                "id": "req4",
                "prompt": "Analyze intent: How are you?",
                "response": "Greeting, Casual conversation"
                }
            ]
            }
        ]
        }
    ]
    }
    """;

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
