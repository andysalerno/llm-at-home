// TODO: extract this to a distinct project

namespace AgentFlow.Util;

public static class NullUtilities
{
    public static T OrElse<T>(this T? input, Func<T> output)
    {
        if (input == null)
        {
            return output();
        }

        return input;
    }
}
