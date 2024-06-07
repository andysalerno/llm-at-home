using Microsoft.Extensions.Logging;

namespace AgentFlow.Prompts;

public sealed record Variable(string Name, string Value);

public sealed record Prompt
{
    private readonly List<Variable> variables = new();

    public Prompt(string templateText)
    {
        this.TemplateText = templateText;
    }

    public IReadOnlyList<Variable> Variables => this.variables;

    public string TemplateText { get; }

    public Prompt AddVariable(string name, string value)
    {
        this.variables.Add(new Variable(name, value));
        return this;
    }

    public string Render()
    {
        var logger = this.GetLogger();

        string result = this.TemplateText;

        foreach (var variable in this.variables)
        {
            string tepmlatedVariableText = $"{{{variable.Name}}}";
            if (!result.Contains(tepmlatedVariableText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected template to include variable {variable.Name}, but was not found");
            }

            result = result.Replace(tepmlatedVariableText, variable.Value, StringComparison.Ordinal);
        }

        logger.LogInformation("Replaced {Count} variables in prompt", this.variables.Count);

        if (result.Contains("{{", StringComparison.Ordinal))
        {
            logger.LogWarning("Prompt was rendered, but still contained unreplaced template artifacts.");
            logger.LogDebug("Saw: {Result}", result);
        }

        return result;
    }
}
