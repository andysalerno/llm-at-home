using Microsoft.Extensions.Logging;

namespace AgentFlow;

public static class Logging
{
    private static readonly object Lock = new object();

    private static ILoggerFactory? loggerFactory;

    public static ILoggerFactory Factory
        => Logging.loggerFactory
            ?? throw new InvalidOperationException("Logger was never registered");

    public static void TryRegisterLoggerFactory(ILoggerFactory loggerFactory)
    {
        if (Logging.loggerFactory != null)
        {
            return;
        }

        lock (Lock)
        {
            if (Logging.loggerFactory == null)
            {
                Logging.loggerFactory = loggerFactory;
            }
        }
    }

    public static void RegisterLoggerFactory(ILoggerFactory loggerFactory)
    {
        if (Logging.loggerFactory != null)
        {
            throw new InvalidOperationException("Logger factory was already registered");
        }

        lock (Lock)
        {
            if (Logging.loggerFactory != null)
            {
                throw new InvalidOperationException("Logger factory was already registered");
            }

            Logging.loggerFactory = loggerFactory;
        }
    }
}

public static class LoggingExtensions
{
    public static ILogger<T> GetLogger<T>(this T input)
    {
        return Logging.Factory.CreateLogger<T>()
            ?? throw new InvalidOperationException("Logger was never registered");
    }
}
