namespace AgentFlow.Prompts;

public interface IPromptRenderer
{
    RenderedPrompt Render(Prompt prompt, IReadOnlyDictionary<string, string>? keyValuePairs = null);

    // RenderedTranscript RenderTranscript(Prompt prompt, ConversationThread conversationThread);
}
