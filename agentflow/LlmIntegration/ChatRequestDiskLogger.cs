using System.Diagnostics;
using System.Text;
using AgentFlow.Config;
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

    public async Task LogRequestToDiskAsync(IEnumerable<LlmClient.Message> messages)
    {
        var logger = this.GetLogger();

        string? requestId = Activity.Current?.GetBaggageItem("requestId");

        int requestIndex = this.GetAndIncrementRequestIndex();

        if (requestId == null)
        {
            logger.LogWarning("RequestId information was not found on current activity");
            requestId = Guid.NewGuid().ToString();
        }

        string timestamp = DateTime.Now.ToString("dd-MM-yyyy");

        string logFilePath = $"{this.config.DiskLoggingPath.TrimEnd('/')}/{timestamp}.{requestId}.{requestIndex}.log";
        string fullPath = Path.GetFullPath(logFilePath);

        logger.LogInformation("Logging chat request to disk at path: {Path}, fullpath: {FullPath}", logFilePath, fullPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Could not create directory"));

        var logContentBuilder = new StringBuilder();

        foreach (var message in messages)
        {
            logContentBuilder.Append($"<|{message.Role.Name}|>\n");
            logContentBuilder.Append(message.Content);
            logContentBuilder.Append("<|end|>\n");
        }

        await File.WriteAllTextAsync(fullPath, logContentBuilder.ToString());
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
}
