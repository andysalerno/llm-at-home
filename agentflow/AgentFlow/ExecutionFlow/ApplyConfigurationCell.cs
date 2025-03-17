using System.Text.Json;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public sealed record ApplyConfigurationCell : Cell<ConversationThread>
{
    private readonly IReadOnlyDictionary<string, string> configurationKeyValues;

    public ApplyConfigurationCell(IReadOnlyDictionary<string, string> configurationKeyValues)
    {
        this.configurationKeyValues = configurationKeyValues;
    }

    public override Task<ConversationThread> RunAsync(ConversationThread input)
    {
        var logger = this.GetLogger();

        logger.LogInformation(
            "Applying configuration to conversation thread: {}",
            JsonSerializer.Serialize(this.configurationKeyValues));

        return Task.FromResult(input.WithConfigurations(this.configurationKeyValues));
    }
}