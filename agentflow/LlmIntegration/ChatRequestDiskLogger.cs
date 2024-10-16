using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFlow.Config;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.LlmClients;

public sealed class ChatRequestDiskLogger
{
    private static readonly object @lock = new object();

    private readonly IChatRequestDiskLoggerConfig config;

    public ChatRequestDiskLogger(IChatRequestDiskLoggerConfig config)
    {
        this.config = config;
    }

    public async Task LogUserVisibleTranscriptToDiskAsync(ConversationThread conversationThread)
    {
        await this.LogRequestToDiskAsync(conversationThread.Messages, "UserTranscript");
    }

    public async Task LogRequestToDiskAsync(IEnumerable<LlmClient.Message> messages)
    {
        int requestIndex = this.GetAndIncrementRequestIndex();

        await this.LogRequestToDiskAsync(messages, requestIndex.ToString());
    }

    public async Task<ImmutableArray<(string FileName, RequestData RequestData)>> ReadRequestsFromDiskAsync()
    {
        string logFileDir = this.config.DiskLoggingPath;
        string fullPath = Path.GetFullPath(logFileDir);

        var logger = this.GetLogger();

        var files = new DirectoryInfo(fullPath)
            .GetFiles()
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToImmutableArray();

        var results = ImmutableArray.CreateBuilder<(string, RequestData)>();

        foreach (var file in files)
        {
            logger.LogInformation("Reading requestdata file: {FileName}", file.FullName);

            string content = await File.ReadAllTextAsync(file.FullName);

            var requestData = JsonSerializer.Deserialize<RequestData>(content)
                ?? throw new InvalidOperationException("The file on disk could not be deserialized to RequestData.");

            results.Add((file.Name, requestData));
        }

        this.GetLogger().LogInformation("Read requests from disk: {Count}", results.Count);

        return results.DrainToImmutable();
    }

    private async Task LogRequestToDiskAsync(IEnumerable<LlmClient.Message> messages, string fileIndex)
    {
        var logger = this.GetLogger();

        string? requestId = Activity.Current?.GetBaggageItem("requestId");

        if (requestId == null)
        {
            logger.LogWarning("RequestId information was not found on current activity");
            requestId = Guid.NewGuid().ToString();
        }

        string timestamp = DateTime.Now.ToString("dd-MM-yyyy");

        string logFilePath = $"{this.config.DiskLoggingPath.TrimEnd('/')}/{timestamp}.{requestId}.{fileIndex}.log";
        string fullPath = Path.GetFullPath(logFilePath);

        logger.LogInformation("Logging chat request to disk at path: {Path}, fullpath: {FullPath}", logFilePath, fullPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Could not create directory"));

        string correlationId = Activity.Current?.GetBaggageItem("correlationId")
            ?? Guid.NewGuid().ToString();

        var logContentBuilder = new StringBuilder();

        foreach (var message in messages)
        {
            logContentBuilder.Append($"<|{message.Role.Name}|>\n");
            logContentBuilder.Append(message.Content);
            logContentBuilder.Append("<|end|>\n");
        }

        var requestData = new RequestData(correlationId, logContentBuilder.ToString());

        var json = JsonSerializer.Serialize(requestData);

        await File.WriteAllTextAsync(fullPath, json);
    }

    private int GetAndIncrementRequestIndex()
    {
        int requestIndexVal = 0;

        lock (@lock)
        {
            string? requestIndex = Activity.Current?.GetBaggageItem("requestIndex");

            int.TryParse(requestIndex, out requestIndexVal);

            Activity.Current?.SetBaggage("requestIndex", (requestIndexVal + 1).ToString());
        }

        return requestIndexVal;
    }

    public sealed record RequestData(
        [property: JsonPropertyName("correlationId")]
        string CorrelationId,
        [property: JsonPropertyName("fullTranscript")]
        string FullTranscript);
}
