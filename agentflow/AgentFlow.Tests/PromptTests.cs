using AgentFlow.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentFlow.Tests.ExecutionFlow;

public class PromptTests
{
    public PromptTests()
    {
        Logging.TryRegisterLoggerFactory(NullLoggerFactory.Instance);
    }

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

        Assert.Equal(expected: "this is some prompt", actual: prompt.Render().Text);
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
}
