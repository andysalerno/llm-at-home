using System.Text.Json;
using AgentFlow.LlmClient;

namespace AgentFlow;

public interface ILlmCompletionsClient
{
    Task<CompletionsResult> GetCompletionsAsync(CompletionsRequest input);

    Task<ChatCompletionsResult> GetChatCompletionsAsync(ChatCompletionsRequest input);
}

public record CompletionsResult(string Text);

public record CompletionsRequest(string Text);

public record ChatCompletionsRequest(
    IEnumerable<Message> Messages,
    IEnumerable<string>? Stop = null,
    JsonElement? JsonSchema = null,
    string? PromptTemplate = null);

public record ChatCompletionsResult(string Text);
