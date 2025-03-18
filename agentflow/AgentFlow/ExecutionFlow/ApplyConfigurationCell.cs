using System.Text.Json;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public sealed record ApplyConfigurationCell : Cell<ConversationThread>
{
    public ApplyConfigurationCell(IReadOnlyDictionary<string, string> configurationKeyValues)
    {
        this.ConfigurationKeyValues = configurationKeyValues;
    }

    public IReadOnlyDictionary<string, string> ConfigurationKeyValues { get; }

    public override Task<ConversationThread> RunAsync(ConversationThread input)
    {
        var logger = this.GetLogger();

        logger.LogInformation(
            "Applying configuration to conversation thread: {}",
            JsonSerializer.Serialize(this.ConfigurationKeyValues));

        return Task.FromResult(input.WithConfigurations(this.ConfigurationKeyValues));
    }
}