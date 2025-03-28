﻿using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AgentFlow.Agents.Extensions;
using AgentFlow.Config;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.Utilities;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.LlmClients;

internal record OpenAICompletionRequest(
    string Model,
    string Prompt,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? MaxTokens = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? Temperature = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? TopP = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? Stream = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ImmutableArray<string>? Stop = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? FrequencyPenalty = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? TopK = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? RepetitionPenalty = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? MinP = null);

internal record OpenAIChatCompletionRequest(
    string Model,

    ImmutableArray<IReadOnlyDictionary<string, string>> Messages,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? MaxTokens = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? Temperature = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? TopP = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? Stream = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ImmutableArray<string>? Stop = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? FrequencyPenalty = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? TopK = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? RepetitionPenalty = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? MinP = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? PromptTemplate = null,

    // [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    // JsonElement? JsonObject = null);

    // [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    // ImmutableArray<Tools>? Tools = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ImmutableArray<JsonElement>? Tools = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? ToolChoice = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    JsonObject? GuidedJson = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    JsonObject? ResponseFormat = null);

internal sealed record ResponseFormat(JsonElement Value, string Type = "json");

internal sealed record Tools(Function Function, string Type = "function");

internal sealed record Function(string Name, string Description);

internal sealed record Parameters(JsonElement Properties)
{
    public string Type { get; } = "object";
}

internal sealed record Properties();

internal record Choice(
    int Index,
    string Text,
    string FinishReason);

internal record OpenAICompletionResponse(
    string @Object,
    string Model,
    ImmutableArray<Choice> Choices);

internal record ChatMessage(
    string Role,
    string Content);

internal record ChatMessageChoice(
    int Index,
    ChatMessage Message,
    string FinishReason);
// string? StopReason);

internal record OpenAIChatCompletionResponse(
    string @Object,
    string Model,
    ImmutableArray<ChatMessageChoice> Choices);

public sealed class OpenAICompletionsClient : ILlmCompletionsClient, IEmbeddingsClient, IScraperClient, IDisposable
{
    private const int MaxTokensToGenerate = 512;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly Uri baseEndpoint;
    private readonly Uri completionsEndpoint;
    private readonly Uri chatCompletionsEndpoint;
    private readonly Uri embeddingsEndpoint;
    private readonly Uri scraperEndpoint;
    private readonly string modelName;
    private readonly Lazy<HttpClient> httpClient;
    private readonly IEnvironmentVariableProvider environmentVariableProvider;
    private readonly ILoggingConfig loggingConfig;
    private readonly ILogger<OpenAICompletionsClient> logger;
    private readonly ChatRequestDiskLogger? chatRequestDiskLogger;
    private readonly IConversationPersistenceWriter? conversationPersistenceWriter;

    public OpenAICompletionsClient(
        ICompletionsEndpointConfig completionsEndpointProviderConfig,
        IEnvironmentVariableProvider environmentVariableProvider,
        IHttpClientFactory httpClientFactory,
        ILoggingConfig loggingConfig,
        ILogger<OpenAICompletionsClient> logger,
        ChatRequestDiskLogger? chatRequestDiskLogger = null,
        IConversationPersistenceWriter? conversationPersistenceWriter = null)
    {
        this.baseEndpoint = completionsEndpointProviderConfig.CompletionsEndpoint;
        this.httpClient = new Lazy<HttpClient>(() => httpClientFactory.CreateClient<OpenAICompletionsClient>());
        this.environmentVariableProvider = environmentVariableProvider;
        this.loggingConfig = loggingConfig;
        this.logger = logger;
        this.chatRequestDiskLogger = chatRequestDiskLogger;
        this.conversationPersistenceWriter = conversationPersistenceWriter;
        this.embeddingsEndpoint = completionsEndpointProviderConfig.EmbeddingsEndpoint;
        this.scraperEndpoint = completionsEndpointProviderConfig.ScraperEndpoint;

        this.modelName = completionsEndpointProviderConfig.ModelName;

        this.chatCompletionsEndpoint = CombineUriFragments(this.baseEndpoint.AbsoluteUri, "/v1/chat/completions");
        this.completionsEndpoint = CombineUriFragments(this.baseEndpoint.AbsoluteUri, "/v1/completions");

        string? bearerToken = this.environmentVariableProvider.GetVariableValue("TOKEN");

        if (bearerToken != null)
        {
            this.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    private HttpClient HttpClient => this.httpClient.Value;

    public async Task<CompletionsResult> GetCompletionsAsync(CompletionsRequest input)
    {
        var request = new OpenAICompletionRequest(
            Model: this.modelName,
            Temperature: 0.0f,
            MaxTokens: MaxTokensToGenerate,
            Prompt: input.Text);

        using var requestContent = JsonContent.Create(request, options: JsonSerializerOptions);

        var result = await this.HttpClient.PostAsync(this.completionsEndpoint, requestContent);

        var resultJson = await result.Content.ReadAsStringAsync();

        if (this.loggingConfig.LogRequestsToLlm)
        {
            this.logger.LogInformation("Received: {received}", resultJson);
        }

        OpenAICompletionResponse parsedResponse = JsonSerializer.Deserialize<OpenAICompletionResponse>(resultJson, JsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to parse response");

        if (this.loggingConfig.LogRequestsToLlm)
        {
            this.logger.LogInformation("Parsed response: {response}", parsedResponse);
        }

        return new CompletionsResult(Text: parsedResponse.Choices.First().Text);
    }

    public async Task<ChatCompletionsResult> GetChatCompletionsAsync(ChatCompletionsRequest input)
    {
        var messages = input.Messages
            .Select(m => new Dictionary<string, string> { ["role"] = m.Role.Name, ["content"] = m.Content })
            .Cast<IReadOnlyDictionary<string, string>>()
            .ToImmutableArray();

        ImmutableArray<JsonObject>? tools = input.JsonSchema is null ? null : new List<JsonObject> { input.JsonSchema }.ToImmutableArray();

        JsonObject? schemaWithHeader = null;

        if (input.JsonSchema != null)
        {
            schemaWithHeader = CreateJsonSchemaObject(input.JsonSchema);
        }

        string model;
        if (input.Model != null)
        {
            model = input.Model;
            this.logger.LogInformation("Client provided model: {Model}", model);
        }
        else
        {
            model = this.modelName;
        }

        var request = new OpenAIChatCompletionRequest(
            Model: model,
            Temperature: 0.00f,
            MaxTokens: MaxTokensToGenerate,
            ResponseFormat: schemaWithHeader,
            // GuidedJson: input.JsonSchema,
            // ResponseFormat: input.JsonSchema is not null ? new ResponseFormat(input.JsonSchema.Value) : null,
            // Tools: input.JsonSchema != null ? [input.JsonSchema] : null,
            // RepetitionPenalty: 1.2f,
            // Tools: tools,
            // ToolChoice: input.ToolChoice,
            PromptTemplate: input.PromptTemplate,
            Stop: input.Stop?.ToImmutableArray(),
            Messages: messages);

        using var requestContent = JsonContent.Create(request, options: JsonSerializerOptions);

        if (this.loggingConfig.LogRequestsToLlm)
        {
            string json = await requestContent.ReadAsStringAsync();
            this.logger.LogInformation("Sending request: {Received}", json);
        }

        var result = await this.HttpClient.PostAsync(this.chatCompletionsEndpoint, requestContent);

        result.EnsureSuccessStatusCode();

        var resultJson = await result.Content.ReadAsStringAsync();

        if (this.loggingConfig.LogRequestsToLlm)
        {
            this.logger.LogInformation("Received: {received}", resultJson);
        }

        if (!result.IsSuccessStatusCode)
        {
            this.logger.LogError("Request failed with status code: {StatusCode}", result.StatusCode);
        }

        OpenAIChatCompletionResponse parsedResponse = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(resultJson, JsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to parse response");

        if (this.loggingConfig.LogRequestsToLlm)
        {
            this.logger.LogInformation("Parsed response: {response}", parsedResponse.Choices.First().Message.Content);
        }

        string preTrim = parsedResponse.Choices.First().Message.Content;
        string trimmed = preTrim.Trim();

        if (preTrim.Length != trimmed.Length)
        {
            this.logger.LogWarning("The response from the LLM had whitespace in the prefix or suffix that was trimmed.");
        }

        if (this.chatRequestDiskLogger is ChatRequestDiskLogger chatRequestDiskLogger)
        {
            // await chatRequestDiskLogger.LogRequestToDiskAsync(input.Messages.Concat(
            //     new[]
            //     {
            //         new Message(new Agents.AgentName("unused"), Role.Assistant, trimmed),
            //     }));
            this.logger.LogWarning("Skipping logging to disk.");
        }

        if (this.conversationPersistenceWriter is IConversationPersistenceWriter conversationPersistenceWriter)
        {
            if (ActivityUtilities.TryGetConversationIdFromCurrentActivity(out ConversationId? conversationId))
            {
                if (ActivityUtilities.TryGetIncomingRequestIdFromCurrentActivity(out IncomingRequestId? incomingRequestId))
                {
                    this.logger.LogInformation("Persisting request during ConversationId: {ConversationId} and IncomingRequestId: {IncomingRequestId}", conversationId, incomingRequestId);
                    var requestToStore = new StoredLlmRequest(
                        Input: input.Messages.Select(m => new StoredMessage(m.Role.Name, m.Content, incomingRequestId)).ToImmutableArray(),
                        Output: new StoredMessage(Role.Assistant.Name, trimmed, incomingRequestId),
                        incomingRequestId);

                    await conversationPersistenceWriter.StoreLlmRequestAsync(
                        conversationId,
                        incomingRequestId,
                        requestToStore);
                }
                else
                {
                    this.logger.LogWarning("Failed to get IncomingRequestId from current activity.");
                }
            }
            else
            {
                this.logger.LogWarning("Failed to get ConversationId from current activity.");
            }
        }
        else
        {
            this.logger.LogWarning("Skipping persistence of request.");
        }

        return new ChatCompletionsResult(trimmed);
    }

    public async Task<EmbeddingResponse> GetEmbeddingsAsync(string query, IEnumerable<Chunk> chunks)
    {
        var passages = chunks.Select(c => c.Content);
        var requestBody = new EmbeddingsRequest(passages.ToImmutableArray(), query);

        var content = JsonContent.Create(requestBody, options: JsonSerializerOptions);
        await content.LoadIntoBufferAsync();

        HttpResponseMessage response = await this.HttpClient.PostAsync(this.embeddingsEndpoint, content);
        response.EnsureSuccessStatusCode();

        EmbeddingResponse embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(JsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to parse embedding json response as EmbeddingResponse");

        return embeddingResponse;
    }

    public async Task<ScoresResponse> GetScoresAsync(string query, IEnumerable<Chunk> chunks)
    {
        var passages = chunks.Select(c => c.Content).ToImmutableArray();
        var requestBody = new EmbeddingsRequest(passages, query);

        var content = JsonContent.Create(requestBody, options: JsonSerializerOptions);
        await content.LoadIntoBufferAsync();

        var scoresParts = string.Join("/", this.embeddingsEndpoint.AbsoluteUri.Split("/").SkipLast(1));
        scoresParts = scoresParts + "/scores";

        HttpResponseMessage response = await this.HttpClient.PostAsync(new Uri(scoresParts), content);
        response.EnsureSuccessStatusCode();

        ScoresResponse scoresResponse = await response.Content.ReadFromJsonAsync<ScoresResponse>(JsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to parse embedding json response as EmbeddingResponse");

        return scoresResponse;
    }

    public async Task<ScrapeResponse> GetScrapedSiteContentAsync(IEnumerable<Uri> uris)
    {
        var requestBody = new ScrapeRequest(uris.ToImmutableArray());

        var content = JsonContent.Create(requestBody, options: JsonSerializerOptions);
        await content.LoadIntoBufferAsync();

        HttpResponseMessage response = await this.HttpClient.PostAsync(this.scraperEndpoint, content);
        response.EnsureSuccessStatusCode();

        ScrapeResponse scrapeResponse = await response.Content.ReadFromJsonAsync<ScrapeResponse>(JsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to parse scrape json response as ScrapeResponse");

        return scrapeResponse;
    }

    public void Dispose()
    {
        this.HttpClient.Dispose();
    }

    private static JsonObject CreateJsonSchemaObject(JsonObject innerSchema)
    {
        JsonObject header = JsonSerializer.Deserialize<JsonObject>(
            """
            {
                "type": "json_schema",
                "json_schema": {
                    "name": "",
                    "strict": true
                }
            }
            """.Trim())
            ?? throw new InvalidCastException("Could not deserialize the json chema template.");

        var schemaProp = header["json_schema"] ?? throw new InvalidOperationException("Expected to find the json_schema");
        schemaProp["schema"] = innerSchema.DeepClone();

        return header;
    }

    private static Uri CombineUriFragments(string @base, string path)
    {
        @base = @base.TrimEnd('/');
        path = @path.TrimStart('/');

        return new Uri($"{@base}/{path}");
    }

    private record EmbeddingsRequest(
        ImmutableArray<string> Input,
        string? Query = null);

    private record ScrapeRequest(ImmutableArray<Uri> Uris);
}
