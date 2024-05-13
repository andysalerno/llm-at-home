using System.Text.RegularExpressions;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.Util;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples;

internal sealed class AgentBenchExample : IRunnableExample
{
    private static readonly string[] Separator = ["{{ END }}"];

    private static readonly Lazy<string> ScenarioDirectory =
        new Lazy<string>(() =>
        {
            DirectoryInfo assemblyDir = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).NonNullOrThrow();
            return $"{assemblyDir.FullName}/Examples/AgentBench/Scenarios";
        });

    private readonly CustomAgentBuilderFactory agentFactory;
    private readonly ICellRunner<ConversationThread> runner;
    private readonly ILogger<AgentBenchExample> logger;

    public AgentBenchExample(CustomAgentBuilderFactory agentFactory, ICellRunner<ConversationThread> runner, ILogger<AgentBenchExample> logger)
    {
        this.agentFactory = agentFactory;
        this.runner = runner;
        this.logger = logger;
    }

    public async Task RunAsync()
    {
        this.logger.LogInformation("Starting AgentBench...");

        await this.RunScenarioAsync("logic_1");

        this.logger.LogInformation("AgentBench complete.");
    }

    private static async Task<string> GetScenarioTextAsync(string scenarioName)
    {
        string scenarioDir = ScenarioDirectory.Value;

        return await File.ReadAllTextAsync($"{scenarioDir}/{scenarioName}.scenario");
    }

    private ConversationThread ParseScenario(string scenarioText)
    {
        var messages = new List<Message>();

        var parts = scenarioText.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

        var messageRegex = new Regex(@"{{ START_(?<start>[^}]*) }}\s*(?<content>.*)", RegexOptions.Singleline);

        foreach (var part in parts)
        {
            string trimmedPart = part.Trim();
            if (string.IsNullOrEmpty(trimmedPart))
            {
                continue; // Skip empty segments.
            }

            Match match = messageRegex.Match(trimmedPart);

            if (!match.Success)
            {
                throw new InvalidOperationException("Failed to parse the scenario text: " + trimmedPart);
            }

            string startTag = match.Groups["start"].Value.Trim();
            string content = match.Groups["content"].Value.Trim();

            this.logger.LogInformation("Parsed message with tag: {Tag}, content: {Content}", startTag, content);

            messages.Add(new Message(new AgentName(startTag), Role.ExpectFromName(startTag), content));
        }

        this.logger.LogInformation("Total parsed messages: {Count}", messages.Count);

        return ConversationThread.CreateBuilder().AddMessages(messages).Build();
    }

    private async Task<ConversationThread> GetScenarioConversationAsync(string scenarioName)
    {
        string scenarioText = await GetScenarioTextAsync(scenarioName);
        return this.ParseScenario(scenarioText);
    }

    private CustomAgent GetSimpleTestAgent()
    {
        return this.agentFactory
            .CreateBuilder()
            .WithRole(Role.Assistant)
            .WithName(new AgentName("BenchAgent"))
            .Build();
    }

    private async Task RunScenarioAsync(string scenarioName)
    {
        using var scope = this.logger.BeginScope(scenarioName);

        var conversationThread = await this.GetScenarioConversationAsync(scenarioName);
        ConversationThread result = await this.runner.RunAsync(new AgentCell(this.GetSimpleTestAgent()), conversationThread);

        await this.WriteResultAsync("modelName", scenarioName, result.Messages.Last().Content);
    }

    private async Task WriteResultAsync(string modelName, string scenarioName, string output)
    {
        DirectoryInfo parent = Directory.GetParent(ScenarioDirectory.Value).NonNullOrThrow();

        string resultDir = $"{parent.FullName}/Results/{modelName}/";

        Directory.CreateDirectory(resultDir);

        string resultPath = resultDir + scenarioName;

        this.logger.LogInformation("writing result to: {Path}", resultPath);

        await File.WriteAllTextAsync(resultPath, output);

        this.logger.LogInformation("wrote result to: {Path}", resultPath);
    }
}
