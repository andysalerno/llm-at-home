using System.Reflection.Emit;

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

public class Configuration : ICompletionsEndpointConfig, ILoggingConfig, IFileSystemPromptProviderConfig
{
    public Configuration(Uri completionsEndpoint, Uri embeddingsEndpoint, Uri scraperEndpoint, string modelName)
    {
        this.CompletionsEndpoint = completionsEndpoint;
        this.EmbeddingsEndpoint = embeddingsEndpoint;
        this.ScraperEndpoint = scraperEndpoint;
        this.ModelName = modelName;
    }

    public Uri CompletionsEndpoint { get; }

    public Uri EmbeddingsEndpoint { get; }

    public Uri ScraperEndpoint { get; }

    public bool LogRequestsToLlm { get; init; }

    public string PromptDirectory { get; init; } = "./Prompts";

    public string ModelName { get; }
}
