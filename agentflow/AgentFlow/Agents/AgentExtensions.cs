using AgentFlow.LlmClient;

namespace AgentFlow.Agents.Extensions;

public static class AgentExtensions
{
    public static Message CreateMessage(this IAgent agent, string text)
    {
        return new Message(agent.Name, agent.Role, text);
    }
}
