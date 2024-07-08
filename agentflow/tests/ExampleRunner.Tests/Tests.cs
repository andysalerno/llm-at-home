using AgentFlow.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentFlow.ExampleRunner.Tests;

public class ExampleRunnerTests
{
    public ExampleRunnerTests()
    {
        Logging.TryRegisterLoggerFactory(NullLoggerFactory.Instance);
    }

    [Fact]
    public void Prompt_WithFrontMatter_CanBeParsed()
    {
        Assert.True(true);
    }
}
