// TODO: extract this to a distinct project

using System.Diagnostics;

namespace AgentFlow.Util;

public static class NullUtilities
{
    public static T OrElse<T>(this T? input, Func<T> output)
    {
        return input ?? output();
    }

    public static T NonNullOrThrow<T>(this T? input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        return input;
    }
}

public static class ActivityExtensions
{
    public static Activity AddRequestIdBaggage(this Activity activity)
    {
        return activity.AddBaggage("requestId", Guid.NewGuid().ToString().Substring(0, 8));
    }

    public static string? GetRequestIdBaggage(this Activity activity)
    {
        return activity.GetBaggageItem("requestId");
    }
}
