## To Do

- [ ] Make every agent representable as json, so they can be composed as json with no code
- [x] Separate lib from program runner
- [ ] All agents should be composable from the base parts, I should not need ProgrammerAgent.cs
- [ ] When two AI agents are conversing, they should each see themselves as "assistant" and the other as "user" 
- [ ] Resolve conflict between a `Prompt` having a dictionary of variables, and a `SetTemplateValueCell` - these concepts are competing

## System prompt strategy

Here's a problem: how should the system prompt be presented, such that the agent will follow it correctly?

Possible strategies:
1. System prompt is provided as-is, with agent instructions, then the conversation is shown, and hopefully the agent follows the system prompt instead of simply continuing the conversation
1. System prompt is provided as the last message in the conversation - defying ChatML convention but possibly working better due to proximity of the context?
1. The agent prompt is presented as a suffix to the user prompt
1. OR, following the previous choice, the agent prompt IS the user message, if we know that the most recent message in the conversation was from the assistant (and therefore it's the user's turn anyway)
1. Same as 1, but we also provide a prefix for the next assistant message like: "Following the system prompt, I will now select the correct X to handle Y:"


## Thinking scratchpad

How to generalize so there is no logic like "if code_on_message then execute_code()" etc etc?

How can it be fully composable where you define agents, define what they can do, and just by composing, the runtime handles the rest?

Idea: use the Evangelion Magi as a demo :)

- consider making the entire 'select an action' section live within a single system->user->assistant turn, and the full conversation will be represented inside the user's turn where they say: read this conversation and tell me what you want to do next..."

## Architecture

### ExecutionFlow
**ExecutionFlow** is a generic framework for input/output cells that can be composed together into a program.

### AgentFlow
**AgentFlow** is built using the ExecutionFlow library. It operations on cell type `Cell<ConversationThread>`.

AgentFlow adds some implementations of `Cell<ConversationThread>` that are useful for building **Agent**s.

### Agent
An **Agent** is something that returns the cell program that should be executed to apply its output to the conversation.