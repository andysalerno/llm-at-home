using AgentFlow.Agents;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;

namespace AgentFlow.Prompts;

/// <summary>
/// Defines the strategies that may be used for presenting the system message to the LLM.
/// </summary>
public enum InstructionStrategy
{
    /// <summary>
    /// The system message will appear as a message with role 'System', as the very first message in a conversation.
    /// </summary>
    TopLevelSystemMessage,

    /// <summary>
    /// The system message will appear as a new message with role 'User' in the conversation.
    /// </summary>
    InlineUserMessage,

    /// <summary>
    /// The system message will appear as a new message with role System in the conversation (as the latest message, NOT as the first message).
    /// </summary>
    InlineSystemMessage,

    /// <summary>
    /// The system message will appear appended to the last user message.
    /// </summary>
    AppendedToUserMessage,

    /// <summary>
    /// The system message will appear as the second-to-last message, with the last message being the last user message.
    /// TODO: fill me in. will probably work best for tool selection.
    /// </summary>
    PrecedingLastUserMessage,
}

public static class InstructionStrategyParser
{
    /// <summary>
    /// Attempts to parse a string into an InstructionStrategy value.
    /// </summary>
    /// <param name="input">The string to parse.</param>
    /// <param name="strategy">When this method returns, contains the InstructionStrategy value equivalent to the string contained in input, if the conversion succeeded, or the default value if the conversion failed.</param>
    /// <returns>true if input was converted successfully; otherwise, false.</returns>
    public static bool TryParse(string input, out InstructionStrategy strategy)
    {
        return Enum.TryParse(input, true, out strategy);
    }
}

public static class InstructionStrategyApplicator
{
    public static ConversationThread ApplyStrategy(
        InstructionStrategy strategy,

        // TODO: remove this, why need agent name here?
        AgentName agentName,
        ConversationThread input)
    {
        var lastMessage = input.Messages.Last();

        if (lastMessage.Role != Role.System)
        {
            throw new InvalidOperationException("Last message in the conversation thread was not a system message.");
        }

        ConversationThread withoutSystem = input.WithMatchingMessages(m => m.Role != Role.System);

        return strategy switch
        {
            InstructionStrategy.TopLevelSystemMessage =>
                withoutSystem.WithFirstMessageSystemMessage(
                    new Message(agentName, Role.System, lastMessage.Content)),
            InstructionStrategy.InlineUserMessage =>
                withoutSystem.WithAddedMessage(
                    new Message(agentName, Role.User, lastMessage.Content)),
            InstructionStrategy.InlineSystemMessage =>
                withoutSystem.WithAddedMessage(
                    new Message(agentName, Role.System, lastMessage.Content)),
            InstructionStrategy.AppendedToUserMessage =>
                AddSystemMessageToLastUserMessage(withoutSystem, lastMessage.Content),
            _ => throw new NotImplementedException(),
        };
    }

    private static ConversationThread AddSystemMessageToLastUserMessage(
        ConversationThread input,
        string systemMessageContent)
    {
        var lastUserMessage = input.Messages.Last();

        if (lastUserMessage.Role != Role.User)
        {
            throw new InvalidOperationException("Last message in the conversation thread was not a user message.");
        }

        string updatedContent = $"{lastUserMessage.Content}\n\n<instructions>{systemMessageContent}</instructions>";

        Message updatedUserMessage = lastUserMessage with { Content = updatedContent };

        var messagesWithoutLast = input.Messages.Take(input.Messages.Count - 1);

        return new ConversationThread(input.ConversationId)
            .WithAddedMessages(messagesWithoutLast.Append(updatedUserMessage));
    }

}