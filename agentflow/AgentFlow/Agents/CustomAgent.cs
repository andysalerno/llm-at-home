using System.Collections.Immutable;
using System.Text.Json;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents;

/// <summary>
/// Defines the strategies that may be used for presenting the system message to the LLM.
/// </summary>
public enum InstructionStrategy
{
    /// <summary>
    /// The system message will appear as normal, as a message with role 'System', as the very first message in a conversation.
    /// </summary>
    TopLevelSystemMessage,

    /// <summary>
    /// The system message will appear as a new message with role 'User' in the conversation.
    /// </summary>
    InlineUserMessage,

    /// <summary>
    /// The system message will appear as a new message with role System in the conversation (as the latest message, NOT as the first message).
    /// </summary>
    InlineSystemMessage,
}

public sealed class CustomAgent : IAgent
{
    private readonly MessageVisibility messageVisibility;
    private readonly IPromptRenderer promptRenderer;
    private readonly ILlmCompletionsClient completionsClient;
    private readonly JsonElement? responseSchema;
    private readonly ILogger<CustomAgent> logger;

    private CustomAgent(
        Role role,
        Prompt? prompt,
        InstructionStrategy instructionStrategy,
        AgentName name,
        MessageVisibility messageVisibility,
        IPromptRenderer promptRenderer,
        ILlmCompletionsClient completionsClient,
        JsonElement? responseSchema = null)
    {
        this.Role = role;
        this.Prompt = prompt;
        this.InstructionStrategy = instructionStrategy;
        this.completionsClient = completionsClient;
        this.responseSchema = responseSchema;
        this.Name = name;
        this.messageVisibility = messageVisibility;
        this.promptRenderer = promptRenderer;
        this.logger = this.GetLogger();
    }

    public Role Role { get; }

    public AgentName Name { get; }

    public string ModelDescription { get; } = "<unset>";

    public bool IsCodeProvider { get; }

    public Prompt? Prompt { get; }

    public InstructionStrategy InstructionStrategy { get; }

    public Task<Cell<ConversationThread>> GetNextThreadStateAsync()
    {
        var cells = ImmutableArray.CreateBuilder<Cell<ConversationThread>>();
        {
            if (this.Prompt is Prompt p)
            {
                RenderedPrompt rendered = this.promptRenderer.Render(p);
                cells.Add(new SetSystemMessageCell(this.Name, rendered));
            }
            else
            {
                this.logger.LogInformation("No system prompt set; this may be intentional");
            }

            cells.Add(new GetAssistantResponseCell(this.Name, this.Role, this.responseSchema, this.completionsClient));
        }

        Cell<ConversationThread> sequence = new CellSequence<ConversationThread>(
            sequence: cells.DrainToImmutable(),
            next: null);

        return Task.FromResult(sequence);
    }

    public class Builder
    {
        private readonly IPromptRenderer promptRenderer;
        private Role? role;
        private Prompt? prompt;
        private AgentName? name;
        private InstructionStrategy instructionsStrategy = InstructionStrategy.TopLevelSystemMessage;
        private ILlmCompletionsClient completionsClient;
        private JsonElement? responseSchema;
        private MessageVisibility visibility;

        public Builder(ILlmCompletionsClient completionsClient, IPromptRenderer promptRenderer)
        {
            this.completionsClient = completionsClient;
            this.promptRenderer = promptRenderer;
            this.visibility = new MessageVisibility(ShownToUser: true, ShownToModel: true);
            this.responseSchema = null;
        }

        public Builder WithMessageVisibility(MessageVisibility visibility)
        {
            this.visibility = visibility;

            return this;
        }

        public Builder WithCompletionsClient(ILlmCompletionsClient completionsClient)
        {
            this.completionsClient = completionsClient;

            return this;
        }

        public Builder WithRole(Role role)
        {
            this.role = role;

            return this;
        }

        public Builder WithPrompt(Prompt prompt)
        {
            this.prompt = prompt;

            return this;
        }

        public Builder WithJsonResponseSchema(JsonElement responseSchema)
        {
            this.responseSchema = responseSchema;

            return this;
        }

        public Builder WithInstructionsFromPrompt(Prompt prompt)
        {
            this.prompt = prompt;

            return this;
        }

        public Builder WithInstructionsStrategy(InstructionStrategy strategy)
        {
            this.instructionsStrategy = strategy;
            return this;
        }

        public Builder WithName(AgentName name)
        {
            this.name = name;
            return this;
        }

        public CustomAgent Build()
        {
            return new CustomAgent(
                role: this.role ?? throw new InvalidDataException("Role is required but was null"),
                prompt: this.prompt,
                instructionStrategy: this.instructionsStrategy,
                name: this.name ?? throw new InvalidDataException("Name is required but was null"),
                messageVisibility: this.visibility,
                promptRenderer: this.promptRenderer,
                completionsClient: this.completionsClient
                    ?? throw new InvalidDataException("CompletionsClient is required but was null"),
                responseSchema: this.responseSchema);
        }
    }
}
