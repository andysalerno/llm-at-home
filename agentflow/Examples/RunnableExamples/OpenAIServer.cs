using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFlow.Examples.Endpoints;
using AgentFlow.LlmClient;
using AgentFlow.LlmClients;
using AgentFlow.Util;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples;

internal sealed class OpenAIServer
{
    private const string PassthruModelName = "passthru";

    public async Task ServeAsync(
        Cell<ConversationThread> program,
        Cell<ConversationThread> passthruProgram,
        ICellRunner<ConversationThread> runner,
        ChatRequestDiskLogger diskLogger,
        int port = 8003)
    {
        var logger = this.GetLogger();

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{port}/");
        listener.Start();

        await HandleIncomingConnectionsAsync(listener, program, passthruProgram, runner, diskLogger, logger);

        listener.Close();
    }

    private static async Task HandleIncomingConnectionsAsync(
        HttpListener listener,
        Cell<ConversationThread> program,
        Cell<ConversationThread> passthruProgram,
        ICellRunner<ConversationThread> runner,
        ChatRequestDiskLogger diskLogger,
        ILogger<OpenAIServer> logger)
    {
        while (true)
        {
            logger.LogInformation("Waiting for request...");
            HttpListenerContext ctx = await listener.GetContextAsync(); // Wait for a request
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;

            logger.LogInformation("AbsolutePath: {Path}", request.Url?.AbsolutePath);
            logger.LogInformation("AbsoluteUri: {Path}", request.Url?.AbsoluteUri);
            logger.LogInformation("RawUrl: {Path}", request.RawUrl);

#pragma warning disable CA2000 // Dispose objects before losing scope
            using var activity = new Activity("incomingHTTP")
                .Start();
#pragma warning restore CA2000 // Dispose objects before losing scope

            if (request.Headers.GetValues("X-Correlation-ID")?.FirstOrDefault()
                is string correlationId)
            {
                activity.AddBaggage("correlationId", correlationId);

                logger.LogInformation("Setting correlationId to: {CorrelationId}", correlationId);

                response.AddHeader("X-Correlation-ID", correlationId);
            }

            try
            {
                var task = request.RawUrl switch
                {
                    "/v1/chat/completions" =>
                        HandleChatCompletionsAsync(
                            request,
                            response,
                            program,
                            passthruProgram,
                            runner,
                            diskLogger,
                            logger),
                    "/transcripts" =>
                        TranscriptEndpointHandler.HandleAsync(response, diskLogger, logger),
                    _ => throw new InvalidOperationException($"Unknown path: {request.RawUrl}"),
                };

                await task;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: "error");
            }
            finally
            {
                // The docs specify you should invoke `Close()`, instead of using `using`,
                // even though it's an `IDisposable`...
                response.Close();
                logger.LogInformation("Request complete.");
            }
        }
    }

    private static async Task HandleChatCompletionsAsync(
        HttpListenerRequest request,
        HttpListenerResponse response,
        Cell<ConversationThread> program,
        Cell<ConversationThread> passthruProgram,
        ICellRunner<ConversationThread> runner,
        ChatRequestDiskLogger diskLogger,
        ILogger<OpenAIServer> logger)
    {
        if (request.HttpMethod == HttpMethod.Options.Method)
        {
            await SendPreflightResponseAsync(response);

            return;
        }

        response.SetCorsAllowAllOrigins();

        try
        {
            ChatCompletionRequest chatRequest;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                string content = await reader.ReadToEndAsync();
                logger.LogInformation("Got request: {Content}", content);
                chatRequest = JsonSerializer.Deserialize<ChatCompletionRequest>(content)
                    ?? throw new InvalidOperationException(
                        $"Could not parse request type as a ChatCompletionRequest: {content}");
            }

            Cell<ConversationThread> programToUse = program;

            if (chatRequest.Model == PassthruModelName)
            {
                programToUse = passthruProgram;

                logger.LogInformation("Passthru model requesting - will handle by passthru agent");
            }

            ConversationThread conversationThread = ToConversationThread(chatRequest);

#pragma warning disable CA2000 // Dispose objects before losing scope
            using var activity = new Activity("request")
                .AddRequestIdBaggage()
                .Start();
#pragma warning restore CA2000 // Dispose objects before losing scope

            ConversationThread output = await runner.RunAsync(programToUse, rootInput: conversationThread);

            var lastMessage = output.Messages.Last();

            logger.LogDebug("Output last message: {}", lastMessage.Content);

            await diskLogger.LogUserVisibleTranscriptToDiskAsync(conversationThread.WithAddedMessage(lastMessage));

            if (lastMessage.Role != Role.Assistant)
            {
                logger.LogWarning(
                    "Last message was not from assistant - this might cause quality issues in the response.");
            }

            // Write the response info
            await SendResponseAsync(lastMessage.Content, response, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, message: "error");
        }
        finally
        {
            // The docs specify you should invoke `Close()`, instead of using `using`,
            // even though it's an `IDisposable`...
            response.Close();
            logger.LogInformation("Request complete.");
        }
    }

    private static async Task SendPreflightResponseAsync(HttpListenerResponse response)
    {
        // Cors allow all:
        response.SetCorsAllowAllOrigins();

        response.ContentEncoding = Encoding.UTF8;

        await response.OutputStream.WriteAsync(default);
    }

    private static async Task SendResponseAsync(
        string content,
        HttpListenerResponse response,
        ILogger<OpenAIServer> logger)
    {
        // Write the response info
        var firstResponse = new ChatCompletionStreamingResponse(
            [
                new ChatChoice(
                            Index: 0,
                            Delta: new Delta(Role: "assistant", Content: content))
            ]);

        var finalResponse = new ChatCompletionStreamingResponse(
            [
                new ChatChoice(
                            Index: 1,
                            Delta: new Delta(Role: "assistant", Content: string.Empty),
                            FinishReason: "stop")
            ]);

        response.ContentType = "text/event-stream; charset=utf-8";
        response.AddHeader("cache-control", "no-cache");
        response.AddHeader("x-accel-buffering", "no");
        response.AddHeader("Transfer-Encoding", "chunked");
        response.ContentEncoding = Encoding.UTF8;

        foreach (var streamingResponse in new[] { firstResponse, finalResponse })
        {
            await SendStreamingResponseAsync(response.OutputStream, streamingResponse, logger);
        }
    }

    private static async Task SendStreamingResponseAsync(
        Stream stream,
        ChatCompletionStreamingResponse response,
        ILogger logger)
    {
        string responseString = SerializeStreamingResponse(response);

        logger.LogInformation("Sending over stream: {Sending}", responseString);

        byte[] data = Encoding.UTF8.GetBytes(responseString);

        // await stream.WriteAsync(data, 0, data.Length);
        await stream.WriteAsync(data);
    }

    private static string SerializeStreamingResponse(ChatCompletionStreamingResponse response)
    {
        var builder = new StringBuilder();

        builder.Append("data: ");

        string serialized = JsonSerializer.Serialize(response);

        builder.Append(serialized)
            .Append("\n\n");

        return builder.ToString();
    }

    private static ConversationThread ToConversationThread(ChatCompletionRequest request)
    {
        var messages = request.Messages.Select(m => new AgentFlow.LlmClient.Message(
            AgentName: new AgentFlow.Agents.AgentName(m.Role),
            Role: Role.ExpectFromName(m.Role),
            Content: m.Content.Text));

        return ConversationThread.CreateBuilder().AddMessages(messages).Build();
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] ImmutableArray<Message> Messages);

    private sealed record Message(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonConverter(typeof(MessageContentConverter))]
        [property: JsonPropertyName("content")] MessageContent Content);

    private sealed record MessageContent(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("type")] string Type = "text");

    private sealed record ChatCompletionStreamingResponse(
        [property: JsonPropertyName("choices")] ImmutableArray<ChatChoice> Choices,
        [property: JsonPropertyName("model")] string Model = "mymodel",
        [property: JsonPropertyName("object")] string Object = "chat.completion.chunk");

    private sealed record ChatChoice(
       [property: JsonPropertyName("index")] int Index,
       [property: JsonPropertyName("delta")] Delta Delta,
       [property: JsonPropertyName("finish_reason")] string? FinishReason = null);

    private sealed record Delta(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed class MessageContentConverter : JsonConverter<MessageContent>
    {
        public override MessageContent? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string content = reader.GetString() ?? throw new JsonException();
                return new MessageContent(Text: content);
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
                {
                    var value = doc.RootElement.Clone();

                    var values = value.Deserialize<List<Dictionary<string, string>>>()
                        ?? throw new JsonException();

                    return new MessageContent(Text: values.First()["text"]);
                }
            }

            throw new JsonException($"Expected StartArray or String, saw {reader.TokenType}");
        }

        public override void Write(
            Utf8JsonWriter writer,
            MessageContent value,
            JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}

internal static class HttpListenerResponseExtensions
{
    public static void SetCorsAllowAllOrigins(this HttpListenerResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add(
            "Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With, X-Correlation-ID");
    }
}
