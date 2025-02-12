namespace Agentflow.Server.Handler;

internal sealed class TranscriptHandler : IHandler<TranscriptRequest, TranscriptResponse>
{
    public Task<TranscriptResponse> HandleAsync(TranscriptRequest payload)
    {
        return Task.FromResult(new TranscriptResponse());
    }
}

internal sealed record TranscriptRequest();

internal sealed record TranscriptResponse();
