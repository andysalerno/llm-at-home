using System.Text.Json;
using AgentFlow.Agents;
using AgentFlow.Agents.ExecutionFlow;
using AgentFlow.Config;
using AgentFlow.ExampleRunner.Tests.Extensions;
using AgentFlow.Examples;
using AgentFlow.Examples.Tools;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.LlmClients;
using AgentFlow.Prompts;
using AgentFlow.WorkSpace;
using Autofac;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static AgentFlow.Examples.Tools.WebSearchTool;

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
            false,
            "fake",
            "fake",
            "fake",
            InstructionStrategy.InlineSystemMessage);

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
            false,
            "Prompts",
            "fake",
            "fake",
            InstructionStrategy.InlineSystemMessage);

        containerBuilder
            .RegisterInstance(config)
            .AsImplementedInterfaces();

        // test-specific:
        {
            var client = new Mock<ILlmCompletionsClient>(MockBehavior.Strict);
            client
                .Setup(c => c.GetChatCompletionsAsync(It.IsAny<ChatCompletionsRequest>()))
                .ReturnsAsync(new ChatCompletionsResult(
                    JsonSerializer.Serialize(
                        value: new ExecuteToolCell.ToolSelectionOutput(
                            LastUserMessageIntent: "the user wants a pizza",
                            FunctionName: "search_web",
                            Invocation: "search_web('pizza')"),
                        options: JsonSerializerOptions)));

            containerBuilder.RegisterMockInstance(client).AsImplementedInterfaces();
        }

        // HttpClient
        {
            var factory = new Mock<IHttpClientFactory>();
            factory.SetupMockedHttpClient<WebSearchTool, SearchResults>(new SearchResults([]));

            factory.SetupMockedHttpClient<OpenAICompletionsClient, object>(new object());

            containerBuilder.RegisterMockInstance(factory).AsImplementedInterfaces();
        }

        // Scraper client
        {
            var scraperMock = new Mock<IScraperClient>();
            scraperMock
                .Setup(s => s.GetScrapedSiteContentAsync(It.IsAny<IEnumerable<Uri>>()))
                .ReturnsAsync(() => new ScrapeResponse([]));

            containerBuilder.RegisterMockInstance(scraperMock);
        }

        // Embeddings client
        {
            var embeddingClientMock = new Mock<IEmbeddingsClient>();
            embeddingClientMock
                .Setup(s => s.GetEmbeddingsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<Chunk>>()))
                .ReturnsAsync(() => new EmbeddingResponse([], new EmbeddingData([], 0), "mymodel"));

            embeddingClientMock
                .Setup(s => s.GetScoresAsync(It.IsAny<string>(), It.IsAny<IEnumerable<Chunk>>()))
                .ReturnsAsync(() => new ScoresResponse([]));

            containerBuilder.RegisterMockInstance(embeddingClientMock);
        }

        // Env vars
        {
            var envVarProvider = new Mock<IEnvironmentVariableProvider>();
            _ = envVarProvider
                .Setup(p => p.GetVariableValue(It.IsAny<string>()))
                .Returns((string _) => "test");

            containerBuilder.RegisterMockInstance(envVarProvider).AsImplementedInterfaces();
        }

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
