using System.Collections.Immutable;

namespace AgentFlow.LlmClient;

public record Role
{
    public static readonly Role User = new Role("user");

    public static readonly Role Assistant = new Role("assistant");

    public static readonly Role System = new Role("system");

    public static readonly Role ToolInvocation = new Role("tool_invocation");

    public static readonly Role ToolOutput = new Role("tool_output");

    private Role(string name)
    {
        this.Name = name;
    }

    public string Name { get; }

    public static Role ExpectFromName(string name)
    {
        ImmutableArray<Role> roles = [
            User, Assistant, System, ToolInvocation, ToolOutput
        ];

        name = name.Replace("_", string.Empty, StringComparison.Ordinal);

        Role? matching = roles.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

        if (matching is Role r)
        {
            return r;
        }

        throw new KeyNotFoundException($"Unknown role: {name}");
    }
}
