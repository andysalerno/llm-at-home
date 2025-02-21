using System.Collections.Immutable;
using AgentFlow;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Config;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;

namespace Agentflow.Server.Handler;

internal sealed class ChatCompletionsHandler : IHandler<ChatCompletionsRequest, ChatCompletionsResponse>
{
    private readonly IFactoryProvider<Prompt, PromptName> promptFactoryProvider;
    private readonly CustomAgentBuilderFactory agentBuilderFactory;
    private readonly Configuration configuration;
    private readonly ILogger<ChatCompletionsHandler> logger;

    public ChatCompletionsHandler(
        IFactoryProvider<Prompt, PromptName> promptFactoryProvider,
        CustomAgentBuilderFactory agentBuilderFactory,
        Configuration configuration,
        ILogger<ChatCompletionsHandler> logger)
    {
        this.promptFactoryProvider = promptFactoryProvider;
        this.agentBuilderFactory = agentBuilderFactory;
        this.configuration = configuration;
        this.logger = logger;
    }

    public Task<ChatCompletionsResponse> HandleAsync(ChatCompletionsRequest payload)
    {
        this.logger.LogInformation("payload received :)");
        return Task.FromResult(new ChatCompletionsResponse());
    }

    public Cell<ConversationThread> CreateProgram()
    {
        ImmutableArray<ITool> tools = [
            // new WebSearchTool(
            //     this.agentBuilderFactory,
            //     this.runner,
            //     this.environmentVariableProvider,
            //     this.embeddingsClient,
            //     this.scraperClient,
            //     this.promptRenderer,
            //     this.promptFactoryProvider.GetFactory(ExamplePrompts.RewriteQuerySystem),
            //     this.httpClientFactory),

            // new WebSearchTool(
            //     this.agentBuilderFactory,
            //     this.runner,
            //     this.environmentVariableProvider,
            //     this.embeddingsClient,
            //     this.scraperClient,
            //     this.promptRenderer,
            //     this.promptFactoryProvider.GetFactory(ExamplePrompts.RewriteQuerySystem),
            //     this.httpClientFactory,
            //     searchSiteUris: ["nytimes.com", "cnn.com", "apnews.com", "cbsnews.com"],
            //     toolName: "search_news",
            //     exampleQueries: ("2024 election polls", "seattle heat wave", "stock market performance")),
        ];

        return new AgentCell(
            new ToolAgent(
                new AgentName("WebSearchAgent"),
                Role.Assistant,
                this.promptFactoryProvider,
                this.agentBuilderFactory,
                this.configuration.InstructionStrategy,
                tools));
    }
}

internal sealed record ChatCompletionsRequest();

internal sealed record ChatCompletionsResponse();
