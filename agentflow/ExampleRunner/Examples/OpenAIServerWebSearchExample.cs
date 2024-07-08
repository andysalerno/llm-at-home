using System.Collections.Immutable;
using System.Globalization;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Examples.Agents;
using AgentFlow.Examples.Tools;
using AgentFlow.Generic;
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
    private readonly IPromptRenderer promptRenderer;
    private readonly IFactoryProvider<Prompt, PromptName> promptFactoryProvider;

    public OpenAIServerWebSearchExample(
        IEmbeddingsClient embeddingsClient,
        IScraperClient scraperClient,
        IHttpClientFactory httpClientFactory,
        ICellRunner<ConversationThread> runner,
        IPromptRenderer promptRenderer,
        IFactoryProvider<Prompt, PromptName> promptFactoryProvider,
        CustomAgentBuilderFactory agentBuilderFactory)
    {
        this.embeddingsClient = embeddingsClient;
        this.scraperClient = scraperClient;
        this.httpClientFactory = httpClientFactory;
        this.runner = runner;
        this.promptRenderer = promptRenderer;
        this.promptFactoryProvider = promptFactoryProvider;
        this.agentBuilderFactory = agentBuilderFactory;
    }

    public async Task RunAsync()
    {
        var program = this.CreateProgram();

        var passthruProgram = new AgentCell(
            this.agentBuilderFactory
                .CreateBuilder()
                .WithName(new AgentName("ResponseAgent"))
                .WithRole(Role.Assistant)

                // .WithPrompt(string.Empty)
                .Build());

        await new OpenAIServer().ServeAsync(program, passthruProgram, this.runner);
    }

    public Cell<ConversationThread> CreateProgram()
    {
        ImmutableArray<ITool> tools = [

            // new LightSwitchTool(this.httpClientFactory),
            new WebSearchTool(
                this.agentBuilderFactory,
                this.runner,
                this.embeddingsClient,
                this.scraperClient,
                this.promptRenderer,
                this.promptFactoryProvider.GetFactory(ExamplePrompts.RewriteQuerySystem),
                this.httpClientFactory),
        ];

        var toolSelectionPrompt = this.promptFactoryProvider
            .GetFactory(ExamplePrompts.WebsearchExampleSystem)
            .Create()
            .AddVariable("CUR_DATE", DateTime.Today.ToString("MMM dd, yyyy", DateTimeFormatInfo.InvariantInfo));

        var respondingPrompt = this.promptFactoryProvider
            .GetFactory(ExamplePrompts.WebsearchExampleResponding)
            .Create();

        respondingPrompt.AddVariable(
            "CUR_DATE",
            DateTime.Today.ToString("MMM dd, yyyy", DateTimeFormatInfo.InvariantInfo));

        return new AgentCell(
            new ToolAgent(
                new AgentName("WebSearchAgent"),
                Role.Assistant,
                toolSelectionPrompt,
                respondingPrompt,
                this.agentBuilderFactory,
                tools));
    }
}
