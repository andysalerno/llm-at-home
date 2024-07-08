using AgentFlow.Prompts;

namespace AgentFlow.Agents;

public class CustomAgentBuilderFactory
{
    private readonly ILlmCompletionsClient completionsClient;
    private readonly IPromptRenderer promptRenderer;

    public CustomAgentBuilderFactory(ILlmCompletionsClient completionsClient, IPromptRenderer promptRenderer)
    {
        this.completionsClient = completionsClient;
        this.promptRenderer = promptRenderer;
    }

    public CustomAgent.Builder CreateBuilder()
    {
        return new CustomAgent.Builder(this.completionsClient, this.promptRenderer);
    }
}
