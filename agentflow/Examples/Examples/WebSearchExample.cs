using System.Collections.Immutable;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Config;
using AgentFlow.Examples.Agents;
using AgentFlow.Examples.Tools;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;

namespace AgentFlow.Examples;

internal static class WebSearchExample
{
    public static Cell<ConversationThread> CreateDefinition(
        IAgent userConsoleAgent,
        IHttpClientFactory httpClientFactory,
        IEmbeddingsClient embeddingsClient,
        IScraperClient scraperClient,
        CustomAgentBuilderFactory customAgentBuilderFactory,
        IFileSystemPromptProviderConfig promptProviderConfig)
    {
        var prompt = new FileSystemPromptProvider(
            "websearch_example_system",
            promptProviderConfig)
            .Get();

        ImmutableArray<ITool> tools = [new WebSearchTool(embeddingsClient, scraperClient, httpClientFactory)];

        // TODO: BeginLoop().WithSequence().AddAgent().AddAgent().EndLoop();
        var loopForever = new WhileCell<ConversationThread>()
        {
            WhileTrue = new CellSequence<ConversationThread>(
                sequence: new Cell<ConversationThread>[]
                {
                    new AgentCell(userConsoleAgent),
                    new AgentCell(
                        new ToolAgent(
                            new AgentName("WebSearchAgent"),
                            Role.Assistant,
                            prompt,
                            customAgentBuilderFactory,
                            tools)),
                }.ToImmutableArray()),
        };

        return loopForever;
    }
}
