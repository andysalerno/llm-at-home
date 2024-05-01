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

  // TODO: take in CustomAgentBuilderFactory, or re-implement the custom agent logic here? Not sure
  public ToolAgent(AgentName name, Role role, Prompt prompt, CustomAgentBuilderFactory customAgentBuilderFactory, ImmutableArray<ITool> tools)
  {
    this.Name = name;
    this.Role = role;
    this.Prompt = prompt;
    this.customAgentBuilderFactory = customAgentBuilderFactory;
    this.tools = tools;
  }

  public AgentName Name { get; }

  public Role Role { get; }

  public Prompt Prompt { get; }

  // apply variables to prompt template
  // apply prompt template as system message
  // get response from assistant provider
  public Task<Cell<ConversationThread>> GetNextThreadStateAsync(ConversationThread conversationThread)
  {
    IAgent toolSelectionAgent = this.customAgentBuilderFactory
        .CreateBuilder()
        .WithName(new AgentName("ToolSelectorAgent"))
        .WithRole(Role.ToolInvocation)
        .WithInstructionsFromPrompt(this.Prompt)
        .WithMessageVisibility(new MessageVisibility(ShownToUser: false, ShownToModel: true))
        .WithJsonResponseSchema(JsonToolSchema)
        .Build();

    // TODO: this should be injected(?)
    IAgent responseAgent = this.customAgentBuilderFactory
        .CreateBuilder()
        .WithName(new AgentName("ResponseAgent"))
        .WithRole(this.Role)
        .WithInstructions("You are a helpful AI. Help the user as much as you can. You may use responses from tool_output as extra context when answering the user. Because your own knowledge may be inaccurate, DO NOT provide any facts or information unless it is given to you by a tool output.")
        .Build();

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
}
