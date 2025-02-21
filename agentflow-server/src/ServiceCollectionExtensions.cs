using AgentFlow;
using AgentFlow.Agents;
using AgentFlow.Config;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.LlmClients;
using AgentFlow.Prompts;
using AgentFlow.WorkSpace;

namespace Agentflow.Server;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentFlow(this IServiceCollection services)
    {
        services.AddSingleton<ICellRunner<ConversationThread>, CellRunner<ConversationThread>>();
        services.AddSingleton<IMessageFormatter, ChatMLMessageFormatter>();
        services.AddSingleton<IEnvironmentVariableProvider, EnvironmentVariableProvider>();

        services.AddSingleton<ILlmCompletionsClient, OpenAICompletionsClient>();
        services.AddSingleton<IEmbeddingsClient, OpenAICompletionsClient>();
        services.AddSingleton<IScraperClient, OpenAICompletionsClient>();

        services.AddSingleton<IPromptParser, PromptParser>();
        services.AddSingleton<IPromptRenderer, PromptRenderer>();
        services.AddSingleton<IFactoryProvider<Prompt, PromptName>, FileSystemPromptFactoryProvider>();
        services.AddSingleton<CustomAgentBuilderFactory>();

        // services.AddSingleton<Configuration>(configuration);
        // services.AddSingleton<ICompletionsEndpointConfig>(configuration);
        // services.AddSingleton<ILoggingConfig>(configuration);
        // services.AddSingleton<IFileSystemPromptProviderConfig>(configuration);
        // services.AddSingleton<IChatRequestDiskLoggerConfig>(configuration);
        // services.AddSingleton<IPromptRendererConfig>(configuration);
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();

        var configuration = configBuilder.Build().GetRequiredSection("AgentFlow").Get<Configuration>()
            ?? throw new InvalidOperationException("Configuration section 'agentflow' is missing or invalid.");

        // services.Configure<Configuration>(configBuilder.Build());
        services.AddSingleton<Configuration>(configuration);
        services.AddSingleton<ICompletionsEndpointConfig>(sc => sc.GetRequiredService<Configuration>());
        services.AddSingleton<ILoggingConfig>(sc => sc.GetRequiredService<Configuration>());
        services.AddSingleton<IFileSystemPromptProviderConfig>(sc => sc.GetRequiredService<Configuration>());
        services.AddSingleton<IChatRequestDiskLoggerConfig>(sc => sc.GetRequiredService<Configuration>());
        services.AddSingleton<IPromptRendererConfig>(sc => sc.GetRequiredService<Configuration>());

        return services;
    }
}