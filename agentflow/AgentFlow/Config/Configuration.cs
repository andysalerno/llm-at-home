namespace AgentFlow.Config;

public interface ICompletionsEndpointConfig
{
    Uri CompletionsEndpoint { get; }
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
    public Configuration(Uri completionsEndpoint)
    {
        this.CompletionsEndpoint = completionsEndpoint;
    }

    public Uri CompletionsEndpoint { get; }

    public bool LogRequestsToLlm { get; init; }

    public string PromptDirectory { get; init; } = "./Prompts";
}
