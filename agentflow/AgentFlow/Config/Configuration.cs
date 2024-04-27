using System.Reflection.Emit;

namespace AgentFlow.Config;

public interface ICompletionsEndpointConfig
{
    Uri CompletionsEndpoint { get; }

    Uri EmbeddingsEndpoint { get; }

    Uri ScraperEndpoint { get; }
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
    public Configuration(Uri completionsEndpoint, Uri embeddingsEndpoint, Uri scraperEndpoint)
    {
        this.CompletionsEndpoint = completionsEndpoint;
        this.EmbeddingsEndpoint = embeddingsEndpoint;
        this.ScraperEndpoint = scraperEndpoint;
    }

    public Uri CompletionsEndpoint { get; }

    public Uri EmbeddingsEndpoint { get; }

    public Uri ScraperEndpoint { get; }

    public bool LogRequestsToLlm { get; init; }

    public string PromptDirectory { get; init; } = "./Prompts";
}
