using System.Collections.Immutable;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.WorkSpace;

namespace AgentFlow.Examples;

internal sealed class SimpleChatExample : IRunnableExample
{
    private readonly IAgent userConsoleAgent;
    private readonly CustomAgentBuilderFactory agentBuilderFactory;
    private readonly ICellRunner<ConversationThread> runner;

    public SimpleChatExample(
        IAgent userConsoleAgent,
        CustomAgentBuilderFactory agentBuilderFactory,
        ICellRunner<ConversationThread> runner)
    {
        this.userConsoleAgent = userConsoleAgent;
        this.agentBuilderFactory = agentBuilderFactory;
        this.runner = runner;
    }

    public async Task RunAsync()
    {
        Cell<ConversationThread> definition = this.CreateDefinition();

        await this.runner.RunAsync(
            definition,
            new ConversationThread());
    }

    private Cell<ConversationThread> CreateDefinition()
    {
        var assistant = this.agentBuilderFactory
            .CreateBuilder()
            .WithName(new AgentName("Assistant"))
            .WithRole(Role.Assistant)
            .WithPrompt(new Prompt("You are a friendly and helpful assistant. Help as much as you can."))
            .Build();

        return new WhileCell<ConversationThread>()
        {
            WhileTrue = new CellSequence<ConversationThread>(
                sequence: new Cell<ConversationThread>[]
                {
                    new AgentCell(this.userConsoleAgent),
                    new AgentCell(assistant),
                }.ToImmutableArray()),
        };
    }
}
