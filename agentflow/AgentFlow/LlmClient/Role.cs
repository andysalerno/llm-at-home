using System.Collections.Immutable;

namespace AgentFlow.LlmClient;

public record Role
{
    public static readonly Role User = new Role("user");

    public static readonly Role Assistant = new Role("assistant");

    public static readonly Role System = new Role("system");

    // Possibly unsupported, depending on the model:
    public static readonly Role ToolInvocation = new Role("tool_invocation");

    // Possibly unsupported, depending on the model:
    public static readonly Role ToolOutput = new Role("tool_output");

    public static Role ExpectFromName(string name)
    {
        ImmutableArray<Role> roles = [
            User, Assistant, System, ToolInvocation, ToolOutput
        ];

        Role? matching = roles.Where(r => r.Name == name).FirstOrDefault();

        if (matching is Role r)
        {
            return r;
        }

        throw new KeyNotFoundException($"Unknown role: {name}");
    }

    private Role(string name)
    {
        this.Name = name;
    }

    public string Name { get; }
}
