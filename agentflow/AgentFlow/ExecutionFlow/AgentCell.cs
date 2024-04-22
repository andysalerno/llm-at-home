using AgentFlow.Agents.Extensions;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public class AgentCell : Cell<ConversationThread>
{
    private readonly IAgent agent;
    private readonly Cell<ConversationThread>? next;
    private readonly ILogger<AgentCell> logger;

    public AgentCell(IAgent agent)
        : this(agent, next: null)
    {
    }

    public AgentCell(IAgent agent, Cell<ConversationThread>? next)
    {
        this.agent = agent;
        this.next = next;
        this.logger = this.GetLogger();
    }

    public AgentName AgentName => this.agent.Name;

    public Role AgentRole => this.agent.Role;

    public override Cell<ConversationThread>? GetNext(ConversationThread input)
    {
        return this.next;
    }

    public override async Task<ConversationThread> RunAsync(ConversationThread input)
    {
        var sequence = await this.agent.GetNextThreadStateAsync(input);

        var result = await sequence.RunAsync(input);

        this.logger.LogInformation("Latest message is: {LatestMessage}", result.Messages.LastOrDefault()?.Content);

        return result;
    }

    public Message CreateMessage(string text)
    {
        return this.agent.CreateMessage(text);
    }
}
