using AgentFlow.WorkSpace;

namespace AgentFlow.Agents.ExecutionFlow;

public class SetTemplateValueCell : Cell<ConversationThread>
{
    private readonly string templateKey;
    private readonly string templateValue;

    public SetTemplateValueCell(string templateKey, string templateValue)
    {
        this.templateKey = templateKey;
        this.templateValue = templateValue;
    }

    public override Task<ConversationThread> RunAsync(ConversationThread input)
    {
        return Task.FromResult(input.WithTemplateValue(this.templateKey, this.templateValue));
    }
}
