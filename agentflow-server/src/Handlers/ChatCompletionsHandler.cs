using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFlow;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Agents.Tools;
using AgentFlow.Config;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;

namespace Agentflow.Server.Handler;

internal sealed class ChatCompletionsHandler : IHandler<ChatCompletionRequest, ChatCompletionStreamingResponse>
{
    private readonly IFactoryProvider<Prompt, PromptName> promptFactoryProvider;
    private readonly ICellRunner<ConversationThread> runner;
    private readonly IEnvironmentVariableProvider environmentVariableProvider;
    private readonly IEmbeddingsClient embeddingsClient;
    private readonly IScraperClient scraperClient;
    private readonly IPromptRenderer promptRenderer;
    private readonly CustomAgentBuilderFactory agentBuilderFactory;
    private readonly Configuration configuration;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<ChatCompletionsHandler> logger;

    public ChatCompletionsHandler(
        IFactoryProvider<Prompt, PromptName> promptFactoryProvider,
        ICellRunner<ConversationThread> runner,
        IEnvironmentVariableProvider environmentVariableProvider,
        IEmbeddingsClient embeddingsClient,
        IScraperClient scraperClient,
        IPromptRenderer promptRenderer,
        CustomAgentBuilderFactory agentBuilderFactory,
        Configuration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<ChatCompletionsHandler> logger)
    {
        this.promptFactoryProvider = promptFactoryProvider;
        this.runner = runner;
        this.environmentVariableProvider = environmentVariableProvider;
        this.embeddingsClient = embeddingsClient;
        this.scraperClient = scraperClient;
        this.promptRenderer = promptRenderer;
        this.agentBuilderFactory = agentBuilderFactory;
        this.configuration = configuration;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task<ChatCompletionStreamingResponse> HandleAsync(ChatCompletionRequest payload)
    {
        this.logger.LogInformation("payload received :)");

        ConversationThread conversationThread = ToConversationThread(payload);

        ConversationThread output = await this.runner.RunAsync(this.CreateProgram(), rootInput: conversationThread);

        return new ChatCompletionStreamingResponse(
            [
                new ChatChoice(
                Index: 0,
                Delta: new Delta(Role: "assistant", Content: output.Messages.Last().Content))
            ],
            Model: payload.Model);
    }

    private static ConversationThread ToConversationThread(ChatCompletionRequest request)
    {
        var messages = request.Messages.Select(m => new AgentFlow.LlmClient.Message(
            AgentName: new AgentName(m.Role),
            Role: Role.ExpectFromName(m.Role),
            Content: m.Content.Text));

        return ConversationThread.CreateBuilder().AddMessages(messages).Build();
    }

    public Cell<ConversationThread> CreateProgram()
    {
        ImmutableArray<ITool> tools = [
            new WebSearchTool(
                this.agentBuilderFactory,
                this.runner,
                this.environmentVariableProvider,
                this.embeddingsClient,
                this.scraperClient,
                this.promptRenderer,
                this.promptFactoryProvider.GetFactory(DefaultPrompts.RewriteQuerySystem),
                this.httpClientFactory),

            new WebSearchTool(
                this.agentBuilderFactory,
                this.runner,
                this.environmentVariableProvider,
                this.embeddingsClient,
                this.scraperClient,
                this.promptRenderer,
                this.promptFactoryProvider.GetFactory(DefaultPrompts.RewriteQuerySystem),
                this.httpClientFactory,
                searchSiteUris: ["nytimes.com", "cnn.com", "apnews.com", "cbsnews.com"],
                toolName: "search_news",
                exampleQueries: ("2024 election polls", "seattle heat wave", "stock market performance")),
        ];

        return new AgentCell(
            new ToolAgent(
                new AgentName("WebSearchAgent"),
                Role.Assistant,
                this.promptFactoryProvider,
                this.agentBuilderFactory,
                this.configuration.InstructionStrategy,
                tools));
    }
}

internal sealed record ChatCompletionRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] ImmutableArray<Message> Messages);

internal sealed record Message(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonConverter(typeof(MessageContentConverter))]
    [property: JsonPropertyName("content")] MessageContent Content);

internal sealed record MessageContent(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("type")] string Type = "text");

internal sealed record ChatCompletionStreamingResponse(
    [property: JsonPropertyName("choices")] ImmutableArray<ChatChoice> Choices,
    [property: JsonPropertyName("model")] string Model = "mymodel",
    [property: JsonPropertyName("object")] string Object = "chat.completion.chunk");

internal sealed record ChatChoice(
   [property: JsonPropertyName("index")] int Index,
   [property: JsonPropertyName("delta")] Delta Delta,
   [property: JsonPropertyName("finish_reason")] string? FinishReason = null);

internal sealed record Delta(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed class MessageContentConverter : JsonConverter<MessageContent>
{
    public override MessageContent? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string content = reader.GetString() ?? throw new JsonException();
            return new MessageContent(Text: content);
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var value = doc.RootElement.Clone();

                var values = value.Deserialize<List<Dictionary<string, string>>>()
                    ?? throw new JsonException();

                return new MessageContent(Text: values.First()["text"]);
            }
        }

        throw new JsonException($"Expected StartArray or String, saw {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        MessageContent value,
        JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
