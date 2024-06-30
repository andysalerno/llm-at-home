using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentFlow.Prompts;

public interface IPromptParser
{
    Prompt Parse(string input);
}

public sealed class PromptParser : IPromptParser
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    public Prompt Parse(string input)
    {
        input = input.Replace("\r\n", "\n", StringComparison.Ordinal);

        const string FrontMatterDelim = "---\n";

        var splits = input.Split(
            FrontMatterDelim,
            3,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new Prompt(splits[1], ParseFrontMatter(splits[0]));
    }

    private static Prompt.FrontMatter ParseFrontMatter(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        // string --> yaml
        var yamlObj = deserializer.Deserialize(yaml);

        // yaml --> json
        string json = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .JsonCompatible()
            .Build()
            .Serialize(yamlObj);

        // json --> FrontMatter
        return JsonSerializer.Deserialize<Prompt.FrontMatter>(json, JsonOptions)
            ?? throw new InvalidOperationException("Could not deserialize frontmatter");
    }
}
