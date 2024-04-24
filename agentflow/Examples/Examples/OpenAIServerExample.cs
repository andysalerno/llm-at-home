using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFlow;
using Microsoft.Extensions.Logging;

internal class OpenAIServerExample
{
    public static async Task ServeAsync()
    {
        var logger = Logging.Factory.CreateLogger<OpenAIServerExample>();

        var listener = new HttpListener();
        listener.Prefixes.Add("http://*:8003/");
        listener.Start();

        await HandleIncomingConnections(listener, logger);

        listener.Close();
    }

    private static async Task HandleIncomingConnections(HttpListener listener, ILogger<OpenAIServerExample> logger)
    {
        while (true)
        {
            logger.LogInformation("Waiting for request...");
            HttpListenerContext ctx = await listener.GetContextAsync(); // Wait for a request
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;
            logger.LogInformation("Incoming request received...");

            ChatCompletionRequest chatRequest;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                string content = await reader.ReadToEndAsync();
                logger.LogInformation("Got request: {Content}", content);
                chatRequest = JsonSerializer.Deserialize<ChatCompletionRequest>(content)
                    ?? throw new InvalidOperationException($"Could not parse request type as a ChatCompletionRequest: {content}");
            }

            // Write the response info
            var firstResponse = new ChatCompletionStreamingResponse(
                [
                    new ChatChoice(
                            Index: 0,
                            Delta: new Delta(Role: "assistant", Content: "Hi! How are you??"))
                ]);

            var finalResponse = new ChatCompletionStreamingResponse(
                [
                    new ChatChoice(
                            Index: 1,
                            FinishReason: "stop",
                            Delta: new Delta(Role: "assistant", Content: string.Empty))
                ]);

            response.ContentType = "text/event-stream; charset=utf-8";
            response.AddHeader("cache-control", "no-cache");
            response.AddHeader("x-accel-buffering", "no");
            response.AddHeader("Transfer-Encoding", "chunked");
            response.ContentEncoding = Encoding.UTF8;

            // Write out to the response stream (asynchronously), then close it
            logger.LogInformation("Responding...");
            await SendStreamingResponseAsync(response.OutputStream, firstResponse, logger);
            await SendStreamingResponseAsync(response.OutputStream, finalResponse, logger);
            response.Close();
            logger.LogInformation("Request complete.");
        }
    }

    private static async Task SendStreamingResponseAsync(Stream stream, ChatCompletionStreamingResponse response, ILogger logger)
    {
        string responseString = SerializeStreamingResponse(response);

        logger.LogInformation("Sending over stream: {Sending}", responseString);

        byte[] data = Encoding.UTF8.GetBytes(responseString);
        await stream.WriteAsync(data, 0, data.Length);
    }

    private static string SerializeStreamingResponse(ChatCompletionStreamingResponse response)
    {
        var builder = new StringBuilder();

        builder.Append("data: ");

        string serialized = JsonSerializer.Serialize(response);

        builder.Append(serialized);

        builder.Append("\n\n");

        return builder.ToString();
    }

    private record ChatCompletionRequest(string Dummy);

    private record ChatCompletionStreamingResponse(
        [property: JsonPropertyName("choices")] ImmutableArray<ChatChoice> Choices,
        [property: JsonPropertyName("model")] string Model = "mymodel",
        [property: JsonPropertyName("object")] string Object = "chat.completion.chunk");

    private record ChatChoice(
       [property: JsonPropertyName("index")] int Index,
       [property: JsonPropertyName("delta")] Delta Delta,
       [property: JsonPropertyName("finish_reason")] string? FinishReason = null);

    private record Delta(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);
}
