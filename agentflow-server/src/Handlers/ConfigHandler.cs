namespace Agentflow.Server.Handler;

internal sealed class ConfigHandler : IHandler<ConfigRequest, ConfigResponse>
{
    public Task<ConfigResponse> HandleAsync(ConfigRequest payload)
    {
        return Task.FromResult(new ConfigResponse());
    }
}

internal sealed record ConfigRequest();

internal sealed record ConfigResponse();
