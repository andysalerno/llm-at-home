using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;

namespace AgentFlow.Agents;

public sealed class ToolAgent : IAgent
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
          ToolOutputStrategy.AppendedToUserMessage,
          tools)
  {
  }

  public ToolAgent(
      AgentName name,
      Role role,
      IFactoryProvider<Prompt, PromptName> promptFactoryProvider,
      CustomAgentBuilderFactory customAgentBuilderFactory,
      InstructionStrategy instructionStrategy,
      ToolOutputStrategy toolOutputStrategy,
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
          this.promptFactoryProvider.GetFactory(DefaultPrompts.WebsearchExampleSystem).Create())
        .WithMessageVisibility(new MessageVisibility(ShownToUser: false, ShownToModel: true))
        .WithJsonResponseSchema(JsonToolSchema)
        .WithToolChoice("invoke_function")
        .WithInstructionsStrategy(instructionStrategy)
        .Build());

    this.responseAgent = new Lazy<IAgent>(() => this.customAgentBuilderFactory
       .CreateBuilder()
       .WithName(new AgentName("ResponseAgent"))
       .WithRole(this.Role)
       .WithToolOutputStrategy(toolOutputStrategy)
       .WithInstructionsFromPrompt(
          this.promptFactoryProvider.GetFactory(DefaultPrompts.WebsearchExampleResponding).Create())
       .SetVariableValue(key: "CUR_DATE", DateTime.Today.ToString("MMM dd, yyyy", DateTimeFormatInfo.InvariantInfo))
       .Build());
  }

  public AgentName Name { get; }

  public Role Role { get; }

  /// <summary>
  /// Gets a VLLM friendly schema.
  /// </summary>
  private static JsonObject JsonToolSchema { get; }
    = JsonSerializer.Deserialize<JsonObject>(
        """
        {
            "type": "object",
            "properties": {
                "last_user_message_intent": { "type": "string" },
                "function_name": { "type": "string" },
                "invocation": { "type": "string" }
            },
            "required": [
                "last_user_message_intent",
                "function_name",
                "invocation"
            ],
            "additionalProperties": false
        }
        """.Trim())
        ?? throw new InvalidOperationException("Could not deserialize the default JsonToolSchema");

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
