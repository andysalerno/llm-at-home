using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

internal class GetUserInputCell : Cell<ConversationThread>
{
    private readonly AgentName name;
    private readonly Role role;
    private readonly ILogger<GetUserInputCell> logger;

    public GetUserInputCell(AgentName name, Role role)
    {
        this.name = name;
        this.role = role;
        this.logger = this.GetLogger();
    }

    public override Cell<ConversationThread>? GetNext(ConversationThread input)
    {
        throw new NotImplementedException("Use V2");
    }

    public override Task<ConversationThread> RunAsync(ConversationThread input)
    {
        Console.Write("User: ");
        string userInput = Console.ReadLine()?.Trim() ?? throw new InvalidOperationException("Could not read input from console.");

        var result = input.WithAddedMessage(new Message(this.name, this.role, userInput));

        return Task.FromResult(result);
    }
}
