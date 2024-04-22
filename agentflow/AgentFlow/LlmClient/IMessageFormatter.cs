using System.Collections.Immutable;

namespace AgentFlow.LlmClient;

public interface IMessageFormatter
{
    string FormatMessages(ImmutableArray<Message> messages, bool addGenerationPrompt = true);
}
