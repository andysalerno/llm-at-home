namespace AgentFlow.Prompts;

public record Variable(string Name, string Value);

public record Prompt
{
    private readonly List<Variable> variables = new();

    public IReadOnlyList<Variable> Variables => this.variables;

    public string TemplateText { get; }

    public Prompt(string templateText)
    {
        this.TemplateText = templateText;
    }

    public Prompt WithVariable(string name, string value)
    {
        this.variables.Add(new Variable(name, value));
        return this;
    }

    public string Render()
    {
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

        if (result.Contains("{{", StringComparison.Ordinal))
        {
            // TODO: this should be a warning, not an exception
            // throw new InvalidOperationException($"Prompt was rendered, but still contained unreplaced template artifacts. Saw: {result}");
        }

        return result;
    }
}
