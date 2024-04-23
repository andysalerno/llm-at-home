using AgentFlow.CodeExecution;
using AgentFlow.WorkSpace;

namespace AgentFlow.Agents.ExecutionFlow;

/// <summary>
/// An ICondition that returns true iff the last message
/// contains a markdown-style codeblock.
/// </summary>
public class ContainsCodeBlockCondition : ICondition<ConversationThread>
{
    public bool Evaluate(ConversationThread input)
    {
        var lastMessage = input.Messages.LastOrDefault();

        if (lastMessage is null)
        {
            return false;
        }

        if (ExtractCodeBlocks(lastMessage.Content).Any())
        {
            return true;
        }

        return false;
    }

    private static List<(string Language, string Code)> ExtractCodeBlocks(string input)
    {
        return MarkdownCodeblockExtractor.ExtractCodeBlocks(input);
    }
}
