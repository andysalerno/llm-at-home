using AgentFlow.WorkSpace;

namespace AgentFlow.Tools;

public interface ITool
{
    string Name { get; }

    string Definition { get; }

    Task<string> GetOutputAsync(ConversationThread conversation, string input);
}
