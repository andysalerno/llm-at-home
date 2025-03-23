using System.Collections.Immutable;
using System.Text.Json.Nodes;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents;

public sealed class CustomAgent : IAgent
{
    private readonly MessageVisibility messageVisibility;
    private readonly IPromptRenderer promptRenderer;
    private readonly ILlmCompletionsClient completionsClient;
    private readonly ImmutableDictionary<string, string> variables;
    private readonly JsonObject? responseSchema;
    private readonly string? toolChoice;
    private readonly ILogger<CustomAgent> logger;

    private CustomAgent(
        Role role,
        Prompt? prompt,
        InstructionStrategy instructionStrategy,
        ToolOutputStrategy toolOutputStrategy,
        AgentName name,
        MessageVisibility messageVisibility,
        ImmutableDictionary<string, string> variables,
        IPromptRenderer promptRenderer,
        ILlmCompletionsClient completionsClient,
        JsonObject? responseSchema = null,
        string? toolChoice = null)
    {
        this.Role = role;
        this.Prompt = prompt;
        this.InstructionStrategy = instructionStrategy;
        this.ToolOutputStrategy = toolOutputStrategy;
        this.completionsClient = completionsClient;
        this.responseSchema = responseSchema;
        this.toolChoice = toolChoice;
        this.Name = name;
        this.variables = variables;
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

    public ToolOutputStrategy ToolOutputStrategy { get; }

    public Task<Cell<ConversationThread>> GetNextConversationStateAsync()
    {
        var cells = ImmutableArray.CreateBuilder<Cell<ConversationThread>>();
        {
            if (this.Prompt is Prompt p)
            {
                RenderedPrompt rendered = this.promptRenderer.Render(p, this.variables);
                cells.Add(new SetSystemMessageCell(this.Name, rendered));
            }
            else
            {
                this.logger.LogInformation("No system prompt set; this may be intentional");
            }

            cells.Add(new GetAssistantResponseCell(
                this.Name,
                this.Role,
                this.responseSchema,
                this.toolChoice,
                this.InstructionStrategy,
                this.ToolOutputStrategy,
                this.completionsClient));
        }

        Cell<ConversationThread> sequence = new CellSequence<ConversationThread>(
            sequence: cells.DrainToImmutable());

        return Task.FromResult(sequence);
    }

    public class Builder
    {
        private readonly IPromptRenderer promptRenderer;
        private readonly ImmutableDictionary<string, string>.Builder keyValuePairs;
        private Role? role;
        private Prompt? prompt;
        private AgentName? name;
        private InstructionStrategy instructionsStrategy = InstructionStrategy.TopLevelSystemMessage;
        private ToolOutputStrategy toolOutputStrategy = ToolOutputStrategy.AppendedToUserMessage;
        private ILlmCompletionsClient completionsClient;
        private JsonObject? responseSchema;
        private MessageVisibility visibility;
        private string? toolChoice;

        public Builder(ILlmCompletionsClient completionsClient, IPromptRenderer promptRenderer)
        {
            this.completionsClient = completionsClient;
            this.promptRenderer = promptRenderer;
            this.visibility = new MessageVisibility(ShownToUser: true, ShownToModel: true);
            this.keyValuePairs = ImmutableDictionary.CreateBuilder<string, string>();

            this.responseSchema = null;
            this.toolChoice = null;
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

        public Builder WithJsonResponseSchema(JsonObject responseSchema)
        {
            this.responseSchema = responseSchema;

            return this;
        }

        public Builder WithToolChoice(string toolChoice)
        {
            this.toolChoice = toolChoice;

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

        public Builder WithToolOutputStrategy(ToolOutputStrategy strategy)
        {
            this.toolOutputStrategy = strategy;
            return this;
        }

        public Builder SetVariableValue(string key, string value)
        {
            this.keyValuePairs[key] = value;

            return this;
        }

        public Builder SetVariableValues(IEnumerable<KeyValuePair<string, string>> kvps)
        {
            foreach (var kvp in kvps)
            {
                this.keyValuePairs[kvp.Key] = kvp.Value;
            }

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
                toolOutputStrategy: this.toolOutputStrategy,
                name: this.name ?? throw new InvalidDataException("Name is required but was null"),
                messageVisibility: this.visibility,
                variables: this.keyValuePairs.ToImmutable(),
                promptRenderer: this.promptRenderer,
                completionsClient: this.completionsClient
                    ?? throw new InvalidDataException("CompletionsClient is required but was null"),
                responseSchema: this.responseSchema);
        }
    }
}
