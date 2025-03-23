using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.CodeExecution;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AgentFlow.Tests.ExecutionFlow;

public class ExecuteCodeCellTests
{
    [Fact]
    public async Task TestAsync()
    {
        var codeExecutor = new Mock<ICodeExecutor>(MockBehavior.Strict);
        codeExecutor
            .Setup(c => c.ExecuteCodeAsync(It.IsAny<string>()))
            .ReturnsAsync("unimportant");

        var executeCell = new ExecuteCodeCell(codeExecutor.Object, new NullLogger<ExecuteCodeCell>());

        const string MessageWithCode = "Hi there! Try running this: ```python\n# test test\n```";

        var input = new ConversationThread(new ConversationId("test"))
            .WithAddedMessage(new Message(new AgentName("SomeAgent"), Role.Assistant, MessageWithCode));

        ConversationThread output = await executeCell.RunAsync(input);

        Assert.Equal(2, output.Messages.Count);
    }
}
