using System.Text.RegularExpressions;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples;

internal sealed class AgentBenchExample : IRunnableExample
{
    private static readonly string[] Separator = new[] { "{{ END }}" };

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

        await this.Bench2Async();

        this.logger.LogInformation("AgentBench complete.");
    }

    private static async Task<string> GetScenarioTextAsync(string scenarioName)
    {
        string currentDir = System.Reflection.Assembly.GetExecutingAssembly().Location;
        currentDir = Directory.GetParent(currentDir)?.FullName ?? throw new InvalidOperationException("Parent dir not found");
        return await File.ReadAllTextAsync($"{currentDir}/Examples/AgentBench/Scenarios/{scenarioName}.scenario");
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

    private IAgent GetSimpleTestAgent()
    {
        return this.agentFactory
            .CreateBuilder()
            .WithRole(Role.Assistant)
            .WithName(new AgentName("BenchAgent"))
            .Build();
    }

    private async Task Bench1Async()
    {
        this.logger.LogInformation("Running Bench2");

        var conversationThread = await this.GetScenarioConversationAsync("scenario_1");
        ConversationThread result = await this.runner.RunAsync(new AgentCell(this.GetSimpleTestAgent()), conversationThread);

        this.logger.LogInformation("Result: {Result}", result);
    }

    private async Task Bench2Async()
    {
        this.logger.LogInformation("Running Bench2");

        var conversationThread = await this.GetScenarioConversationAsync("scenario_2");
        ConversationThread result = await this.runner.RunAsync(new AgentCell(this.GetSimpleTestAgent()), conversationThread);

        this.logger.LogInformation("Result: {Result}", result);
    }
}
