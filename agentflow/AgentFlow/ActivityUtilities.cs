using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AgentFlow.WorkSpace;

namespace AgentFlow.Utilities;

public static class ActivityUtilities
{
    private const string RequestIdName = "requestId";

    public static Activity StartConversationActivity(ConversationId conversationId)
#pragma warning disable CA2000 // Dispose objects before losing scope
        => new Activity("Conversation")
            .AddBaggage("ConversationId", conversationId.Value)
            .Start();
#pragma warning restore CA2000 // Dispose objects before losing scope

    public static bool TryGetConversationIdFromCurrentActivity([NotNullWhen(true)] out ConversationId? conversationId)
    {
        conversationId = null;

        Activity? currentActivity = Activity.Current;

        if (currentActivity is null)
        {
            return false;
        }

        string? rawConversationId = currentActivity.GetBaggageItem("ConversationId");

        if (rawConversationId is null)
        {
            return false;
        }

        conversationId = new ConversationId(rawConversationId);

        return true;
    }
}
