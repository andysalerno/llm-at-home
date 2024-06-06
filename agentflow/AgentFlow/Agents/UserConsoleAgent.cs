using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.CodeExecution;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents;

public class UserConsoleAgent : IAgent
{
    private readonly ILogger<UserConsoleAgent> logger;

    public UserConsoleAgent(ICodeExecutor codeExecutor)
    {
        this.CodeExecutor = codeExecutor;
        this.logger = this.GetLogger();
    }

    public Role Role { get; } = Role.User;

    public AgentName Name { get; } = new AgentName("UserConsoleAgent");

    public string ModelDescription { get; } = "blah";

    public ICodeExecutor? CodeExecutor { get; }

    public bool IsCodeProvider { get; }

    public Task<Cell<ConversationThread>> ContinueLastAssistantMessageAsync(ConversationThread conversationThread)
        => throw new NotImplementedException("User console agent does not support continuing assistant messages.");

    public Task<string> ExecuteCodeAsync(string code)
    {
        return this.CodeExecutor?.ExecuteCodeAsync(code)
            ?? throw new InvalidOperationException("CodeExecutor not configured on the UserConsoleAgent, cannot execute code.");
    }

    public Task<AgentResponse> GetNextResponseAsync(ConversationThread thread)
    {
        throw new NotImplementedException("Use GetNextThreadStateAsync instead");
    }

    public Task<Cell<ConversationThread>> GetNextThreadStateAsync()
    {
        Cell<ConversationThread> result = new GetUserInputCell(this.Name, this.Role);

        return Task.FromResult(result);
    }
}
