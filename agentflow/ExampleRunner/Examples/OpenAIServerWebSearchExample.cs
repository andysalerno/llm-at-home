using System.Collections.Immutable;
using System.Globalization;
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
    private readonly IPromptParser promptParser;
    private readonly IFileSystemPromptProviderConfig fileSystemPromptProviderConfig;

    // constructor should take the prompt factory, not both the parser
    // and the config for the purposes of constructing the factory.
    public OpenAIServerWebSearchExample(
        IEmbeddingsClient embeddingsClient,
        IScraperClient scraperClient,
        IHttpClientFactory httpClientFactory,
        ICellRunner<ConversationThread> runner,
        IPromptParser promptParser,
        CustomAgentBuilderFactory agentBuilderFactory,
        IFileSystemPromptProviderConfig fileSystemPromptProviderConfig)
    {
        this.embeddingsClient = embeddingsClient;
        this.scraperClient = scraperClient;
        this.httpClientFactory = httpClientFactory;
        this.runner = runner;
        this.promptParser = promptParser;
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
                new FileSystemPromptFactory(ExamplePrompts.RewriteQuerySystem, this.promptParser, this.fileSystemPromptProviderConfig),
                this.httpClientFactory),
        ];

        var toolSelectionPrompt = new FileSystemPromptFactory(
            ExamplePrompts.WebsearchExampleSystem,
            this.promptParser,
            this.fileSystemPromptProviderConfig)
            .Create().AddVariable("CUR_DATE", DateTime.Today.ToString("MMM dd, yyyy", DateTimeFormatInfo.InvariantInfo));

        var respondingPrompt = new FileSystemPromptFactory(
            ExamplePrompts.WebsearchExampleResponding,
            this.promptParser,
            this.fileSystemPromptProviderConfig)
            .Create();

        respondingPrompt.AddVariable("CUR_DATE", DateTime.Today.ToString("MMM dd, yyyy", DateTimeFormatInfo.InvariantInfo));

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
