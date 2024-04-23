using AgentFlow.Agents;

namespace AgentFlow.LlmClient;

public record Message(
    AgentName AgentName,
    Role Role,
    string Content,
    MessageVisibility? Visibility = null);

public record MessageVisibility(bool ShownToUser = true, bool ShownToModel = true);
