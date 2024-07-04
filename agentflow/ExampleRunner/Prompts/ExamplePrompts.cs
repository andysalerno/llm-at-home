using AgentFlow.Prompts;

namespace AgentFlow.Examples;

public static class ExamplePrompts
{
    public static PromptName CodeAssistantSystem { get; } = new PromptName("example_assistant_system");

    public static PromptName CriticSelectorSystem { get; } = new PromptName("critic_selector_system");

    public static PromptName ProgrammerSystem { get; } = new PromptName("programmer_system");

    public static PromptName PythonProgrammerSystem { get; } = new PromptName("python_programmer_system");

    public static PromptName RewriteQuerySystem { get; } = new PromptName("rewrite_query_system");

    public static PromptName WebsearchExampleResponding { get; } = new PromptName("websearch_example_responding");

    public static PromptName WebsearchExampleSystem { get; } = new PromptName("websearch_example_system");
}
