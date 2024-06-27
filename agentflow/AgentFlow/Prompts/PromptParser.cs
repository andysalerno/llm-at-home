namespace AgentFlow.Prompts;

public interface IPromptParser
{
    Prompt Parse(string input);
}

public sealed class PromptParser : IPromptParser
{
    public Prompt Parse(string input)
    {
        return new Prompt(input, new Prompt.FrontMatter(string.Empty));
    }
}