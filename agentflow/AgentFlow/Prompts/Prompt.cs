using System.Collections.Immutable;
using AgentFlow.LlmClient;

namespace AgentFlow.Prompts;

/// <summary>
/// The name of a prompt. Used for retrieving a prompt by name.
/// </summary>
public sealed record PromptName(string Value);

/// <summary>
/// A variable key/value representation in a <see cref="Prompt"/>.
/// </summary>
public sealed record Variable(string Name, string Value);

/// <summary>
/// The result of using a <see cref="IPromptRenderer"/> to render a <see cref="Prompt"/>.
/// </summary>
public sealed record RenderedPrompt(string Text);

public sealed record RenderedTranscript(ImmutableArray<Message> Transcript);

/// <summary>
/// A prompt that can be sent to an LLM for completion, either as the full
/// context, as a single chat message, or as multiple chat messages.
///
/// <para>A <see cref="IPromptParser"/> takes a raw <see cref="string"/> and parses
/// it into a <see cref="Prompt"/>.</para>
///
/// <para>A <see cref="IPromptRenderer"/> takes a <see cref="Prompt"/> and renders
/// it back to a string, replacing any variables provided.</para>
/// </summary>
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

    /// <summary>
    /// The configuration values present in the prompt.
    /// </summary>
    public sealed record FrontMatter(string Name, string? Description = null);
}
