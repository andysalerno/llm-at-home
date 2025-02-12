namespace Agentflow.Server.Handler;

internal sealed class ChatCompletionsHandler : IHandler<ChatCompletionsRequest, ChatCompletionsResponse>
{
    private readonly ILogger<ChatCompletionsHandler> logger;

    public string Path => "/v1/chat/completions";

    public ChatCompletionsHandler(ILogger<ChatCompletionsHandler> logger)
    {
        this.logger = logger;
    }


    public Task<ChatCompletionsResponse> HandleAsync(ChatCompletionsRequest payload)
    {
        logger.LogInformation("payload received :)");
        return Task.FromResult(new ChatCompletionsResponse());
    }
}

internal sealed record ChatCompletionsRequest();
internal sealed record ChatCompletionsResponse();