using System.Diagnostics;

namespace Agentflow.Server.Utilities;

public static class ActivityExtensions
{
    private const string RequestIdName = "requestId";

    public static Activity AddRequestIdBaggage(this Activity activity)
    {
        return activity.AddBaggage(RequestIdName, Guid.NewGuid().ToString().Substring(0, 8));
    }

    public static string? GetRequestIdBaggage(this Activity activity)
    {
        return activity.GetBaggageItem(RequestIdName);
    }
}