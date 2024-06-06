using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;

namespace AgentFlow.Agents;

public interface IAgent
{
    AgentName Name { get; }

    Role Role { get; }

    Task<Cell<ConversationThread>> GetNextThreadStateAsync();
}

public record AgentName(string Value);

public record AgentResponse(string Text, AgentName AgentName, Role Role, bool IsTerminate = false);
