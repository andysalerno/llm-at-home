// TODO: extract this to a distinct project

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
            throw new ArgumentNullException("Expected non-null value, but found null");
        }

        return input;
    }
}
