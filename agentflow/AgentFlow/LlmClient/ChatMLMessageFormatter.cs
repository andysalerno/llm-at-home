using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AgentFlow.LlmClient;

public class ChatMLMessageFormatter : IMessageFormatter
{
    private readonly ILogger<ChatMLMessageFormatter> logger;

    public ChatMLMessageFormatter(ILogger<ChatMLMessageFormatter> logger)
    {
        this.logger = logger;
    }

    public string FormatMessages(ImmutableArray<Message> messages, bool addGenerationPrompt = true)
    {
        const string User = "user";
        const string Assistant = "assistant";
        const string System = "system";

        var builder = new StringBuilder();

        foreach (var message in messages)
        {
            string content = message.Content.Trim();

            string role;
            if (message.Role == Role.User)
            {
                role = User;
            }
            else if (message.Role == Role.Assistant)
            {
                role = Assistant;
            }
            else if (message.Role == Role.System)
            {
                role = System;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(messages), $"Unknown role: {message.Role.Name}");
            }

            builder.Append($"<|im_start|>{role}\n{content}<|im_end|>\n");
        }

        if (messages.Last().Role == Role.Assistant)
        {
            if (addGenerationPrompt)
            {
                this.logger.LogWarning(
                    "addGenerationPrompt is true, but the last message is already from the assistant, so ignoring addGenerationPrompt");
            }

            addGenerationPrompt = false;

            string result = builder.ToString();

            const string ImEnd = "<|im_end|>\n";
            if (result.EndsWith(ImEnd))
            {
                int finalLength = result.Length - ImEnd.Length;
                return result.Substring(0, finalLength);
            }
        }

        if (addGenerationPrompt)
        {
            builder.Append("<|im_start|>assistant\n");
        }

        return builder.ToString();
    }
}
