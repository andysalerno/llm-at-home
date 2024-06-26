using AgentFlow;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExecutionFlow.Tests;

public class PromptTests
{
    public PromptTests()
    {
        Logging.TryRegisterLoggerFactory(NullLoggerFactory.Instance);
    }

    [Fact]
    public void Sample()
    {

        Assert.True(true);
    }
}
