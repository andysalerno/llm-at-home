using AgentFlow.CodeExecution;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public sealed record ExecuteCodeCell : Cell<ConversationThread>
{
    private readonly ICodeExecutor codeExecutor;
    private readonly ILogger<ExecuteCodeCell> logger;

    public ExecuteCodeCell(ICodeExecutor codeExecutor, ILogger<ExecuteCodeCell> logger)
    {
        this.codeExecutor = codeExecutor;
        this.logger = logger;
    }

    public override async Task<ConversationThread> RunAsync(ConversationThread input)
    {
        string messageText = input.Messages.Last().Content;

        IEnumerable<(string Language, string Code)> blocks = MarkdownCodeblockExtractor.ExtractCodeBlocks(messageText);

        this.logger.LogInformation("Detected {CodeBlocksCount} code blocks", blocks.Count());

        (string language, string code) = blocks.First();

        string codeOutput = await this.codeExecutor.ExecuteCodeAsync(code);

        string response = $"<internal message>Here is the verbatim output from executing your code:\n{codeOutput}";

        return input.WithAddedMessage(new Message(new AgentName("CodeExecutor"), Role.User, response));
    }
}
