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

    private readonly Dictionary<string, string> templateKeyValuePairs;

    public ConversationThread()
    {
        this.messageList = ImmutableArray<Message>.Empty;
        this.templateKeyValuePairs = new Dictionary<string, string>();
    }

    private ConversationThread(ImmutableArray<Message> messages)
    {
        this.messageList = messages;
        this.templateKeyValuePairs = new Dictionary<string, string>();
    }

    private ConversationThread(ImmutableArray<Message> messages, IDictionary<string, string> keyValuePairs)
        : this(messages)
    {
        this.templateKeyValuePairs = keyValuePairs.ToDictionary();
    }

    public IReadOnlyList<Message> Messages => this.messageList;

    /// <summary>
    /// Gets the agent names involved in this thread.
    /// </summary>
    public IEnumerable<AgentName> AgentNames => this.messageList.Select(m => m.AgentName).Distinct();

    public static Builder CreateBuilder()
    {
        return new Builder();
    }

    /// <summary>
    /// Returns a copy of this <see cref="ConversationThread"/> with the same message history, including a new message.
    /// </summary>
    /// <returns>A copy of the <see cref="ConversationThread"/> with the added <paramref name="message"/>.</returns>
    /// <param name="message">The message to add.</param>
    public ConversationThread WithAddedMessage(Message message)
    {
        return new Builder()
            .CopyFrom(this)
            .AddMessage(message)
            .Build();
    }

    public ConversationThread WithMatchingMessages(Func<Message, bool> predicate)
    {
        return new Builder()
            .CopyFrom(this)
            .Build();
    }

    /// <summary>
    /// Returns a ConversationThread with the same message history, excluding System messages.
    /// </summary>
    public ConversationThread WithoutSystem()
    {
        return new Builder()
            .CopyFrom(this)
            .RemoveSystem()
            .Build();
    }

    /// <summary>
    /// Returns a ConversationThread with the same message history, excluding System messages.
    /// </summary>
    /// <param name="systemMessage">The system message to set.</param>
    public ConversationThread WithSystemMessage(Message systemMessage)
    {
        return new Builder()
            .CopyFrom(this)
            .RemoveSystem()
            .AddMessageToFront(systemMessage)
            .Build();
    }

    public ConversationThread WithTemplateValue(string key, string value)
    {
        var output = new Builder().CopyFrom(this).Build();

        output.templateKeyValuePairs[key] = value;

        return output;
    }

    public ConversationThread WithMessagesVisibleToAssistant()
    {
        var messages = this.messageList.Where(m => m.Visibility?.ShownToModel ?? true);

        return new Builder()
            .CopyFrom(this)
            .ReplaceMessages(messages)
            .Build();
    }

    public ConversationThread WithTemplateAppliedToSystem()
    {
        Message? systemMessage = this.messageList.FirstOrDefault(m => m.Role == Role.System);

        if (systemMessage == null)
        {
            return this;
        }

        string systemMessageContent = systemMessage.Content;

        foreach (var (k, v) in this.templateKeyValuePairs)
        {
            systemMessageContent = systemMessageContent.Replace("{{" + k + "}}", v, StringComparison.OrdinalIgnoreCase);
        }

        return this.WithSystemMessage(
            new Message(systemMessage.AgentName, systemMessage.Role, systemMessageContent));
    }

    public sealed class Builder
    {
        private readonly ImmutableArray<Message>.Builder messages;
        private readonly Dictionary<string, string> templateKeyValuePairs;

        internal Builder()
        {
            this.templateKeyValuePairs = new Dictionary<string, string>();
            this.messages = ImmutableArray.CreateBuilder<Message>();
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

        public Builder RemoveSystem()
        {
            this.messages.RemoveAll(m => m.Role == Role.System);

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
            return new ConversationThread(this.messages.DrainToImmutable(), this.templateKeyValuePairs);
        }
    }
}
