using System.Collections.Immutable;
using System.Text.Json.Nodes;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public sealed record GetAssistantResponseCell : Cell<ConversationThread>
{
    private readonly ILogger<GetAssistantResponseCell> logger;
    private readonly AgentName agentName;
    private readonly Role agentRole;
    private readonly JsonObject? responseSchema;
    private readonly string? toolChoice;
    private readonly ILlmCompletionsClient completionsClient;
    private readonly InstructionStrategy strategy;
    private readonly ToolOutputStrategy toolOutputStrategy;

    public GetAssistantResponseCell(
        AgentName agentName,
        Role agentRole,
        InstructionStrategy instructionStrategy,
        ToolOutputStrategy toolOutputStrategy,
        ILlmCompletionsClient completionsClient)
        : this(agentName, agentRole, null, null, instructionStrategy, toolOutputStrategy, completionsClient)
    {
    }

    public GetAssistantResponseCell(
        AgentName agentName,
        Role agentRole,
        JsonObject? responseSchema,
        string? toolChoice,
        InstructionStrategy strategy,
        ToolOutputStrategy toolOutputStrategy,
        ILlmCompletionsClient completionsClient)
    {
        this.agentName = agentName;
        this.agentRole = agentRole;
        this.responseSchema = responseSchema;
        this.toolChoice = toolChoice;
        this.completionsClient = completionsClient;
        this.strategy = strategy;
        this.toolOutputStrategy = toolOutputStrategy;
        this.logger = this.GetLogger();
    }

    public override async Task<ConversationThread> RunAsync(ConversationThread input)
    {
        // this should be where we apply the instruction strategy
        var messages = input.Messages.ToImmutableArray();

        if (messages.LastOrDefault() is Message lastMessage && lastMessage.Role == Role.Assistant)
        {
            this.logger
                .LogWarning("Last message was from assistant already. Some LLMs may not work well in this scenario.");
        }

        ConversationThread templateFilled = input
            .WithTemplateAppliedToSystem()
            .WithMessagesVisibleToAssistant();

        // At this point, update the conversation with the strategy:
        ConversationThread withInstructionStrategyApplied = InstructionStrategyApplicator.ApplyStrategy(
            this.strategy,
            this.agentName,
            templateFilled);

        // ToolOutputStrategy.AppendedToUserMessage
        ConversationThread withToolOutputStrategyApplied = ToolStrategyApplicator.ApplyStrategy(
            this.toolOutputStrategy,
            withInstructionStrategyApplied);

        string? model = null;

        if (input.ConfigurationKeyValues.TryGetValue("model", out var modelValue))
        {
            model = modelValue;
            this.logger.LogInformation("Using overriden model from context: {Model}", model);
        }

        var response = await this.completionsClient.GetChatCompletionsAsync(
            new ChatCompletionsRequest(
                withToolOutputStrategyApplied.Messages,
                Model: model,
                JsonSchema: this.responseSchema,
                ToolChoice: this.toolChoice));

        // Don't return the template filled version - we only ever want the template filled for sending to the LLM
        return input.WithAddedMessage(new Message(this.agentName, this.agentRole, response.Text));
    }
}
