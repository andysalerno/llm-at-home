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

public static class ToolStrategyApplicator
{
    public static ConversationThread ApplyStrategy(
        ToolOutputStrategy strategy,
        ConversationThread input)
    {
        var conversationWithoutToolOutputs = input.WithMatchingMessages(m => m.Role != Role.ToolOutput);

        var lastUserMessage = input.Messages.Last(m => m.Role == Role.User);

        var lastToolMessage = input.Messages.Last(m => m.Role == Role.ToolOutput);

        string updatedContent =
            $"{lastUserMessage.Content}\n\n<tool_output>\n{lastToolMessage.Content}\n</tool_output>";

        Message updatedUserMessage = lastUserMessage with { Content = updatedContent };

        var messagesWithoutLastUser = input.WithMatchingMessages(m => m != lastUserMessage);

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

        var conversationWithoutToolOutputs = input.WithMatchingMessages(m => m.Role != Role.ToolOutput);

        var lastUserMessage = input.Messages.Last();

        if (lastUserMessage.Role != Role.User)
        {
            throw new InvalidOperationException("Last message in the conversation thread was not a user message.");
        }

        string updatedContent =
            $"{lastUserMessage.Content}\n\n<tool_output>\n{lastToolMessage.Content}\n</tool_output>";

        Message updatedUserMessage = lastUserMessage with { Content = updatedContent };

        var messagesWithoutLastUser = input.WithMatchingMessages(m => !object.Equals(m, lastUserMessage));

        return messagesWithoutLastUser.WithAddedMessage(updatedUserMessage);
    }
}