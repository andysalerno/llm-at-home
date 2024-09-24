using System.Collections.Immutable;
using System.Text.Json;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public class GetAssistantResponseCell : Cell<ConversationThread>
{
    private readonly ILogger<GetAssistantResponseCell> logger;
    private readonly AgentName agentName;
    private readonly Role agentRole;
    private readonly JsonElement? responseSchema;
    private readonly string? toolChoice;
    private readonly ILlmCompletionsClient completionsClient;

    public GetAssistantResponseCell(AgentName agentName, Role agentRole, ILlmCompletionsClient completionsClient)
        : this(agentName, agentRole, null, null, completionsClient)
    {
    }

    public GetAssistantResponseCell(
        AgentName agentName,
        Role agentRole,
        JsonElement? responseSchema,
        string? toolChoice,
        ILlmCompletionsClient completionsClient)
    {
        this.agentName = agentName;
        this.agentRole = agentRole;
        this.responseSchema = responseSchema;
        this.toolChoice = toolChoice;
        this.completionsClient = completionsClient;
        this.logger = this.GetLogger();
    }

    public override async Task<ConversationThread> RunAsync(ConversationThread input)
    {
        var messages = input.Messages.ToImmutableArray();

        if (messages.LastOrDefault() is Message lastMessage && lastMessage.Role == Role.Assistant)
        {
            this.logger
                .LogWarning("Last message was from assistant already. Some LLMs may not work well in this scenario.");
        }

        ConversationThread templateFilled = input
            .WithTemplateAppliedToSystem()
            .WithMessagesVisibleToAssistant();

        var response = await this.completionsClient.GetChatCompletionsAsync(
            new ChatCompletionsRequest(
                templateFilled.Messages,
                JsonSchema: this.responseSchema,
                ToolChoice: this.toolChoice));

        // Don't return the template filled version - we only ever want the template filled for sending to the LLM
        return input.WithAddedMessage(new Message(this.agentName, this.agentRole, response.Text));
    }
}
