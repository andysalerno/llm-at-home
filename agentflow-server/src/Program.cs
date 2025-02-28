using Agentflow.Server;
using Agentflow.Server.Handler;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLogging(c => c.AddSimpleConsole(o =>
{
    o.IncludeScopes = true;
    o.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
    o.SingleLine = true;
    o.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
}));

builder.Services.AddHttpClient();
builder.Services.AddCors();
builder.Services.AddAgentFlow();

builder.Services.AddSingleton<ChatCompletionsHandler>();
builder.Services.AddSingleton<ConfigHandler>();
builder.Services.AddSingleton<TranscriptHandler>();

var app = builder.Build();

AgentFlow.Logging.RegisterLoggerFactory(app.Services.GetRequiredService<ILoggerFactory>());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// allow OPTIONS requests via CORS:
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapPost(
    "/v1/chat/completions",
    async (
            HttpContext context,
            [FromServices] ChatCompletionsHandler handler,
            [FromBody] ChatCompletionRequest request,
            CancellationToken ct)
        => await PostCompletionsStreamingAsync(context, handler, request, ct))
    .WithOpenApi();

app.MapPost(
    "/config",
    async ([FromServices] ConfigHandler handler, [FromBody] ConfigRequest request)
        => await handler.HandleAsync(request))
    .WithOpenApi();

app.MapGet(
    "/transcripts",
    async ([FromServices] TranscriptHandler handler, [FromBody] TranscriptRequest request)
        => await handler.HandleAsync(request))
    .WithOpenApi();

async Task PostCompletionsStreamingAsync(
    HttpContext context,
    ChatCompletionsHandler handler,
    ChatCompletionRequest request,
    CancellationToken ct)
{
    var stream = new HttpContextStreamingPublisher(context);

    await handler.HandleAsync(request, stream, ct);
}

app.Run();
