namespace AgentFlow.CodeExecution;

public interface ICodeExecutor
{
    Task<string> ExecuteCodeAsync(string code);
}
