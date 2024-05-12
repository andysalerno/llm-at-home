using System.Collections.Immutable;
using System.Text;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Examples.ExecutionCells;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;

namespace AgentFlow.Examples;

internal class MagiExample : IRunnableExample
{
    private readonly IAgent userConsoleAgent;
    private readonly CustomAgentBuilderFactory agentBuilderFactory;
    private readonly ICellRunner<ConversationThread> runner;

    public MagiExample(IAgent userConsoleAgent, CustomAgentBuilderFactory agentBuilderFactory, ICellRunner<ConversationThread> runner)
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

    private static string BuildInstructionsForRole(string roleName)
    {
        return $"You are a {roleName}. Your repsonses are always from the point of view of a {roleName}. Consider what the user says, and provide your decision, keeping in mind your role as a {roleName}.";
    }

    private Cell<ConversationThread> CreateDefinition()
    {
        IAgent bot = this.agentBuilderFactory
            .CreateBuilder()
            .WithRole(Role.Assistant)
            .WithName(new AgentName("MagiCollector"))
            .WithInstructions(
                "You are an assistant who queries multiple other AI assistants for their responses. " +
                "You see all their responses, and then you combine them into one response for the user. " +
                "**Always** provide a response that corresponds with the **majority decision** of the agents you query.")
            .Build();

        // In the famous science-fiction anime Neon Genesis: Evangelion, there is an AI system named 'Magi'
        // which is composed of 3 agents who make decisions by majority decision.
        // Spoiler: the 3 agents are revealed to be 3 different aspects of their creator, Dr. Naoko Agaki.
        // Those aspects are: Dr. Agaki as a mother, Dr. Agaki as a woman, and Dr. Agaki as a scientist.
        IAgent magiMother = this.agentBuilderFactory
            .CreateBuilder()
            .WithRole(Role.Assistant)
            .WithName(new AgentName("MagiMother"))
            .WithInstructions(BuildInstructionsForRole("mother"))
            .Build();

        IAgent magiWoman = this.agentBuilderFactory
            .CreateBuilder()
            .WithName(new AgentName("MagiWoman"))
            .WithRole(Role.Assistant)
            .WithInstructions(BuildInstructionsForRole("woman"))
            .Build();

        IAgent magiScientist = this.agentBuilderFactory
            .CreateBuilder()
            .WithName(new AgentName("MagiScientist"))
            .WithRole(Role.Assistant)
            .WithInstructions(BuildInstructionsForRole("scientist"))
            .Build();

        var loopForever = new WhileCell<ConversationThread>()
        {
            WhileTrue = new CellSequence<ConversationThread>(
                sequence: new Cell<ConversationThread>[]
                {
                        // 1. user provides the question
                        new AgentCell(this.userConsoleAgent),

                        // 2. Magi agents respond with their majority decision:
                        new JoinMultipleAgentsCell(
                            agents: new[]
                            {
                                new AgentCell(magiMother),
                                new AgentCell(magiWoman),
                                new AgentCell(magiScientist),
                            }.ToImmutableArray(),
                            collector: new AgentCell(bot),
                            collectorMessageBuilder: (agentsOutput) =>
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine("I queried multiple agents to respond. Here is what they said:");
                                sb.AppendLine(agentsOutput);
                                sb.AppendLine("\nBased on their majority decision, I can provide you with this final repsonse:");
                                return sb.ToString();
                            }),
                }.ToImmutableArray()),
        };

        return loopForever;
    }
}
