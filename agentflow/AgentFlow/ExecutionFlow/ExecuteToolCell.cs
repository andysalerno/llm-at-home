using System.Collections.Immutable;
using System.Text.Json;
using AgentFlow.LlmClient;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public sealed record ExecuteToolCell : Cell<ConversationThread>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly ImmutableArray<ITool> tools;
    private readonly ILogger<ExecuteToolCell> logger;

    public ExecuteToolCell(ImmutableArray<ITool> tools)
    {
        this.tools = tools;
        this.logger = this.GetLogger();
    }

    public override async Task<ConversationThread> RunAsync(ConversationThread input)
    {
        var lastMessage = input.Messages.Last();

        if (lastMessage.Role != Role.ToolInvocation)
        {
            throw new InvalidOperationException(
                "Expected the last message to be a tool message from the assistant");
        }

        // trim until the first occurrence of {:
        int indexOfFirstCurlyBracket = lastMessage.Content.IndexOf('{');
        string content = lastMessage.Content[indexOfFirstCurlyBracket..];
        int indexOfLastCurlyBracket = content.LastIndexOf('}');
        content = content[..(indexOfLastCurlyBracket + 1)];

        ToolSelectionOutput toolSelection = JsonSerializer.Deserialize<ToolSelectionOutput>(
            content,
            JsonSerializerOptions)
            ?? throw new InvalidOperationException(
                $"Could not parse the last message as a ToolSelectionOutput: {content}");

        this.logger.LogInformation("Saw tool selection: {Selection}", toolSelection);

        string toolOutput;
        if (toolSelection.Invocation == "direct_response()" || toolSelection.Invocation == "direct_response")
        {
            toolOutput = "nothing; no tool was executed. respond directly.";
        }
        else
        {
            var tool = this.SelectMatchingTool(lastMessage);

            string toolInput = ExtractToolInvocationInput(toolSelection);

            this.GetLogger().LogInformation("Detected tool input string: {Input}", toolInput);

            toolOutput = await tool.GetOutputAsync(input, toolInput);
        }

        return input.WithAddedMessage(
            new Message(
                new AgentName("ToolExecution"),
                Role.ToolOutput,
                toolOutput,
                new MessageVisibility(ShownToUser: false, ShownToModel: true)));
    }

    private static string ExtractToolInvocationInput(ToolSelectionOutput toolSelectionOutput)
    {
        // e.x.: search_web('cute puppies')
        string invocation = toolSelectionOutput.Invocation.Trim();

        string extractedInputs;
        {
            string stage1 = invocation.Split('(')[1];
            string stage2 = stage1.TrimStart('\'');
#pragma warning disable RCS1124 // Inline local variable
            string stage3 = stage2.TrimEnd(')').TrimEnd('\'');
#pragma warning restore RCS1124 // Inline local variable

            extractedInputs = stage3;
        }

        return extractedInputs;
    }

    private ITool SelectMatchingTool(Message toolMessage)
    {
        // TODO
        if (toolMessage.Content.Contains("search_web", StringComparison.Ordinal))
        {
            return this.tools.FirstOrDefault(t => t.Name == "search_web")
                ?? throw new InvalidOperationException("tool not found");
        }

        if (toolMessage.Content.Contains("search_news", StringComparison.Ordinal))
        {
            return this.tools.FirstOrDefault(t => t.Name == "search_news")
                ?? throw new InvalidOperationException("tool not found");
        }
        else if (toolMessage.Content.Contains("lights", StringComparison.Ordinal))
        {
            return this.tools.FirstOrDefault(t => t.Name == "turn_lights_on_off")
                ?? throw new InvalidOperationException("tool not found");
        }

        return this.tools.First();
    }

    internal sealed record ToolSelectionOutput(
        string LastUserMessageIntent,
        string FunctionName,
        string Invocation);
}
