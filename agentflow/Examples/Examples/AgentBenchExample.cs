using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples;

internal class AgentBenchExample : IRunnableExample
{
    private readonly ILogger<AgentBenchExample> logger;

    public AgentBenchExample(ILogger<AgentBenchExample> logger)
    {
        this.logger = logger;
    }

    public async Task RunAsync()
    {
        this.logger.LogInformation("Starting agentbench...");
    }
}
