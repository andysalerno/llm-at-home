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

internal sealed class OpenAIServerWebSearchExample : IRunnableExample
{
    private readonly CustomAgentBuilderFactory agentBuilderFactory;
    private readonly IEmbeddingsClient embeddingsClient;
    private readonly IScraperClient scraperClient;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ICellRunner<ConversationThread> runner;
    private readonly IFileSystemPromptProviderConfig fileSystemPromptProviderConfig;

    public OpenAIServerWebSearchExample(
        IEmbeddingsClient embeddingsClient,
        IScraperClient scraperClient,
        IHttpClientFactory httpClientFactory,
        ICellRunner<ConversationThread> runner,
        CustomAgentBuilderFactory agentBuilderFactory,
        IFileSystemPromptProviderConfig fileSystemPromptProviderConfig)
    {
        this.embeddingsClient = embeddingsClient;
        this.scraperClient = scraperClient;
        this.httpClientFactory = httpClientFactory;
        this.runner = runner;
        this.agentBuilderFactory = agentBuilderFactory;
        this.fileSystemPromptProviderConfig = fileSystemPromptProviderConfig;
    }

    public async Task RunAsync()
    {
        ImmutableArray<ITool> tools = [

            // new LightSwitchTool(this.httpClientFactory),
            new WebSearchTool(
                this.agentBuilderFactory,
                this.runner,
                this.embeddingsClient,
                this.scraperClient,
                new FileSystemPromptFactory("rewrite_query_system", this.fileSystemPromptProviderConfig),
                this.httpClientFactory),
            ];

        var toolSelectionPrompt = new FileSystemPromptFactory(
            "websearch_example_system",
            this.fileSystemPromptProviderConfig)
            .Create();

        var respondingPrompt = new FileSystemPromptFactory(
            "websearch_example_responding",
            this.fileSystemPromptProviderConfig)
            .Create();

        var program = new AgentCell(
            new ToolAgent(
                new AgentName("WebSearchAgent"),
                Role.Assistant,
                toolSelectionPrompt,
                respondingPrompt,
                this.agentBuilderFactory,
                tools));

        var passthruProgram = new AgentCell(
            this.agentBuilderFactory
                .CreateBuilder()
                .WithName(new AgentName("ResponseAgent"))
                .WithRole(Role.Assistant)
                .WithInstructions(string.Empty)
                .Build());

        await new OpenAIServer().ServeAsync(program, passthruProgram, this.runner);
    }
}
