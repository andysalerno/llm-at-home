namespace AgentFlow.Prompts;

public sealed record Variable(string Name, string Value);

public sealed record RenderedPrompt(string Text);

public sealed record PromptText(string Text);

public sealed record Prompt
{
    private readonly List<Variable> variables = new();

    public Prompt(string templateText, FrontMatter frontMatter)
    {
        this.TemplateText = templateText;
        this.PromptFrontMatter = frontMatter;
    }

    public Prompt(string templateText)
        : this(templateText, new FrontMatter(string.Empty))
    {
    }

    public IReadOnlyList<Variable> Variables => this.variables;

    public string TemplateText { get; }

    public FrontMatter PromptFrontMatter { get; }

    public Prompt AddVariable(string name, string value)
    {
        this.variables.Add(new Variable(name, value));
        return this;
    }

    public RenderedPrompt Render()
    {
        // todo: remove or obsolete this method
        return new PromptRenderer().Render(this);
    }

    public sealed record FrontMatter(string Name, string? Description = null);
}
