using System.Text.RegularExpressions;

namespace AgentFlow.CodeExecution;

public static class MarkdownCodeblockExtractor
{
    public static List<(string Language, string Code)> ExtractCodeBlocks(string input)
    {
        // Regular expression to match any markdown code block with its language
        const string pattern = @"```(\w+)\s*([\s\S]*?)\s*```";

        var matches = Regex.Matches(input, pattern);
        var codeBlocks = new List<(string Language, string Code)>();

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                // Add the language identifier and code block to the list
                codeBlocks.Add((match.Groups[1].Value, match.Groups[2].Value));
            }
        }

        return codeBlocks;
    }
}
