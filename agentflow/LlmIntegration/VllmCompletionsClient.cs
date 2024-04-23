using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFlow.Config;
using AgentFlow.LlmClient;
using Microsoft.Extensions.Logging;

namespace AgentFlow.LlmClients.Vllm;

internal record VllmCompletionRequest(
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

internal record VllmChatCompletionRequest(
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
    float? MinP = null);

internal record Choice(
    int Index,
    string Text,
    string FinishReason);

internal record VllmCompletionResponse(
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

internal record VllmChatCompletionResponse(
    string @Object,
    string Model,
    ImmutableArray<ChatMessageChoice> Choices);

/// <summary>
/// A class that interacts with the VLLM completions endpoint.
/// Note that since this uses the completions endpoint, and NOT the chat endpoint,
/// it does not understand the concept of chat messages, which must be parsed by the caller.
/// </summary>
[Obsolete("Use OpenAICompletionsClient")]
public sealed class VllmCompletionsClient : ILlmCompletionsClient, IDisposable
{
    private const int MaxTokensToGenerate = 512;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly Uri endpoint;
    private readonly HttpClient httpClient;
    private readonly IMessageFormatter messageFormatter;
    private readonly ILoggingConfig loggingConfig;
    private readonly ILogger<VllmCompletionsClient> logger;

    public VllmCompletionsClient(
        ICompletionsEndpointConfig completionsEndpointProvider,
        IMessageFormatter messageFormatter,
        IHttpClientFactory httpClientFactory,
        ILoggingConfig loggingConfig,
        ILogger<VllmCompletionsClient> logger)
    {
        this.endpoint = completionsEndpointProvider.CompletionsEndpoint;
        this.httpClient = httpClientFactory.CreateClient();
        this.messageFormatter = messageFormatter;
        this.loggingConfig = loggingConfig;
        this.logger = logger;
    }

    public async Task<CompletionsResult> GetCompletionsAsync(CompletionsRequest input)
    {
        var request = new VllmCompletionRequest(
            Model: "model",
            Temperature: 0.0f,
            MaxTokens: MaxTokensToGenerate,
            Prompt: input.Text);

        using var requestContent = JsonContent.Create(request, options: JsonSerializerOptions);

        var uri = new Uri(this.endpoint, "/v1/completions");

        var result = await this.httpClient.PostAsync(uri, requestContent);

        var resultJson = await result.Content.ReadAsStringAsync();

        if (this.loggingConfig.LogRequestsToLlm)
        {
            this.logger.LogInformation("Received: {received}", resultJson);
        }

        VllmCompletionResponse parsedResponse = JsonSerializer.Deserialize<VllmCompletionResponse>(resultJson, JsonSerializerOptions)
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

        var request = new VllmChatCompletionRequest(
            Model: "model",
            Temperature: 0.0f,
            MaxTokens: MaxTokensToGenerate,
            Messages: messages);

        using var requestContent = JsonContent.Create(request, options: JsonSerializerOptions);

        var uri = new Uri(this.endpoint, "/v1/chat/completions");

        var result = await this.httpClient.PostAsync(uri, requestContent);

        var resultJson = await result.Content.ReadAsStringAsync();

        this.logger.LogInformation("Received: {received}", resultJson);
        if (this.loggingConfig.LogRequestsToLlm)
        {
            this.logger.LogInformation("Received: {received}", resultJson);
        }

        VllmChatCompletionResponse parsedResponse = JsonSerializer.Deserialize<VllmChatCompletionResponse>(resultJson, JsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to parse response");

        if (this.loggingConfig.LogRequestsToLlm)
        {
            this.logger.LogInformation("Parsed response: {response}", parsedResponse);
        }

        return new ChatCompletionsResult(parsedResponse.Choices.First().Message.Content);
    }

    public void Dispose()
    {
        this.httpClient.Dispose();
    }
}
