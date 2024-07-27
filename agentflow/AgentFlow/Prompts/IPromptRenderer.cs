using AgentFlow.WorkSpace;

namespace AgentFlow.Prompts;

public interface IPromptRenderer
{
    RenderedPrompt Render(Prompt prompt);

    // RenderedTranscript RenderTranscript(Prompt prompt, ConversationThread conversationThread);
}
