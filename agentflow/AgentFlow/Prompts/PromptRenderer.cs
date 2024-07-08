using Microsoft.Extensions.Logging;

namespace AgentFlow.Prompts;

public sealed class PromptRenderer : IPromptRenderer
{
    public RenderedPrompt Render(Prompt prompt)
    {
        var logger = this.GetLogger();

        string result = prompt.TemplateText;

        foreach (var variable in prompt.Variables)
        {
            string tepmlatedVariableText = $"{{{variable.Name}}}";
            if (!result.Contains(tepmlatedVariableText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected template to include variable {variable.Name}, but was not found");
            }

            result = result.Replace(tepmlatedVariableText, variable.Value, StringComparison.Ordinal);
        }

        logger.LogInformation("Replaced {Count} variables in prompt", prompt.Variables.Count);

        if (result.Contains("{{", StringComparison.Ordinal))
        {
            logger.LogWarning("Prompt was rendered, but (most likely) still contained unreplaced template artifacts.");
            logger.LogDebug("Saw: {Result}", result);
        }

        return new RenderedPrompt(result);
    }
}
