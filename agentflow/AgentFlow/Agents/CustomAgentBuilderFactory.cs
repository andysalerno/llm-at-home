namespace AgentFlow.Agents;

public class CustomAgentBuilderFactory
{
    private readonly ILlmCompletionsClient completionsClient;

    public CustomAgentBuilderFactory(ILlmCompletionsClient completionsClient)
    {
        this.completionsClient = completionsClient;
    }

    public CustomAgent.Builder CreateBuilder()
    {
        return new CustomAgent.Builder(this.completionsClient);
    }
}
