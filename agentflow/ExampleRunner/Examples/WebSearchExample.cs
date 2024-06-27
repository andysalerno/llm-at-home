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
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples;

internal sealed class WebSearchExample : IRunnableExample
{
    private readonly ICellRunner<ConversationThread> runner;
    private readonly IAgent userConsoleAgent;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IEmbeddingsClient embeddingsClient;
    private readonly IScraperClient scraperClient;
    private readonly IPromptParser promptParser;
    private readonly CustomAgentBuilderFactory customAgentBuilderFactory;
    private readonly IFileSystemPromptProviderConfig promptProviderConfig;
    private readonly ILoggingConfig loggingConfig;

    public WebSearchExample(
        ICellRunner<ConversationThread> runner,
        IAgent userConsoleAgent,
        IHttpClientFactory httpClientFactory,
        IEmbeddingsClient embeddingsClient,
        IScraperClient scraperClient,
        IPromptParser promptParser,
        CustomAgentBuilderFactory customAgentBuilderFactory,
        IFileSystemPromptProviderConfig promptProviderConfig,
        ILoggingConfig loggingConfig)
    {
        this.runner = runner;
        this.userConsoleAgent = userConsoleAgent;
        this.httpClientFactory = httpClientFactory;
        this.embeddingsClient = embeddingsClient;
        this.scraperClient = scraperClient;
        this.promptParser = promptParser;
        this.customAgentBuilderFactory = customAgentBuilderFactory;
        this.promptProviderConfig = promptProviderConfig;
        this.loggingConfig = loggingConfig;
    }

    public Cell<ConversationThread> CreateDefinition()
    {
        ImmutableArray<ITool> tools = [
            new WebSearchTool(
                this.customAgentBuilderFactory,
                this.runner,
                this.embeddingsClient,
                this.scraperClient,
                new FileSystemPromptFactory("rewrite_query_system", this.promptParser, this.promptProviderConfig),
                this.httpClientFactory)
        ];

        var toolSelectionPrompt = new FileSystemPromptFactory(
            "websearch_example_system",
            this.promptParser,
            this.promptProviderConfig)
            .Create();

        var respondingPrompt = new FileSystemPromptFactory(
            "websearch_example_responding",
            this.promptParser,
            this.promptProviderConfig)
            .Create();

        // TODO: BeginLoop().WithSequence().AddAgent().AddAgent().EndLoop();
        return new WhileCell<ConversationThread>()
        {
            WhileTrue = new CellSequence<ConversationThread>(
                sequence: new Cell<ConversationThread>[]
                {
                    new AgentCell(this.userConsoleAgent),
                    new AgentCell(
                        new ToolAgent(
                            new AgentName("WebSearchAgent"),
                            Role.Assistant,
                            toolSelectionPrompt,
                            respondingPrompt,
                            this.customAgentBuilderFactory,
                            tools)),
                }.ToImmutableArray()),
        };
    }

    public async Task RunAsync()
    {
        var logger = this.GetLogger();
        logger.LogInformation("Starting up...");
        logger.LogInformation("Running web search example");

        if (this.loggingConfig.LogRequestsToLlm)
        {
            logger.LogWarning("Verbosity mode enabled, will log requests and responses to/from llm");
        }

        Cell<ConversationThread> definition = this.CreateDefinition();

        await this.runner.RunAsync(
            definition,
            new ConversationThread());
    }
}
