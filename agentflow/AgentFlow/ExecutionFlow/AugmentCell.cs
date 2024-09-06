using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.ExecutionFlow;

/// <summary>
/// The 'augment' of Retrieval Augmented Generation.
/// Inserts (augments) information into the conversation
/// to be used by the bot to respond.
/// </summary>
public class AugmentCell : Cell<ConversationThread>
{
    private readonly ILogger<AugmentCell> logger;

    public AugmentCell(ILogger<AugmentCell> logger)
    {
        this.logger = logger;
    }

    public override Task<ConversationThread> RunAsync(ConversationThread input)
    {
        // end of day thought-
        // the cell type for everything should be something like Cell<ConversationContext>
        // where ConversationContext 'has a' ConversationThread (or else just update ConversationThread)
        // and also a Prompt which has a base template plus variables
        // and AugmentCells can set the variables which will render in a final stage later on
        // An Agent's Instructions can be the base prompt.
        // or-
        // IAgent has a Prompt Prompt { get; }
        // and Prompt has a string template with {{ variables }}
        // and those are what ultimately get replaced by whatever happens
        // update - doesn't matter what agent does, conversationthread only knows messages
        // update - wtf is the difference in AgentCell and GetAssistantResponseCell?
        // answer: CustomAgent uses GetAssistantResponseCell, CustomAgent *is a* IAgent, IAgent is used by AgentCell
        return Task.FromResult(input);
    }
}
