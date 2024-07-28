using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;

namespace AgentFlow.Agents;

public interface IAgent
{
    AgentName Name { get; }

    Role Role { get; }

    Task<Cell<ConversationThread>> GetNextConversationStateAsync();
}

public sealed record AgentName(string Value);
