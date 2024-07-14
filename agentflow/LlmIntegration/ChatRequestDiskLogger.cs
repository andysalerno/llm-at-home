using System.Text;
using AgentFlow.Config;
using Microsoft.Extensions.Logging;

namespace AgentFlow.LlmClients;

public sealed class ChatRequestDiskLogger
{
    private readonly IChatRequestDiskLoggerConfig config;

    public ChatRequestDiskLogger(IChatRequestDiskLoggerConfig config)
    {
        this.config = config;
    }

    public async Task LogRequestToDiskAsync(ChatCompletionsRequest chatCompletionsRequest)
    {
        var logger = this.GetLogger();

        string logFilePath = $"{this.config.DiskLoggingPath.TrimEnd('/')}/{Guid.NewGuid()}.log";

        logger.LogInformation("Logging chat request to disk at path: {Path}", logFilePath);

        var logContentBuilder = new StringBuilder();

        foreach (var message in chatCompletionsRequest.Messages)
        {
            logContentBuilder.Append($"<|{message.Role.Name}|>\n");
            logContentBuilder.Append(message.Content);
            logContentBuilder.Append("<|end|>");
        }

        await File.WriteAllTextAsync(logFilePath, logContentBuilder.ToString());
    }
}
