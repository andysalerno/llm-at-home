using System.Net;
using System.Text;
using System.Text.Json;
using AgentFlow;
using Microsoft.Extensions.Logging;

internal class OpenAIServerExample
{
    public static async Task ServeAsync()
    {
        var logger = Logging.Factory.CreateLogger<OpenAIServerExample>();

        var listener = new HttpListener();
        listener.Prefixes.Add("http://nzxt:8003/");
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
            string responseString = @"{ ""text"": ""example""  }";
            byte[] data = Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.LongLength;

            // Write out to the response stream (asynchronously), then close it
            logger.LogInformation("Responding...");
            await response.OutputStream.WriteAsync(data, 0, data.Length);
            response.Close();
            logger.LogInformation("Request complete.");
        }
    }

    private record ChatCompletionRequest(string Dummy);
}
