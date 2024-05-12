using System.CommandLine;
using AgentFlow.Agents;
using AgentFlow.CodeExecution;
using AgentFlow.Config;
using AgentFlow.Examples;
using AgentFlow.LlmClient;
using AgentFlow.LlmClients.OpenAI;
using AgentFlow.Prompts;
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
        var rootCommand = new RootCommand("Run an example");

        var uriArg = new Argument<string>(
            name: "uri",
            description: "the target URI of the service hosting the LLM API");

        var scraperUriArg = new Argument<string>(
            name: "scraperUri",
            description: "the target URI of the scraping service");

        var embeddingsUriArg = new Argument<string>(
            name: "embeddingsUri",
            description: "the target URI of the embeddings service");

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

        rootCommand.AddArgument(uriArg);
        rootCommand.AddArgument(embeddingsUriArg);
        rootCommand.AddArgument(scraperUriArg);
        rootCommand.AddOption(verbose);
        rootCommand.AddOption(promptDir);

        rootCommand.SetHandler(
            RunAppAsync,
            uriArg,
            embeddingsUriArg,
            scraperUriArg,
            verbose,
            promptDir);

        await rootCommand.InvokeAsync(args);
    }

    internal static async Task RunAppAsync(string uri, string embeddingsUri, string scraperUri, bool verbose, string? promptDir)
    {
        promptDir = promptDir ?? throw new ArgumentNullException(nameof(promptDir));

        var commandLineArgs = new CommandLineArgs(uri, embeddingsUri, scraperUri, verbose, promptDir);

        IContainer container = ConfigureContainer(commandLineArgs);

        await using var scope = container.BeginLifetimeScope();

        // Register logging before anything else:
        {
            ILoggerFactory loggerFactory = scope.Resolve<ILoggerFactory>();
            Logging.RegisterLoggerFactory(loggerFactory);
        }

        var serverExample = scope.Resolve<AgentBenchExample>();

        await serverExample.RunAsync();
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
            containerBuilder.RegisterType<FileSystemPromptProvider>().AsImplementedInterfaces();

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
        => new Configuration(new Uri(args.Uri), new Uri(args.EmbeddingsUri), new Uri(args.ScraperUri))
        {
            LogRequestsToLlm = args.Verbose,
            PromptDirectory = args.PromptDir ?? "./Prompts",
        };

    private sealed class CommandLineArgs
    {
        public CommandLineArgs(string uri, string embeddingsUri, string scraperUri, bool verbose, string promptDir)
        {
            this.Uri = uri;
            this.EmbeddingsUri = embeddingsUri;
            this.ScraperUri = scraperUri;
            this.Verbose = verbose;
            this.PromptDir = promptDir;
        }

        public string Uri { get; }

        public string ScraperUri { get; }

        public string EmbeddingsUri { get; }

        public bool Verbose { get; }

        public string PromptDir { get; }
    }
}
