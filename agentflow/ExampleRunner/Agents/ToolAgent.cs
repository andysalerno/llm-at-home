using System.Collections.Immutable;
using System.Text.Json;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;

namespace AgentFlow.Examples.Agents;

// TODO: move out of examples and into AgentFlow lib
public class ToolAgent : IAgent
{
    private readonly CustomAgentBuilderFactory customAgentBuilderFactory;
    private readonly ImmutableArray<ITool> tools;
    private readonly Lazy<IAgent> toolSelectionAgent;
    private readonly Lazy<IAgent> responseAgent;

    public ToolAgent(
        AgentName name,
        Role role,
        Prompt toolSelectionPrompt,
        Prompt respondingPrompt,
        CustomAgentBuilderFactory customAgentBuilderFactory,
        ImmutableArray<ITool> tools)
    {
        this.Name = name;
        this.Role = role;
        this.ToolSelectionPrompt = toolSelectionPrompt;
        this.RespondingPrompt = respondingPrompt;
        this.customAgentBuilderFactory = customAgentBuilderFactory;
        this.tools = tools;

        this.toolSelectionAgent = new Lazy<IAgent>(() => this.customAgentBuilderFactory
            .CreateBuilder()
            .WithName(new AgentName("ToolSelectorAgent"))
            .WithRole(Role.ToolInvocation)
            .WithInstructionsFromPrompt(this.ToolSelectionPrompt)
            .WithMessageVisibility(new MessageVisibility(ShownToUser: false, ShownToModel: true))
            .WithJsonResponseSchema(JsonToolSchema)
            .Build());

        this.responseAgent = new Lazy<IAgent>(() => this.customAgentBuilderFactory
           .CreateBuilder()
           .WithName(new AgentName("ResponseAgent"))
           .WithRole(this.Role)
           .WithInstructionsFromPrompt(this.RespondingPrompt)
           .Build());
    }

    public AgentName Name { get; }

    public Role Role { get; }

    public Prompt ToolSelectionPrompt { get; }

    public Prompt RespondingPrompt { get; }

    private static JsonElement JsonToolSchema { get; }
      = JsonSerializer.Deserialize<JsonElement>(
  """
{
    "title": "AnswerFormat",
    "type": "object",
    "properties": {
      "last_user_message_intent": {
        "type": "string"
      },
      "function_name": {
        "type": "string"
      },
      "invocation": {
        "type": "string"
      }
    },
    "required": [
      "last_user_message_intent",
      "function_name",
      "invocation"
    ]
}
""".Trim());

    public Task<Cell<ConversationThread>> GetNextThreadStateAsync()
    {
        IAgent toolSelectionAgent = this.toolSelectionAgent.Value;
        IAgent responseAgent = this.responseAgent.Value;

        string toolsDefinitions = BuildToolsDefinitions(this.tools);

        // TODO: add CellSequence<T>.BeginSequence().Then(...).Then(...).Then(...).Build();
        Cell<ConversationThread> setupSequence = new CellSequence<ConversationThread>(
            sequence:
            [

                // Set the template values for the system message:
                new SetTemplateValueCell("tools", toolsDefinitions),

                // Get the response from the tool selection agent:
                new AgentCell(toolSelectionAgent),

                // Execute the selected tool and add its result to the conversation:
                new ExecuteToolCell(this.tools),

                // Get the ultimate response from the responding agent:
                new AgentCell(responseAgent),
            ]);

        return Task.FromResult(setupSequence);
    }

    private static string BuildToolsDefinitions(IEnumerable<ITool> tools)
    {
        const string Sep = "\n\n";
        return string.Join(Sep, tools.Select(t => t.Definition));
    }
}
