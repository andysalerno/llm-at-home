using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public sealed record SetSystemMessageCell : Cell<ConversationThread>
{
    private readonly ILogger<SetSystemMessageCell> logger;
    private readonly AgentName agentName;
    private readonly RenderedPrompt systemMessageContent;
    private readonly InstructionStrategy instructionStrategy;

    public SetSystemMessageCell(
        AgentName agentName,
        RenderedPrompt systemMessageContent,
        InstructionStrategy instructionStrategy = InstructionStrategy.TopLevelSystemMessage)
    {
        this.logger = this.GetLogger();
        this.agentName = agentName;
        this.systemMessageContent = systemMessageContent;
        this.instructionStrategy = instructionStrategy;
    }

    public override Task<ConversationThread> RunAsync(ConversationThread input)
    {
        if (input.Messages.FirstOrDefault(m => m.Role == Role.System) is Message existingSystemMessage
            && !string.Equals(existingSystemMessage.Content, this.systemMessageContent.Text, StringComparison.Ordinal))
        {
            this.logger
                .LogWarning("ConversationThread already contained a different system message, which will be replaced.");
        }

        ConversationThread withoutSystem = input.WithMatchingMessages(m => m.Role != Role.System);

        ConversationThread updated = this.instructionStrategy switch
        {
            InstructionStrategy.TopLevelSystemMessage =>
                withoutSystem.WithFirstMessageSystemMessage(
                    new Message(this.agentName, Role.System, this.systemMessageContent.Text)),
            InstructionStrategy.InlineUserMessage =>
                withoutSystem.WithAddedMessage(
                    new Message(this.agentName, Role.User, this.systemMessageContent.Text)),
            InstructionStrategy.InlineSystemMessage =>
                withoutSystem.WithAddedMessage(
                    new Message(this.agentName, Role.System, this.systemMessageContent.Text)),
            InstructionStrategy.AppendedToUserMessage =>
                AddSystemMessageToLastUserMessage(withoutSystem, this.systemMessageContent.Text),
            _ => throw new NotImplementedException(),
        };

        return Task.FromResult(updated);
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

        return new ConversationThread()
            .WithAddedMessages(messagesWithoutLast.Append(updatedUserMessage));
    }
}
