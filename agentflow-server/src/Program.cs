using Agentflow.Server.Handler;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

builder.Services.AddSingleton<ChatCompletionsHandler>();
builder.Services.AddSingleton<ConfigHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost(
    "/v1/chat/completions",
    async ([FromServices] ChatCompletionsHandler handler, [FromBody] ChatCompletionsRequest request)
        => await handler.HandleAsync(request))
    .WithOpenApi();

app.MapPost(
    "/config",
    async ([FromServices] ConfigHandler handler, [FromBody] ConfigRequest request)
        => await handler.HandleAsync(request))
    .WithOpenApi();

app.Run();
