using System.Text.Json;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Config;
using AgentFlow.Examples;
using AgentFlow.LlmClient;
using AgentFlow.WorkSpace;
using Autofac;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AgentFlow.ExampleRunner.Tests;

public class ExampleRunnerTests
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

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

        // test-specific:
        var client = new Mock<ILlmCompletionsClient>(MockBehavior.Strict);
        client
            .Setup(c => c.GetChatCompletionsAsync(It.IsAny<ChatCompletionsRequest>()))
            .ReturnsAsync(new ChatCompletionsResult(
                JsonSerializer.Serialize(
                    options: JsonSerializerOptions,
                    value: new ExecuteToolCell.ToolSelectionOutput(
                        LastUserMessageIntent: "the user wants a pizza",
                        FunctionName: "search_web",
                        Invocation: "search_web('pizza')"))));

        containerBuilder.RegisterInstance(client.Object).AsImplementedInterfaces();

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
