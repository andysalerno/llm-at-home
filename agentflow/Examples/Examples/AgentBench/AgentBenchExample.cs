using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Agents.Extensions;
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

    private async Task BenchOneAsync()
    {
        Message CreateUserMessage(string content)
        {
            return new Message(new AgentName("UserAgent"), Role.User, content);
        }

        var conversationThread = ConversationThread
            .CreateBuilder()
            .AddMessage(CreateUserMessage("how many prime numbers are there?"))
            .Build();

        IAgent agent = this.agentFactory
            .CreateBuilder()
            .WithRole(Role.Assistant)
            .WithName(new AgentName("BenchOneAgent"))
            .Build();

        ConversationThread result = await this.runner.RunAsync(new AgentCell(agent), conversationThread);

        this.logger.LogInformation("Result: {Result}", result);
    }
}
