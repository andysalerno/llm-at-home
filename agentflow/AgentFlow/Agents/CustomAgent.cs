﻿using System.Collections.Immutable;
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

public class CustomAgent : IAgent
{
    private readonly MessageVisibility messageVisibility;
    private readonly ILlmCompletionsClient completionsClient;
    private readonly JsonElement? responseSchema;
    private readonly ILogger<CustomAgent> logger;

    private CustomAgent(
        Role role,
        string instructions,
        InstructionStrategy instructionStrategy,
        AgentName name,
        MessageVisibility messageVisibility,
        ILlmCompletionsClient completionsClient,
        JsonElement? responseSchema = null)
    {
        this.Role = role;
        this.Instructions = instructions;
        this.InstructionStrategy = instructionStrategy;
        this.completionsClient = completionsClient;
        this.responseSchema = responseSchema;
        this.Name = name;
        this.messageVisibility = messageVisibility;
        this.logger = this.GetLogger();
    }

    public Role Role { get; private set; }

    public AgentName Name { get; private set; }

    public string ModelDescription { get; } = "blah";

    public bool IsCodeProvider { get; }

    public string Instructions { get; private set; }

    public InstructionStrategy InstructionStrategy { get; private set; }

    public Task<Cell<ConversationThread>> GetNextThreadStateAsync(ConversationThread conversationThread)
    {
        Cell<ConversationThread> sequence = new CellSequence<ConversationThread>(
            sequence: new Cell<ConversationThread>[]
            {
                new SetSystemMessageCell(this.Name, new Prompt(this.Instructions)),
                new GetAssistantResponseCell(this.Name, this.Role, this.responseSchema, this.completionsClient),
            }.ToImmutableArray(),
            next: null);

        return Task.FromResult(sequence);
    }

    public class Builder
    {
        private Role? role;
        private string? instructions;
        private AgentName? name;
        private InstructionStrategy instructionsStrategy = InstructionStrategy.TopLevelSystemMessage;
        private ILlmCompletionsClient completionsClient;
        private JsonElement? responseSchema;
        private MessageVisibility visibility;

        public Builder(ILlmCompletionsClient completionsClient)
        {
            this.completionsClient = completionsClient;
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

        public Builder WithInstructions(string instructions)
        {
            this.instructions = instructions;

            return this;
        }

        public Builder WithJsonResponseSchema(JsonElement responseSchema)
        {
            this.responseSchema = responseSchema;

            return this;
        }

        public Builder WithInstructionsFromPrompt(Prompt prompt)
        {
            this.instructions = prompt.Render();

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
                instructions: this.instructions ?? throw new InvalidDataException("Instructions is required but was null"),
                instructionStrategy: this.instructionsStrategy,
                name: this.name ?? throw new InvalidDataException("Name is required but was null"),
                messageVisibility: this.visibility,
                responseSchema: this.responseSchema,
                completionsClient: this.completionsClient ?? throw new InvalidDataException("CompletionsClient is required but was null"));
        }
    }
}