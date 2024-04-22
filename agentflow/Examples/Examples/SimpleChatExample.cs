using System.Collections.Immutable;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;

namespace AgentFlow.Examples;

internal static class SimpleChatExample
{
    public static Cell<ConversationThread> CreateDefinition(
        IAgent userConsoleAgent,
        CustomAgentBuilderFactory agentBuilderFactory)
    {
        var assistant = agentBuilderFactory
            .CreateBuilder()
            .WithName(new AgentName("Assistant"))
            .WithRole(Role.Assistant)
            .WithInstructions("You are a friendly and helpful assistant. Help as much as you can.")
            .Build();

        var loopForever = new WhileCell<ConversationThread>()
        {
            WhileTrue = new CellSequence<ConversationThread>(
                sequence: new Cell<ConversationThread>[]
                {
                    new AgentCell(userConsoleAgent),
                    new AgentCell(assistant),
                }.ToImmutableArray()),
        };

        return loopForever;
    }
}
