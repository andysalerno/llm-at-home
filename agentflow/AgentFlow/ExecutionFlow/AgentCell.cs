using AgentFlow.Agents.Extensions;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public sealed record AgentCell : Cell<ConversationThread>
{
    public IAgent Agent { get; }

    private readonly ILogger<AgentCell> logger;

    public AgentCell(IAgent agent)
    {
        this.Agent = agent;
        this.logger = this.GetLogger();
    }

    public AgentName AgentName => this.Agent.Name;

    public Role AgentRole => this.Agent.Role;

    public override async Task<ConversationThread> RunAsync(ConversationThread input)
    {
        var sequence = await this.Agent.GetNextConversationStateAsync();

        return await sequence.RunAsync(input);
    }

    public Message CreateMessage(string text)
    {
        return this.Agent.CreateMessage(text);
    }
}
