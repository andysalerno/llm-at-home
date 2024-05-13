using System.Collections.Immutable;
using System.Net.Mime;
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

        await this.BenchOneAsync();

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

        while (true)
        {
            var split = scenarioText.Split("{{ END }}", count: 2).Where(s => !string.IsNullOrWhiteSpace(s)).ToImmutableArray();
            if (split.Length == 0)
            {
                break;
            }
            else if (split.Length > 2)
            {
                throw new InvalidOperationException($"Expected two splits at most, saw {split.Length}");
            }

            string nextMessageRaw = split[0];

            var regex = new Regex(@"{{ START_(?<start>[^}]*) }}\s*(?<content>.*)", RegexOptions.Singleline);

            this.logger.LogInformation("Parsing text: {Text}", nextMessageRaw);

            Match match = regex.Match(nextMessageRaw);

            if (!match.Success)
            {
                throw new InvalidOperationException("Could not parse the scenario text.");
            }

            string startTag = match.Groups["start"].Value.Trim();
            string content = match.Groups["content"].Value.Trim();

            this.logger.LogInformation("saw tag: {Tag}", startTag);
            this.logger.LogInformation("saw content: {Content}", content);

            messages.Add(new Message(new AgentName(startTag), Role.ExpectFromName(startTag), Content: content));

            if (split.Length == 1)
            {
                break;
            }

            scenarioText = split[1];
        }

        this.logger.LogInformation("Saw messages: {Thread}", messages);

        return new ConversationThread();
    }

    private async Task BenchOneAsync()
    {
        this.logger.LogInformation("Running BenchOne");
        string scenario1Content = await GetScenarioTextAsync("scenario_1");
        var conversationThread = ParseScenario(scenario1Content);

        IAgent agent = this.agentFactory
            .CreateBuilder()
            .WithRole(Role.Assistant)
            .WithName(new AgentName("BenchOneAgent"))
            .Build();

        ConversationThread result = await this.runner.RunAsync(new AgentCell(agent), conversationThread);

        this.logger.LogInformation("Result: {Result}", result);
    }
}
