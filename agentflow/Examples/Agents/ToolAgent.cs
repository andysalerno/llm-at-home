using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;

namespace AgentFlow.Examples.Agents;

// TODO: move out of examples and into AgentFlow lib
public class ToolAgent : IAgent
{
  private readonly IFactoryProvider<Prompt, PromptName> promptFactoryProvider;
  private readonly CustomAgentBuilderFactory customAgentBuilderFactory;
  private readonly ImmutableArray<ITool> tools;
  private readonly Lazy<IAgent> toolSelectionAgent;
  private readonly Lazy<IAgent> responseAgent;

  public ToolAgent(
      AgentName name,
      Role role,
      IFactoryProvider<Prompt, PromptName> promptFactoryProvider,
      CustomAgentBuilderFactory customAgentBuilderFactory,
      ImmutableArray<ITool> tools)
      : this(
          name,
          role,
          promptFactoryProvider,
          customAgentBuilderFactory,
          InstructionStrategy.TopLevelSystemMessage,
          tools)
  {
  }

  public ToolAgent(
      AgentName name,
      Role role,
      IFactoryProvider<Prompt, PromptName> promptFactoryProvider,
      CustomAgentBuilderFactory customAgentBuilderFactory,
      InstructionStrategy instructionStrategy,
      ImmutableArray<ITool> tools)
  {
    this.Name = name;
    this.Role = role;
    this.promptFactoryProvider = promptFactoryProvider;
    this.customAgentBuilderFactory = customAgentBuilderFactory;
    this.tools = tools;

    this.toolSelectionAgent = new Lazy<IAgent>(() => this.customAgentBuilderFactory
        .CreateBuilder()
        .WithName(new AgentName("ToolSelectorAgent"))
        .WithRole(Role.ToolInvocation)
        .SetVariableValue(key: "tools", value: BuildToolsDefinitions(this.tools))
        .SetVariableValue(
            key: "CUR_DATE",
            DateTime.Today.ToString("MMM dd, yyyy", DateTimeFormatInfo.InvariantInfo))
        .WithInstructionsFromPrompt(
          this.promptFactoryProvider.GetFactory(ExamplePrompts.WebsearchExampleSystem).Create())
        .WithMessageVisibility(new MessageVisibility(ShownToUser: false, ShownToModel: true))
        .WithJsonResponseSchema(JsonToolSchema)
        .WithInstructionsStrategy(instructionStrategy)
        .Build());

    this.responseAgent = new Lazy<IAgent>(() => this.customAgentBuilderFactory
       .CreateBuilder()
       .WithName(new AgentName("ResponseAgent"))
       .WithRole(this.Role)
       .WithInstructionsFromPrompt(
          this.promptFactoryProvider.GetFactory(ExamplePrompts.WebsearchExampleResponding).Create())
       .SetVariableValue(key: "CUR_DATE", DateTime.Today.ToString("MMM dd, yyyy", DateTimeFormatInfo.InvariantInfo))
       .Build());
  }

  public AgentName Name { get; }

  public Role Role { get; }

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

  public Task<Cell<ConversationThread>> GetNextConversationStateAsync()
  {
    IAgent toolSelectionAgent = this.toolSelectionAgent.Value;
    IAgent responseAgent = this.responseAgent.Value;

    string toolsDefinitions = BuildToolsDefinitions(this.tools);

    // TODO: add CellSequence<T>.BeginSequence().Then(...).Then(...).Then(...).Build();
    Cell<ConversationThread> setupSequence = new CellSequence<ConversationThread>(
        sequence:
        [

            // Set the template values for the system message:
            // TODO: remove this
            // new SetTemplateValueCell("tools", toolsDefinitions),
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
