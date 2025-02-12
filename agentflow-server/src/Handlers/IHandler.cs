namespace Agentflow.Server.Handler;

public interface IHandler<TPayload, TResponse>
{
    string Path { get; }

    Task<TResponse> HandleAsync(TPayload payload);
}
