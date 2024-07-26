using AgentFlow.WorkSpace;

namespace AgentFlow.Prompts;

public interface IPromptRenderer
{
    /// <summary>
    /// To be deprecated by <see cref="RenderTranscript"/>.
    /// </summary>
    RenderedPrompt Render(Prompt prompt);

    RenderedTranscript RenderTranscript(Prompt prompt, ConversationThread conversationThread);
}
