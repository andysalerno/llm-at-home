namespace Agentflow.Server.Handler;

internal sealed class ChatCompletionsHandler : IHandler<ChatCompletionsRequest, ChatCompletionsResponse>
{
    private readonly ILogger<ChatCompletionsHandler> logger;

    public ChatCompletionsHandler(ILogger<ChatCompletionsHandler> logger)
    {
        this.logger = logger;
    }

    public Task<ChatCompletionsResponse> HandleAsync(ChatCompletionsRequest payload)
    {
        this.logger.LogInformation("payload received :)");
        return Task.FromResult(new ChatCompletionsResponse());
    }
}

internal sealed record ChatCompletionsRequest();

internal sealed record ChatCompletionsResponse();
