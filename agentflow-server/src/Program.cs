using Agentflow.Server;
using Agentflow.Server.Handler;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
builder.Services.AddAgentFlow();

builder.Services.AddSingleton<ChatCompletionsHandler>();
builder.Services.AddSingleton<ConfigHandler>();
builder.Services.AddSingleton<TranscriptHandler>();

var app = builder.Build();

AgentFlow.Logging.RegisterLoggerFactory(app.Services.GetRequiredService<ILoggerFactory>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost(
    "/v1/chat/completions",
    async ([FromServices] ChatCompletionsHandler handler, [FromBody] ChatCompletionRequest request)
        => await handler.HandleAsync(request))
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

app.Run();
