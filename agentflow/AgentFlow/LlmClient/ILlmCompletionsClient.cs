using System.Text.Json;
using System.Text.Json.Nodes;
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
    string? Model = null,
    IEnumerable<string>? Stop = null,
    JsonObject? JsonSchema = null,
    string? ToolChoice = null,
    string? PromptTemplate = null);

public record ChatCompletionsResult(string Text);
