namespace Agentflow.Server.Handler;

public interface IHandler<TPayload, TResponse>
{
    Task<TResponse> HandleAsync(TPayload payload);
}
