using System.Collections.Immutable;
using System.Text.Json;
using AgentFlow.LlmClient;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public class ExecuteToolCell : Cell<ConversationThread>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

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

        ToolSelectionOutput toolSelection = JsonSerializer.Deserialize<ToolSelectionOutput>(lastMessage.Content, JsonSerializerOptions)
            ?? throw new InvalidOperationException("Could not parse the last message as a ToolSelectionOutput");

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

            toolOutput = await tool.GetOutput(toolInput);
        }

        var next = input.WithAddedMessage(
            new Message(
                new AgentName("ToolExecution"),
                Role.ToolOutput,
                toolOutput,
                new MessageVisibility(ShownToUser: false, ShownToModel: true)));

        return next;
    }

    private static string ExtractToolInvocationInput(ToolSelectionOutput toolSelectionOutput)
    {
        // e.x.: search_web('cute puppies')
        string invocation = toolSelectionOutput.Invocation.Trim();

        string extractedInputs;
        {
            string stage1 = invocation.Split('(')[1];
            string stage2 = stage1.TrimStart('\'');
            string stage3 = stage2.TrimEnd(')').TrimEnd('\'');

            extractedInputs = stage3;
        }

        return extractedInputs;
    }

    private ITool SelectMatchingTool(Message toolMessage)
    {
        // TODO
        return this.tools.First();
    }

    private record ToolSelectionOutput(
        string LastUserMessageIntent,
        string FunctionName,
        string Invocation);
}
