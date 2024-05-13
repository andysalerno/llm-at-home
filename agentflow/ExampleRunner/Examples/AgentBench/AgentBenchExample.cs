using System.Collections.Immutable;
using System.Net.Mime;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples;

internal sealed class AgentBenchExample : IRunnableExample
{
    private readonly CustomAgentBuilderFactory agentFactory;
    private readonly ICellRunner<ConversationThread> runner;
    private readonly ILogger<AgentBenchExample> logger;

    public AgentBenchExample(CustomAgentBuilderFactory agentFactory, ICellRunner<ConversationThread> runner, ILogger<AgentBenchExample> logger)
    {
        this.agentFactory = agentFactory;
        this.runner = runner;
        this.logger = logger;
    }

    public async Task RunAsync()
    {
        this.logger.LogInformation("Starting AgentBench...");

        await this.BenchOneAsync();

        this.logger.LogInformation("AgentBench complete.");
    }

    private ConversationThread ParseScenario(string scenarioText)
    {
        var messages = new List<Message>();

        while (true)
        {
            var split = scenarioText.Split("{{ END }}", count: 2).Where(s => !string.IsNullOrWhiteSpace(s)).ToImmutableArray();
            if (split.Length == 0)
            {
                break;
            }
            else if (split.Length > 2)
            {
                throw new InvalidOperationException($"Expected two splits at most, saw {split.Length}");
            }

            if (!split[0].StartsWith("{{ START_"))
            {
                throw new InvalidOperationException("Expected split to start with message prefix");
            }

            string messageContent = split[0].Substring("{{ START_".Length);

            messages.Add(new Message(new AgentName("blah"), Role.User, Content: messageContent));

            scenarioText = split[1];
        }

        this.logger.LogInformation("Saw messages: {Thread}", messages);

        return new ConversationThread();
    }

    private async Task BenchOneAsync()
    {
        var conversationThread = ParseScenario("hi");

        IAgent agent = this.agentFactory
            .CreateBuilder()
            .WithRole(Role.Assistant)
            .WithName(new AgentName("BenchOneAgent"))
            .Build();

        ConversationThread result = await this.runner.RunAsync(new AgentCell(agent), conversationThread);

        this.logger.LogInformation("Result: {Result}", result);
    }
}
