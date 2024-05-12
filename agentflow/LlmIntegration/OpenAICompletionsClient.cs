using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFlow.Config;
using AgentFlow.LlmClient;
using Microsoft.Extensions.Logging;

namespace AgentFlow.LlmClients.OpenAI;

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

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    JsonElement? JsonSchema = null);

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
    string FinishReason,
    string? StopReason);

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
    private readonly HttpClient httpClient;
    private readonly ILoggingConfig loggingConfig;
    private readonly ILogger<OpenAICompletionsClient> logger;

    public OpenAICompletionsClient(
        ICompletionsEndpointConfig completionsEndpointProviderConfig,
        IHttpClientFactory httpClientFactory,
        ILoggingConfig loggingConfig,
        ILogger<OpenAICompletionsClient> logger)
    {
        this.baseEndpoint = completionsEndpointProviderConfig.CompletionsEndpoint;
        this.httpClient = httpClientFactory.CreateClient();
        this.loggingConfig = loggingConfig;
        this.logger = logger;

        this.embeddingsEndpoint = completionsEndpointProviderConfig.EmbeddingsEndpoint;
        this.scraperEndpoint = completionsEndpointProviderConfig.ScraperEndpoint;

        this.modelName = completionsEndpointProviderConfig.ModelName;

        this.chatCompletionsEndpoint = CombineUriFragments(this.baseEndpoint.AbsoluteUri, "/v1/chat/completions");
        this.completionsEndpoint = CombineUriFragments(this.baseEndpoint.AbsoluteUri, "/v1/completions");

        logger.LogInformation("chat completions endpoint: {Endpoint}", this.chatCompletionsEndpoint);

        string? bearerToken = Environment.GetEnvironmentVariable("TOKEN");

        if (bearerToken != null)
        {
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    public async Task<CompletionsResult> GetCompletionsAsync(CompletionsRequest input)
    {
        var request = new OpenAICompletionRequest(
            Model: this.modelName,
            Temperature: 0.0f,
            MaxTokens: MaxTokensToGenerate,
            Prompt: input.Text);

        using var requestContent = JsonContent.Create(request, options: JsonSerializerOptions);

        var result = await this.httpClient.PostAsync(this.completionsEndpoint, requestContent);

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

        var request = new OpenAIChatCompletionRequest(
            Model: this.modelName,
            Temperature: 0.01f,
            MaxTokens: MaxTokensToGenerate,
            JsonSchema: input.JsonSchema,
            PromptTemplate: input.PromptTemplate,
            Stop: input.Stop?.ToImmutableArray(),
            Messages: messages);

        using var requestContent = JsonContent.Create(request, options: JsonSerializerOptions);

        if (this.loggingConfig.LogRequestsToLlm)
        {
            string json = await requestContent.ReadAsStringAsync();
            this.logger.LogInformation("Sending request: {Received}", json);
        }

        var result = await this.httpClient.PostAsync(this.chatCompletionsEndpoint, requestContent);

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
            this.logger.LogInformation("Parsed response: {response}", parsedResponse);
        }

        return new ChatCompletionsResult(parsedResponse.Choices.First().Message.Content);
    }

    public async Task<EmbeddingResponse> GetEmbeddingsAsync(string query, IEnumerable<Chunk> chunks)
    {
        var passages = chunks.Select(c => c.Content);
        var requestBody = new EmbeddingsRequest(passages.ToImmutableArray(), query);

        var content = JsonContent.Create(requestBody, options: JsonSerializerOptions);
        await content.LoadIntoBufferAsync();

        HttpResponseMessage response = await this.httpClient.PostAsync(this.embeddingsEndpoint, content);
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

        HttpResponseMessage response = await this.httpClient.PostAsync(new Uri(scoresParts), content);
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

        HttpResponseMessage response = await this.httpClient.PostAsync(this.scraperEndpoint, content);
        response.EnsureSuccessStatusCode();

        ScrapeResponse scrapeResponse = await response.Content.ReadFromJsonAsync<ScrapeResponse>(JsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to parse scrape json response as ScrapeResponse");

        return scrapeResponse;
    }

    public void Dispose()
    {
        this.httpClient.Dispose();
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
