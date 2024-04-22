namespace AgentFlow.Tools;

public interface ITool
{
    string Name { get; }

    string Definition { get; }

    Task<string> GetOutput(string input);
}
