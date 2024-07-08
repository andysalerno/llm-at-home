using AgentFlow.Agents;
using AgentFlow.Config;
using AgentFlow.Examples;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Autofac;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentFlow.ExampleRunner.Tests;

public class ExampleRunnerTests
{
    public ExampleRunnerTests()
    {
        Logging.TryRegisterLoggerFactory(NullLoggerFactory.Instance);
    }

    [Fact]
    public void OpenAIExample_CanResolve()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterModule<LocalEnvironmentModule>();
        containerBuilder.RegisterModule<DependencyModule>();
        containerBuilder.RegisterType<OpenAIServerWebSearchExample>();

        Configuration config = new Configuration(
            new Uri("http://localhost"),
            new Uri("http://localhost"),
            new Uri("http://localhost"),
            "fake");

        containerBuilder
            .RegisterInstance(config)
            .AsImplementedInterfaces();

        IContainer container = containerBuilder.Build();

        ILifetimeScope scope = container.BeginLifetimeScope();

        var example = scope.Resolve<OpenAIServerWebSearchExample>();
    }

    [Fact]
    public async Task OpenAIExample_CanRunAsync()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterModule<LocalEnvironmentModule>();
        containerBuilder.RegisterModule<DependencyModule>();
        containerBuilder.RegisterType<OpenAIServerWebSearchExample>();

        Configuration config = new Configuration(
            new Uri("http://localhost"),
            new Uri("http://localhost"),
            new Uri("http://localhost"),
            "fake");

        containerBuilder
            .RegisterInstance(config)
            .AsImplementedInterfaces();

        IContainer container = containerBuilder.Build();

        ILifetimeScope scope = container.BeginLifetimeScope();

        var example = scope.Resolve<OpenAIServerWebSearchExample>();
        ICellRunner<ConversationThread> runner = scope.Resolve<ICellRunner<ConversationThread>>();

        Cell<ConversationThread> program = example.CreateProgram();

        var initialConversation = new ConversationThread()
            .WithAddedMessage(
                new Message(
                    AgentName: new AgentName("user"),
                    Role: Role.User,
                    Content: "hi!"));

        await runner.RunAsync(program, initialConversation);
    }
}
