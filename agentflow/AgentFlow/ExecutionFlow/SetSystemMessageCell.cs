using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public class SetSystemMessageCell : Cell<ConversationThread>
{
    private readonly ILogger<SetSystemMessageCell> logger;
    private readonly AgentName agentName;
    private readonly Prompt systemMessageContent;

    public SetSystemMessageCell(AgentName agentName, Prompt systemMessageContent)
    {
        this.logger = this.GetLogger();
        this.agentName = agentName;
        this.systemMessageContent = systemMessageContent;
    }

    public override Cell<ConversationThread>? GetNext(ConversationThread input)
    {
        return null;
    }

    public override Task<ConversationThread> RunAsync(ConversationThread input)
    {
        if (input.Messages.Any(m => m.Role == Role.System))
        {
            this.logger.LogWarning("Workspace message context already contained a system message, which is unexpected.");
        }

        return Task.FromResult(input.WithSystemMessage(new Message(this.agentName, Role.System, this.systemMessageContent.Render())));
    }
}
