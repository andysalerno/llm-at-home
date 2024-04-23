using Microsoft.Extensions.Logging;

namespace AgentFlow.CodeExecution;

public class HostedCodeExecutor : ICodeExecutor
{
    private static readonly Uri ServerEndpoint = new Uri("http://192.168.50.44:8000");

    private readonly HttpClient httpClient;
    private readonly ILogger<HostedCodeExecutor> logger;

    public HostedCodeExecutor(IHttpClientFactory httpClientFactory, ILogger<HostedCodeExecutor> logger)
    {
        this.httpClient = httpClientFactory.CreateClient();
        this.logger = logger;
    }

    public async Task<string> ExecuteCodeAsync(string code)
    {
        this.logger.LogInformation("Making request to python server uri {Uri}", ServerEndpoint);

        using var content = new StringContent(code);
        var response = await this.httpClient.PostAsync(ServerEndpoint, content);

        string text = await response.Content.ReadAsStringAsync();

        this.logger.LogInformation("Got text response: {Text}", text);

        return text;
    }
}
