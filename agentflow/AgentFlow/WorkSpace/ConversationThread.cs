using System.Collections.Immutable;
using AgentFlow.Agents;
using AgentFlow.LlmClient;

namespace AgentFlow.WorkSpace;

/// <summary>
/// A thread is a conversation between two agents.
/// A Workspace can have multiple threads. For example, in a Workspace with three agents:
/// - Agent A and Agent B can have an ongoing thread
/// - Agent A and Agent C can have an ongoing thread
/// - Agents A, B, and C can have an ongoing thread
/// Either agent in a thread can decide to terminate it.
/// </summary>
public sealed class ConversationThread
{
    private readonly ImmutableArray<Message> messageList;

    // TODO: deprecate?
    private readonly Dictionary<string, string> templateKeyValuePairs;

    public ConversationThread(ConversationId conversationId)
    {
        this.messageList = ImmutableArray<Message>.Empty;
        this.templateKeyValuePairs = new Dictionary<string, string>();
        this.ConversationId = conversationId;
    }

    private ConversationThread(ConversationId conversationId, ImmutableArray<Message> messages)
    {
        this.messageList = messages;
        this.templateKeyValuePairs = new Dictionary<string, string>();
        this.ConversationId = conversationId;
    }

    private ConversationThread(
        ConversationId conversationId,
        ImmutableArray<Message> messages,
        IDictionary<string, string> keyValuePairs)
    {
        this.messageList = messages;
        this.templateKeyValuePairs = keyValuePairs.ToDictionary();
        this.ConversationId = conversationId;
    }

    public ConversationId ConversationId { get; }

    public IReadOnlyList<Message> Messages => this.messageList;

    /// <summary>
    /// Gets the agent names involved in this thread.
    /// </summary>
    public IEnumerable<AgentName> AgentNames => this.messageList.Select(m => m.AgentName).Distinct();

    public static Builder CreateBuilder(ConversationId conversationId)
    {
        return new Builder(conversationId);
    }

    /// <summary>
    /// Returns a copy of this <see cref="ConversationThread"/> with the same message history, including a new message.
    /// </summary>
    /// <returns>A copy of the <see cref="ConversationThread"/> with the added <paramref name="message"/>.</returns>
    /// <param name="message">The message to add.</param>
    public ConversationThread WithAddedMessage(Message message)
        => this.WithAddedMessages(new[] { message });

    public ConversationThread WithAddedMessages(IEnumerable<Message> messages)
    {
        var builder = new Builder(this.ConversationId).CopyFrom(this);

        foreach (var message in messages)
        {
            builder.AddMessage(message);
        }

        return builder.Build();
    }

    public ConversationThread WithMatchingMessages(Func<Message, bool> predicate)
    {
        return new Builder(this.ConversationId)
            .CopyFrom(this, predicate)
            .Build();
    }

    /// <summary>
    /// Returns a ConversationThread with the same message history, excluding System messages.
    /// </summary>
    /// <param name="systemMessage">The system message to set.</param>
    public ConversationThread WithFirstMessageSystemMessage(Message systemMessage)
    {
        return new Builder(this.ConversationId)
            .CopyFrom(this)
            .RemoveTopSystemMessage()
            .AddMessageToFront(systemMessage)
            .Build();
    }

    // TODO: deprecate?
    public ConversationThread WithTemplateValue(string key, string value)
    {
        var output = new Builder(this.ConversationId).CopyFrom(this).Build();

        output.templateKeyValuePairs[key] = value;

        return output;
    }

    public ConversationThread WithMessagesVisibleToAssistant()
    {
        var messages = this.messageList.Where(m => m.Visibility?.ShownToModel ?? true);

        return new Builder(this.ConversationId)
            .CopyFrom(this)
            .ReplaceMessages(messages)
            .Build();
    }

    // TODO: deprecate?
    public ConversationThread WithTemplateAppliedToSystem()
    {
        Message? topSystemMessage = this.messageList.FirstOrDefault();

        if (topSystemMessage == null || topSystemMessage.Role != Role.System)
        {
            return this;
        }

        string systemMessageContent = topSystemMessage.Content;

        foreach (var (k, v) in this.templateKeyValuePairs)
        {
            systemMessageContent = systemMessageContent.Replace("{{" + k + "}}", v, StringComparison.OrdinalIgnoreCase);
        }

        return this.WithFirstMessageSystemMessage(
            new Message(topSystemMessage.AgentName, topSystemMessage.Role, systemMessageContent));
    }

    public sealed class Builder
    {
        private readonly ImmutableArray<Message>.Builder messages;
        private readonly Dictionary<string, string> templateKeyValuePairs;
        private readonly ConversationId conversationId;

        internal Builder(ConversationId conversationId)
        {
            this.templateKeyValuePairs = new Dictionary<string, string>();
            this.messages = ImmutableArray.CreateBuilder<Message>();
            this.conversationId = conversationId;
        }

        public Builder CopyFrom(ConversationThread other)
        {
            return this.CopyFrom(other, _ => true);
        }

        public Builder CopyFrom(ConversationThread other, Func<Message, bool> predicate)
        {
            foreach (var (k, v) in other.templateKeyValuePairs)
            {
                this.templateKeyValuePairs[k] = v;
            }

            this.messages.AddRange(other.messageList.Where(predicate));

            return this;
        }

        public Builder AddMessage(Message message)
        {
            this.messages.Add(message);

            return this;
        }

        public Builder AddMessageToFront(Message message)
        {
            this.messages.Insert(0, message);

            return this;
        }

        public Builder RemoveTopSystemMessage()
        {
            this.messages.RemoveAll(m => m.Role == Role.System);

            if (this.messages.FirstOrDefault() is Message m
                && m.Role == Role.System)
            {
                this.messages.RemoveAt(0);
            }

            return this;
        }

        public Builder AddMessages(IEnumerable<Message> messages)
        {
            this.messages.AddRange(messages);

            return this;
        }

        public Builder ReplaceMessages(IEnumerable<Message> messages)
        {
            this.messages.Clear();
            this.messages.AddRange(messages);

            return this;
        }

        public ConversationThread Build()
        {
            return new ConversationThread(
                this.conversationId,
                this.messages.DrainToImmutable(),
                this.templateKeyValuePairs);
        }
    }
}
