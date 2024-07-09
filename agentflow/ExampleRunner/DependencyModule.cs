using AgentFlow.Agents;
using AgentFlow.CodeExecution;
using AgentFlow.Examples;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.LlmClients.OpenAI;
using AgentFlow.Prompts;
using AgentFlow.WorkSpace;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentFlow;

internal sealed class DependencyModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // builder.RegisterType<PodmanPythonCodeExecutor>().AsImplementedInterfaces();
        builder.RegisterType<HostedCodeExecutor>().AsImplementedInterfaces();
        builder.RegisterType<CellRunner<ConversationThread>>().AsImplementedInterfaces();
        builder.RegisterType<ChatMLMessageFormatter>().AsImplementedInterfaces();
        builder.RegisterType<UserConsoleAgent>();
        builder.RegisterType<EnvironmentVariableProvider>().AsImplementedInterfaces();

        // builder.RegisterType<GroqCompletionsClient>().AsImplementedInterfaces();
        // builder.RegisterType<VllmCompletionsClient>().AsImplementedInterfaces();
        builder.RegisterType<OpenAICompletionsClient>().AsImplementedInterfaces();
        builder.RegisterType<PromptParser>().AsImplementedInterfaces();
        builder.RegisterType<PromptRenderer>().AsImplementedInterfaces();
        builder.RegisterType<FileSystemPromptFactoryProvider>().AsImplementedInterfaces();
        builder.RegisterType<CustomAgentBuilderFactory>();

        // Runnable example classes:
        builder.RegisterType<WebSearchExample>();
        builder.RegisterType<SimpleChatExample>();
        builder.RegisterType<AgentBenchExample>();
        builder.RegisterType<OpenAIServerWebSearchExample>();
    }
}

internal sealed class LocalEnvironmentModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddHttpClient();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddSimpleConsole(config =>
            {
                config.IncludeScopes = true;
                config.SingleLine = true;
                config.TimestampFormat = "HH:mm::ss::ff";
            });
        });

        builder.Populate(serviceCollection);
    }
}
