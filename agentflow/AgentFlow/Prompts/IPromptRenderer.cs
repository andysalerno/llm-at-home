namespace AgentFlow.Prompts;

public interface IPromptRenderer
{
    RenderedPrompt Render(Prompt prompt);
}
