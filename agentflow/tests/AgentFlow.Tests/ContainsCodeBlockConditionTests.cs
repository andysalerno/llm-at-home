using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;

namespace AgentFlow.Tests;

public class ContainsCodeBlockConditionTests
{
    private static ConversationId ConversationId => new ConversationId("test");

    [Fact]
    public void EmptyConversation_ReturnsFalse()
    {
        var condition = new ContainsCodeBlockCondition();

        var conversation = new ConversationThread(ConversationId);

        bool result = condition.Evaluate(conversation);

        Assert.False(result);
    }

    [Fact]
    public void LastMessage_IsCodeBlock_ExpectsTrue()
    {
        var condition = new ContainsCodeBlockCondition();

        var conversation = new ConversationThread(ConversationId)
            .WithAddedMessage(AssistantMessageWithText("```javascript\nfn someFun() {}\n```"));

        bool result = condition.Evaluate(conversation);

        Assert.True(result);
    }

    [Fact]
    public void LastMessage_HasCodeBlock_ExpectsTrue()
    {
        var condition = new ContainsCodeBlockCondition();

        var conversation = new ConversationThread(ConversationId)
            .WithAddedMessage(
                AssistantMessageWithText("Here is some javascript: \n```javascript\nfn someFun() {}\n```"));

        bool result = condition.Evaluate(conversation);

        Assert.True(result);
    }

    [Fact]
    public void LastMessage_NotHasCodeBlock_ExpectsFalse()
    {
        var condition = new ContainsCodeBlockCondition();

        var conversation = new ConversationThread(ConversationId)
            .WithAddedMessage(AssistantMessageWithText(
                "Here is some javascript, but not as a codeblock: \njavascript\nfn someFun() {}"));

        bool result = condition.Evaluate(conversation);

        Assert.False(result);
    }

    [Fact]
    public void SecondToLastMessage_HasCodeBlock_ExpectsFalse()
    {
        var condition = new ContainsCodeBlockCondition();

        var conversation = new ConversationThread(ConversationId)
            .WithAddedMessage(
                AssistantMessageWithText("Here is some javascript: \n```javascript\nfn someFun() {}\n```"))
            .WithAddedMessage(AssistantMessageWithText("some other message is the latest one"));

        bool result = condition.Evaluate(conversation);

        Assert.False(result);
        Assert.Equal(2, conversation.Messages.Count);
    }

    private static Message AssistantMessageWithText(string text)
    {
        return new Message(new AgentName("assistant"), Role.Assistant, Content: text);
    }
}
