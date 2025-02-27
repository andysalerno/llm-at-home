namespace Agentflow.Server.Handler;

public interface IHandler<TPayload, TResponse>
{
    Task<TResponse> HandleAsync(TPayload payload);
}

public interface IStreamingHandler<TPayload>
{
    Task HandleAsync(TPayload payload, IStreamingPublisher publisher, CancellationToken ct);
}

public interface IStreamingPublisher
{
    Task PublishAsync(string data, CancellationToken ct);
}

public sealed class HttpContextStreamingPublisher : IStreamingPublisher
{
    private readonly HttpContext context;

    public HttpContextStreamingPublisher(HttpContext context)
    {
        this.context = context;

        this.context
            .Response
            .Headers
            .Append("Content-Type", "text/event-stream");
    }

    public async Task PublishAsync(string data, CancellationToken ct)
    {
        await this.context.Response.WriteAsync("data: ", ct);
        await this.context.Response.WriteAsync(data, ct);
        await this.context.Response.WriteAsync("\n\n", ct);
        await this.context.Response.Body.FlushAsync(ct);
    }
}