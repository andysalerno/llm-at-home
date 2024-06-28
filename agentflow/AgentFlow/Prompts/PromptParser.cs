namespace AgentFlow.Prompts;

public interface IPromptParser
{
    Prompt Parse(string input);
}

public sealed class PromptParser : IPromptParser
{
    public Prompt Parse(string input)
    {
        input = input.Replace("\r\n", "\n", StringComparison.Ordinal);

        const string FrontMatterDelim = "---\n";

        var splits = input.Split(
            FrontMatterDelim,
            3,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new Prompt(splits[1], new Prompt.FrontMatter(splits[0]));
    }
}
