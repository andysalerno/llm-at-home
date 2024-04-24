using System.Collections.Immutable;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using AgentFlow;
using AgentFlow.LlmClient;
using Microsoft.Extensions.Logging;

// 20:28::03::30info: OpenAIServerExample[0] Got request: {   "model": "andymodel",   "messages": [     {       "role": "system",       "content": "You are a summarization AI. Summarize the user's request into a single short sentence of four words or less. Do not try to answer it, only summarize the user's query. Always start your answer with an emoji relevant to the summary"     },     {       "role": "user",       "content": "Who is the president of Gabon?"     },     {       "role": "assistant",       "content": "🇬🇦 President of Gabon"     },     {       "role": "user",       "content": "Who is Julien Chaumond?"     },     {       "role": "assistant",       "content": "🧑 Julien Chaumond"     },     {       "role": "user",       "content": "what is 1 + 1?"     },     {       "role": "assistant",       "content": "🔢 Simple math operation"     },     {       "role": "user",       "content": "What are the latest news?"     },     {       "role": "assistant",       "content": "📰 Latest news"     },     {       "role": "user",       "content": "How to make a great cheesecake?"     },     {       "role": "assistant",       "content": "🍰 Cheesecake recipe"     },     {       "role": "user",       "content": "what is your favorite movie? do a short answer."     },     {       "role": "assistant",       "content": "🎥 Favorite movie"     },     {       "role": "user",       "content": "Explain the concept of artificial intelligence in one sentence"     },     {       "role": "assistant",       "content": "🤖 AI definition"     },     {       "role": "user",       "content": "hi"     }   ],   "stream": true,   "max_tokens": 15,   "stop": [],   "temperature": 0.9,   "top_p": 0.95,   "frequency_penalty": 1.2 }
// 20:28::03::32info: OpenAIServerExample[0] Got request: {   "model": "andymodel",   "messages": [     {       "role": "system",       "content": ""     },     {       "role": "user",       "content": "hi"     }   ],   "stream": true,   "max_tokens": 1024,   "stop": [],   "temperature": 0.9,   "top_p": 0.95,   "frequency_penalty": 1.2 }

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
            string responseString = SerializeStreamingResponse(
                new ChatCompletionStreamingResponse([
                    new ChatChoice(
                        Index: 0,
                        FinishReason: "stop",
                        Delta: new Delta(Role: "assistant", Content: "hi!")),
                ]));

            byte[] data = Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "text/event-stream; charset=utf-8";
            response.AddHeader("cache-control", "no-cache");
            response.AddHeader("x-accel-buffering", "no");
            response.AddHeader("Transfer-Encoding", "chunked");
            response.ContentEncoding = Encoding.UTF8;
            // response.ContentLength64 = data.LongLength;

            // Write out to the response stream (asynchronously), then close it
            logger.LogInformation("Responding...");
            await response.OutputStream.WriteAsync(data, 0, data.Length);
            response.Close();
            logger.LogInformation("Request complete.");
        }
    }

    private static async Task SendStreamingResponse(ChatCompletionStreamingResponse response)

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
        ImmutableArray<ChatChoice> Choices,
        string Model = "mymodel",
        string Object = "chat.completion.chunk");

    private record ChatChoice(
        int Index,
        string FinishReason,
        Delta Delta);

    private record Delta(string Role, string Content);
}
