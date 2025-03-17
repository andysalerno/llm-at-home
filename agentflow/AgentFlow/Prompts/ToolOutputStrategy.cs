using AgentFlow.Agents;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;

namespace AgentFlow.Prompts;

/// <summary>
/// Defines the strategies that may be used for presenting the system message to the LLM.
/// </summary>
public enum ToolOutputStrategy
{
    /// <summary>
    /// The tool output will have a dedicated message..
    /// </summary>
    InlineToolOutputMessage,

    /// <summary>
    /// The tool output message will appear appended to the last user message.
    /// </summary>
    AppendedToUserMessage,
}

public static class ToolOutputStrategyParser
{
    /// <summary>
    /// Attempts to parse a string into a ToolOutputStrategy value.
    /// </summary>
    /// <param name="input">The string to parse.</param>
    /// <param name="strategy">When this method returns, contains the InstructionStrategy value equivalent to the string contained in input, if the conversion succeeded, or the default value if the conversion failed.</param>
    /// <returns>true if input was converted successfully; otherwise, false.</returns>
    public static bool TryParse(string input, out ToolOutputStrategy strategy)
    {
        return Enum.TryParse(input, true, out strategy);
    }
}

public static class ToolStrategyApplicator
{
    public static ConversationThread ApplyStrategy(
        ToolOutputStrategy strategy,
        ConversationThread input)
    {
        return strategy switch
        {
            ToolOutputStrategy.InlineToolOutputMessage =>
                input, // no change needed
            ToolOutputStrategy.AppendedToUserMessage =>
                AppendToLastUserMessage(input),
            _ => throw new NotImplementedException(),
        };
    }

    private static ConversationThread AppendToLastUserMessage(
        ConversationThread input)
    {
        var lastToolMessage = input.Messages.LastOrDefault(m => m.Role == Role.ToolOutput);

        if (lastToolMessage is null)
        {
            // Nothing to transform if there is no tool output message.
            return input;
        }

        var withoutToolInvocation = input.WithMatchingMessages(m => m.Role != Role.ToolInvocation);

        var conversationWithoutToolOutputs = withoutToolInvocation.WithMatchingMessages(m => m.Role != Role.ToolOutput);

        var lastUserMessage = conversationWithoutToolOutputs.Messages.Last();

        if (lastUserMessage.Role != Role.User)
        {
            throw new InvalidOperationException(
                $"Last message in the conversation thread was not a user message, was: {lastUserMessage.Role}");
        }

        string updatedContent =
            $"{lastUserMessage.Content}\n\n<tool_output>\n{lastToolMessage.Content}\n</tool_output>";

        Message updatedUserMessage = lastUserMessage with { Content = updatedContent };

        var messagesWithoutLastUser = conversationWithoutToolOutputs
            .WithMatchingMessages(m => !object.Equals(m, lastUserMessage));

        return messagesWithoutLastUser.WithAddedMessage(updatedUserMessage);
    }
}