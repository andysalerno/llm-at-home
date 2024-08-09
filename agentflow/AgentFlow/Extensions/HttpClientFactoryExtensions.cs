namespace AgentFlow.Agents.Extensions;

public static class HttpClientFactoryExtensions
{
    public static HttpClient CreateClient<T>(this IHttpClientFactory factory)
    {
        string typeName = typeof(T).Name;

        return factory.CreateClient(typeName);
    }
}
