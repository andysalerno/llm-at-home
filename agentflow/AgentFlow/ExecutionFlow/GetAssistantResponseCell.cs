using System.Collections.Immutable;
using System.Text.Json;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

public class GetAssistantResponseCell : Cell<ConversationThread>
{
    private readonly ILogger<GetAssistantResponseCell> logger;
    private readonly AgentName agentName;
    private readonly Role agentRole;
    private readonly JsonElement? responseSchema;
    private readonly ILlmCompletionsClient completionsClient;

    public GetAssistantResponseCell(AgentName agentName, Role agentRole, ILlmCompletionsClient completionsClient)
        : this(agentName, agentRole, null, completionsClient)
    {
    }

    public GetAssistantResponseCell(AgentName agentName, Role agentRole, JsonElement? responseSchema, ILlmCompletionsClient completionsClient)
    {
        this.agentName = agentName;
        this.agentRole = agentRole;
        this.responseSchema = responseSchema;
        this.completionsClient = completionsClient;
        this.logger = this.GetLogger();
    }

    public override Cell<ConversationThread>? GetNext(ConversationThread input)
    {
        return null;
    }

    public override async Task<ConversationThread> RunAsync(ConversationThread input)
    {
        if (input.Messages.Any(m => m.Role == Role.System))
        {
            this.logger.LogWarning("Workspace message context already contained a system message, which is unexpected.");
        }

        var messages = input.Messages.ToImmutableArray();

        if (messages.LastOrDefault() is Message lastMessage && lastMessage.Role == Role.Assistant)
        {
            this.logger.LogWarning("Last message was from assistant already. Some LLMs may not work well in this scenario.");
        }

        ConversationThread templateFilled = input
            .WithTemplateAppliedToSystem()
            .WithMessagesVisibleToAssistant();

        var response = await this.completionsClient.GetChatCompletionsAsync(
            new ChatCompletionsRequest(templateFilled.Messages, JsonSchema: this.responseSchema));

        // Don't return the template filled version - we only ever want the template filled for sending to the LLM
        return input.WithAddedMessage(new Message(this.agentName, this.agentRole, response.Text));
    }

    private static string ChatTemplateForRole_Qwen(Role role) =>
        "{% for message in messages %}{% if loop.first and messages[0]['role'] != 'system' %}{{ '<|im_start|>system\nYou are a helpful assistant<|im_end|>\n' }}{% endif %}{{'<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n'}}{% endfor %}{% if add_generation_prompt %}{{ '<|im_start|>" + role.Name + "\n' }}{% endif %}";

    private static string ChatTemplateForRole_Llama3(Role role) =>
        "{% set loop_messages = messages %}{% for message in loop_messages %}{% set content = '<|start_header_id|>' + message['role'] + '<|end_header_id|>\n\n'+ message['content'] | trim + '<|eot_id|>' %}{% if loop.index0 == 0 %}{% set content = bos_token + content %}{% endif %}{{ content }}{% endfor %}{{ '<|start_header_id|>" + role.Name + "<|end_header_id|>\n\n' }}";
}
