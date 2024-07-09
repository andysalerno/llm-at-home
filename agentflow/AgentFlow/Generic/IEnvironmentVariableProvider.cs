namespace AgentFlow.Generic;

public interface IEnvironmentVariableProvider
{
    string? GetVariableValue(string variableName);
}

public class EnvironmentVariableProvider : IEnvironmentVariableProvider
{
    public string? GetVariableValue(string variableName)
        => Environment.GetEnvironmentVariable(variableName);
}
