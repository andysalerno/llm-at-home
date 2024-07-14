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

public class Configuration :
    ICompletionsEndpointConfig,
    ILoggingConfig,
    IFileSystemPromptProviderConfig,
    IChatRequestDiskLoggerConfig
{
    public Configuration(
        Uri completionsEndpoint,
        Uri embeddingsEndpoint,
        Uri scraperEndpoint,
        string modelName,
        string promptDirectory,
        string diskLoggingPath)
    {
        this.CompletionsEndpoint = completionsEndpoint;
        this.EmbeddingsEndpoint = embeddingsEndpoint;
        this.ScraperEndpoint = scraperEndpoint;
        this.ModelName = modelName;
        this.PromptDirectory = promptDirectory;
        this.DiskLoggingPath = diskLoggingPath;
    }

    public Uri CompletionsEndpoint { get; }

    public Uri EmbeddingsEndpoint { get; }

    public Uri ScraperEndpoint { get; }

    public bool LogRequestsToLlm { get; init; }

    public string PromptDirectory { get; }

    public string ModelName { get; }

    public string DiskLoggingPath { get; }
}
