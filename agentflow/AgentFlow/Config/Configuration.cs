namespace AgentFlow.Config;

public interface ICompletionsEndpointConfig
{
    Uri CompletionsEndpoint { get; }

    Uri EmbeddingsEndpoint { get; }

    Uri ScraperEndpoint { get; }

    string ModelName { get; }
}

public interface ILoggingConfig
{
    bool LogRequestsToLlm { get; }
}

public interface IFileSystemPromptProviderConfig
{
    string PromptDirectory { get; }
}

public interface IChatRequestDiskLoggerConfig
{
    string DiskLoggingPath { get; }
}

public record Configuration(
    Uri CompletionsEndpoint,
    Uri EmbeddingsEndpoint,
    Uri ScraperEndpoint,
    bool LogRequestsToLlm,
    string PromptDirectory,
    string ModelName,
    string DiskLoggingPath) :
    ICompletionsEndpointConfig,
    ILoggingConfig,
    IFileSystemPromptProviderConfig,
    IChatRequestDiskLoggerConfig;
