using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agentflow.Server.Serialization;
using AgentFlow;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Agents.Tools;
using AgentFlow.Config;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.Tools;
using AgentFlow.Utilities;
using AgentFlow.WorkSpace;

namespace Agentflow.Server.Handler;

internal sealed class ChatCompletionsHandler : IStreamingHandler<ChatCompletionRequest>
{
    private readonly IFactoryProvider<Prompt, PromptName> promptFactoryProvider;
    private readonly ICellRunner<ConversationThread> runner;
    private readonly IEnvironmentVariableProvider environmentVariableProvider;
    private readonly IEmbeddingsClient embeddingsClient;
    private readonly IScraperClient scraperClient;
    private readonly IPromptRenderer promptRenderer;
    private readonly CustomAgentBuilderFactory agentBuilderFactory;
    private readonly Configuration configuration;
    private readonly IConversationPersistenceWriter conversationPersistenceWriter;
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
        IConversationPersistenceWriter conversationPersistenceWriter,
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
        this.conversationPersistenceWriter = conversationPersistenceWriter;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task HandleAsync(
        ChatCompletionRequest payload,
        IStreamingPublisher publisher,
        CancellationToken ct)
    {
        var requestId = new IncomingRequestId(Guid.NewGuid().ToString());

        this.logger.LogInformation(
            "ChatCompletions request received. ConversationId from caller: {ConversationId} Generated RequestId: {RequestId}",
            payload.ConversationId,
            requestId);

        this.logger.LogInformation(
            "Caller-supplied configuration: {Config}", JsonSerializer.Serialize(payload.AgentFlowConfig));

        var conversationId = new ConversationId(payload.ConversationId ?? Guid.NewGuid().ToString());

        using var activity = ActivityUtilities.StartConversationActivity(conversationId, requestId);

        ConversationThread conversationThread = ToConversationThread(payload, conversationId);

        ConversationThread output = await this.runner.RunAsync(
            this.CreateProgram(payload.AgentFlowConfig),
            rootInput: conversationThread);

        await publisher.PublishAsync(
            JsonSerializer.Serialize(new ChatCompletionStreamingResponse(
                Choices: [new ChatChoice(
                    Index: 0,
                    Delta: new Delta(Role: "assistant", Content: output.Messages.Last().Content))],
                Model: payload.Model)),
            ct);

        await this.conversationPersistenceWriter.StoreUserMessageAsync(
            conversationId,
            requestId,
            new StoredMessage(Role: "user", Content: payload.Messages.Last().Content.Text, requestId));

        await this.conversationPersistenceWriter.StoreUserMessageAsync(
            conversationId,
            requestId,
            new StoredMessage(Role: "assistant", Content: output.Messages.Last().Content, requestId));
    }

    private static ConversationThread ToConversationThread(ChatCompletionRequest request, ConversationId conversationId)
    {
        var messages = request.Messages.Select(m => new AgentFlow.LlmClient.Message(
            AgentName: new AgentName(m.Role),
            Role: Role.ExpectFromName(m.Role),
            Content: m.Content.Text));

        return ConversationThread.CreateBuilder(conversationId).AddMessages(messages).Build();
    }

    private Cell<ConversationThread> CreateProgram(AgentFlowRequestConfig? agentFlowConfig = null)
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

        var configDictionary = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(agentFlowConfig?.InstructionStrategy))
        {
            configDictionary["instructionStrategy"] = agentFlowConfig.InstructionStrategy;
        }

        if (!string.IsNullOrEmpty(agentFlowConfig?.Model))
        {
            configDictionary["model"] = agentFlowConfig.Model;
        }

        InstructionStrategy instructionStrategy = string.IsNullOrEmpty(agentFlowConfig?.InstructionStrategy) switch
        {
            true => this.configuration.InstructionStrategy,
            false => InstructionStrategyParser.TryParse(agentFlowConfig.InstructionStrategy, out var strategy)
                ? strategy : throw new InvalidOperationException("Invalid instruction strategy"),
        };

        ToolOutputStrategy toolOutputStrategy = string.IsNullOrEmpty(agentFlowConfig?.ToolOutputStrategy) switch
        {
            true => this.configuration.ToolOutputStrategy,
            false => ToolOutputStrategyParser.TryParse(agentFlowConfig.ToolOutputStrategy, out var strategy)
                ? strategy : throw new InvalidOperationException("Invalid instruction strategy"),
        };

        var program = new CellSequence<ConversationThread>(
            [
                new ApplyConfigurationCell(configDictionary),
                new AgentCell(
                    new ToolAgent(
                        new AgentName("WebSearchAgent"),
                        Role.Assistant,
                        this.promptFactoryProvider,
                        this.agentBuilderFactory,
                        instructionStrategy,
                        toolOutputStrategy,
                        tools)),
            ]);

        {
            this.logger.LogInformation("Running program (next log)");
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new PolymorphicJsonConverter<Cell<ConversationThread>>(),
                },
            };

            string serialized = JsonSerializer.Serialize(program, options);
            this.logger.LogInformation("{Program}", serialized);
        }

        return program;

        // return new AgentCell(
        //     new ToolAgent(
        //         new AgentName("WebSearchAgent"),
        //         Role.Assistant,
        //         this.promptFactoryProvider,
        //         this.agentBuilderFactory,
        //         this.configuration.InstructionStrategy,
        //         tools));
    }
}

internal sealed record ChatCompletionRequest(
    [property: JsonPropertyName("agentFlowConfig")] AgentFlowRequestConfig? AgentFlowConfig,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("conversationId")] string? ConversationId,
    [property: JsonPropertyName("messages")] ImmutableArray<Message> Messages);

internal sealed record AgentFlowRequestConfig(
    [property: JsonPropertyName("instructionStrategy")] string? InstructionStrategy,
    [property: JsonPropertyName("toolOutputStrategy")] string? ToolOutputStrategy,
    [property: JsonPropertyName("model")] string? Model);

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
