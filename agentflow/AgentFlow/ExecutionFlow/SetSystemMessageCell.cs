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

    public SetSystemMessageCell(
        AgentName agentName,
        RenderedPrompt systemMessageContent)
    {
        this.logger = this.GetLogger();
        this.agentName = agentName;
        this.systemMessageContent = systemMessageContent;
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

        ConversationThread updated = withoutSystem.WithAddedMessage(
                    new Message(this.agentName, Role.System, this.systemMessageContent.Text));

        return Task.FromResult(updated);
    }
}
