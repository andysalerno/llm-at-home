using System.CommandLine;
using AgentFlow.Agents;
using AgentFlow.CodeExecution;
using AgentFlow.Config;
using AgentFlow.Examples;
using AgentFlow.LlmClient;
using AgentFlow.LlmClients.OpenAI;
using AgentFlow.WorkSpace;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentFlow;

public static class Program
{
    public static async Task Main(string[] args)
    {
        string s = string.Join(" ", args);
        Console.WriteLine(s);

        var rootCommand = BuildRootCommand();

        await rootCommand.InvokeAsync(args);
    }

    private static Command BuildBenchmarkCommand()
    {
        var command = new Command("bench");

        var uriArg = new Argument<string>(
            name: "uri",
            description: "the target URI of the service hosting the LLM API");

        var modelName = new Argument<string>(
            name: "modelName",
            description: "The name of the model to use");

        var verbose = new Option<bool>(
            name: "-v",
            getDefaultValue: () => false,
            description: "Enable verbose mode");
        verbose.AddAlias("--verbose");

        var promptDir = new Option<string?>(
            name: "-p",
            getDefaultValue: () => "./Prompts",
            description: "directory containing prompt files");
        promptDir.AddAlias("--prompt-dir");

        command.AddArgument(uriArg);
        command.AddArgument(modelName);
        command.AddOption(verbose);
        command.AddOption(promptDir);

        command.SetHandler(
            RunBenchmarkAsync,
            uriArg,
            modelName,
            verbose,
            promptDir);

        return command;
    }

    private static Command BuildServerCommand()
    {
        var command = new Command("server");

        var uriArg = new Argument<string>(
            name: "uri",
            description: "the target URI of the service hosting the LLM API");

        var scraperUriArg = new Argument<string>(
            name: "scraperUri",
            description: "the target URI of the scraping service");

        var embeddingsUriArg = new Argument<string>(
            name: "embeddingsUri",
            description: "the target URI of the embeddings service");

        var modelName = new Argument<string>(
            name: "modelName",
            description: "The name of the model to use");

        var verbose = new Option<bool>(
            name: "-v",
            getDefaultValue: () => false,
            description: "Enable verbose mode");
        verbose.AddAlias("--verbose");

        var promptDir = new Option<string?>(
            name: "-p",
            getDefaultValue: () => "./Prompts",
            description: "directory containing prompt files");
        promptDir.AddAlias("--prompt-dir");

        command.AddArgument(uriArg);
        command.AddArgument(embeddingsUriArg);
        command.AddArgument(scraperUriArg);
        command.AddArgument(modelName);
        command.AddOption(verbose);
        command.AddOption(promptDir);

        command.SetHandler(
            RunServerAsync,
            uriArg,
            embeddingsUriArg,
            scraperUriArg,
            modelName,
            verbose,
            promptDir);

        return command;
    }

    private static RootCommand BuildRootCommand()
    {
        var command = new RootCommand("Run an example");

        command.AddCommand(BuildServerCommand());
        command.AddCommand(BuildBenchmarkCommand());

        return command;
    }

    private static async Task RunBenchmarkAsync(
        string uri,
        string modelName,
        bool verbose,
        string? promptDir)
    {
        promptDir = promptDir ?? throw new ArgumentNullException(nameof(promptDir));

        var commandLineArgs = new CommandLineArgs(uri, uri, uri, modelName, verbose, promptDir);

        IContainer container = ConfigureContainer(commandLineArgs);

        await using var scope = container.BeginLifetimeScope();

        // Register logging before anything else:
        {
            ILoggerFactory loggerFactory = scope.Resolve<ILoggerFactory>();
            Logging.RegisterLoggerFactory(loggerFactory);
        }

        var example = scope.Resolve<AgentBenchExample>();

        await example.RunAsync();
    }

    private static async Task RunServerAsync(
        string uri,
        string embeddingsUri,
        string scraperUri,
        string modelName,
        bool verbose,
        string? promptDir)
    {
        promptDir = promptDir ?? throw new ArgumentNullException(nameof(promptDir));

        var commandLineArgs = new CommandLineArgs(uri, embeddingsUri, scraperUri, modelName, verbose, promptDir);

        IContainer container = ConfigureContainer(commandLineArgs);

        await using var scope = container.BeginLifetimeScope();

        // Register logging before anything else:
        {
            ILoggerFactory loggerFactory = scope.Resolve<ILoggerFactory>();
            Logging.RegisterLoggerFactory(loggerFactory);
        }

        var example = scope.Resolve<OpenAIServerWebSearchExample>();
        // var example = scope.Resolve<AgentBenchExample>();

        await example.RunAsync();
    }

    private static IContainer ConfigureContainer(CommandLineArgs args)
    {
        var containerBuilder = new ContainerBuilder();

        // Register dotnet ServiceCollection builtins:
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient();
            serviceCollection.AddLogging(builder =>
            {
                builder.AddSimpleConsole(config =>
                {
                    config.IncludeScopes = true;
                    config.SingleLine = true;
                    config.TimestampFormat = "HH:mm::ss::ff";
                });
            });

            containerBuilder.Populate(serviceCollection);
        }

        // Register the types from this project:
        {
            // containerBuilder.RegisterType<PodmanPythonCodeExecutor>().AsImplementedInterfaces();
            containerBuilder.RegisterType<HostedCodeExecutor>().AsImplementedInterfaces();
            containerBuilder.RegisterType<CellRunner<ConversationThread>>().AsImplementedInterfaces();
            containerBuilder.RegisterType<ChatMLMessageFormatter>().AsImplementedInterfaces();
            containerBuilder.RegisterType<UserConsoleAgent>();

            // containerBuilder.RegisterType<GroqCompletionsClient>().AsImplementedInterfaces();
            // containerBuilder.RegisterType<VllmCompletionsClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<OpenAICompletionsClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<CustomAgentBuilderFactory>();

            // Runnable example classes:
            containerBuilder.RegisterType<WebSearchExample>();
            containerBuilder.RegisterType<SimpleChatExample>();
            containerBuilder.RegisterType<AgentBenchExample>();
            containerBuilder.RegisterType<OpenAIServerWebSearchExample>();
        }

        // Parse args as configuration:
        {
            Configuration configuration = BuildConfiguration(args);
            containerBuilder
                .RegisterInstance(configuration)
                .AsImplementedInterfaces();
        }

        return containerBuilder.Build();
    }

    private static Configuration BuildConfiguration(CommandLineArgs args)
        => new Configuration(new Uri(args.Uri), new Uri(args.EmbeddingsUri), new Uri(args.ScraperUri), args.ModelName)
        {
            LogRequestsToLlm = args.Verbose,
            PromptDirectory = args.PromptDir ?? "./Prompts",
        };

    private sealed record CommandLineArgs(
        string Uri,
        string EmbeddingsUri,
        string ScraperUri,
        string ModelName,
        bool Verbose,
        string PromptDir);
}
