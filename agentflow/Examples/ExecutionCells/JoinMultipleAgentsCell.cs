using System.Collections.Immutable;
using System.Text.Json;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples.ExecutionCells;

/// <summary>
/// Gets a response from several different agents, and then collects them
/// into a majority decision.
/// </summary>
internal sealed class JoinMultipleAgentsCell : Cell<ConversationThread>
{
    private readonly ImmutableArray<AgentCell> agents;
    private readonly AgentCell collector;
    private readonly Func<string, string> collectorMessageBuilder;
    private readonly ILogger<JoinMultipleAgentsCell> logger;

    public JoinMultipleAgentsCell(
        ImmutableArray<AgentCell> agents,
        AgentCell collector,
        Func<string, string> collectorMessageBuilder)
    {
        this.agents = agents;
        this.collector = collector;
        this.collectorMessageBuilder = collectorMessageBuilder;
        this.logger = this.GetLogger();
    }

    public override Cell<ConversationThread>? GetNext(ConversationThread input)
    {
        return null;
    }

    public override async Task<ConversationThread> RunAsync(ConversationThread input)
    {
        var decisions = new List<Message>();

        // Get response from each agent:
        foreach (AgentCell agent in this.agents)
        {
            var outputConversation = await agent.RunAsync(input);

            Message last = outputConversation.Messages.Last();

            decisions.Add(last);
        }

        string agentsOutputJson;
        {
            var formatted = decisions.ConvertAll(d => new { Name = d.AgentName.Value, Response = d.Content });
            agentsOutputJson = JsonSerializer.Serialize(formatted);
            this.logger.LogInformation("Formatted agent outputs to: {Outputs}", agentsOutputJson);
        }

        var collectorMessage = this.collector.CreateMessage(this.collectorMessageBuilder(agentsOutputJson));

        ConversationThread threadWithAllResponses = input.WithAddedMessage(collectorMessage);

        if (this.collector.AgentRole != Role.Assistant)
        {
            throw new InvalidOperationException("The 'collector' agent must have role 'Assistant', but it did not");
        }

        // Then, ask the 'collector' agent to tally up the votes and provide the final response:
        return await this.collector.RunAsync(threadWithAllResponses);
    }
}
