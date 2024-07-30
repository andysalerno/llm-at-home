using AgentFlow.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentFlow.Tests;

public class PromptTests
{
    public PromptTests()
    {
        Logging.TryRegisterLoggerFactory(NullLoggerFactory.Instance);
    }

    private IPromptRenderer Renderer { get; } = new PromptRenderer(new PromptRendererConfig());

    [Fact]
    public void Prompt_WithFrontMatter_CanBeParsed()
    {
        const string PromptText = """
---
name: some prompt
---
this is some prompt
""";

        var parser = new PromptParser();

        var prompt = parser.Parse(PromptText);

        Assert.Equal(expected: "this is some prompt", actual: this.Renderer.Render(prompt).Text);
    }

    [Fact]
    public void PromptFrontMatter_WithFrontMatter_CanBeParsed()
    {
        const string PromptText = """
---
name: some prompt
---
this is some prompt
""";

        var parser = new PromptParser();

        var prompt = parser.Parse(PromptText);

        Assert.Equal(expected: "some prompt", actual: prompt.PromptFrontMatter.Name);
    }

    [Fact]
    public void PromptFrontMatter_WithUnknownFrontMatterProperty_CanBeParsed()
    {
        const string PromptText = """
---
name: some prompt
fakeprop: some fake property
---
this is some prompt
""";

        var parser = new PromptParser();

        var prompt = parser.Parse(PromptText);

        Assert.Equal(expected: "some prompt", actual: prompt.PromptFrontMatter.Name);
    }

    [Fact]
    public void Prompt_MissingFrontMatter_CanBeParsed()
    {
        const string PromptText = """
this is some prompt
""";

        var parser = new PromptParser();

        Prompt prompt = parser.Parse(PromptText);

        Assert.Equal(expected: "this is some prompt", actual: prompt.TemplateText);
    }

    [Fact]
    public void Prompt_WithVariables_WhenRendered_ExpectsVariablesAppear()
    {
        const string PromptText = """
this is some prompt with a variable called foo: {{foo}}
""";

        var parser = new PromptParser();

        Prompt prompt = parser.Parse(PromptText);

        // prompt.AddVariable(name: "foo", value: "bar");

        RenderedPrompt rendered = this.Renderer.Render(prompt);

        Assert.Equal(expected: "this is some prompt with a variable called foo: bar", actual: rendered.Text);
    }
}
