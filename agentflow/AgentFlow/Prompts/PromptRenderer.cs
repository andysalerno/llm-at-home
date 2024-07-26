using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Prompts;

public interface IPromptRendererConfig
{
}

public sealed record PromptRendererConfig() : IPromptRendererConfig;

public sealed class PromptRenderer : IPromptRenderer
{
    private readonly IPromptRendererConfig config;

    public PromptRenderer(IPromptRendererConfig config)
    {
        this.config = config;
    }

    public RenderedPrompt Render(Prompt prompt)
    {
        var logger = this.GetLogger();

        string result = prompt.TemplateText;

        foreach (var variable in prompt.Variables)
        {
            string templatedVariableText = "{{" + variable.Name + "}}";
            if (!result.Contains(templatedVariableText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected template to include variable {variable.Name}, but was not found");
            }

            result = result.Replace(templatedVariableText, variable.Value, StringComparison.Ordinal);
        }

        logger.LogInformation("Replaced {Count} variables in prompt", prompt.Variables.Count);

        if (result.Contains("{{", StringComparison.Ordinal))
        {
            logger.LogWarning("Prompt was rendered, but (most likely) still contained unreplaced template artifacts.");
            logger.LogDebug("Saw: {Result}", result);
        }

        return new RenderedPrompt(result);
    }

    public RenderedTranscript RenderTranscript(Prompt prompt, ConversationThread conversationThread)
    {
        // There is a special well-known variable name for the conversation history
        throw new NotImplementedException();
    }
}
