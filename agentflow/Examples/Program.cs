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

        var verbose = new Option<bool>(
            name: "-v",
            description: "Enable verbose mode",
            getDefaultValue: () => false);
        verbose.AddAlias("--verbose");

        var promptDir = new Option<string?>(
            name: "-p",
            description: "directory containing prompt files",
            getDefaultValue: () => "./Prompts");
        promptDir.AddAlias("--prompt-dir");

        rootCommand.AddArgument(uriArg);
        rootCommand.AddOption(verbose);
        rootCommand.AddOption(promptDir);

        rootCommand.SetHandler(RunAppAsync, uriArg, verbose, promptDir);

        await rootCommand.InvokeAsync(args);
    }

    internal static async Task RunAppAsync(string uri, bool verbose, string? promptDir)
    {
        promptDir = promptDir ?? throw new ArgumentNullException(nameof(promptDir));

        var commandLineArgs = new CommandLineArgs(uri, verbose, promptDir);

        IContainer container = ConfigureContainer(commandLineArgs);

        using var scope = container.BeginLifetimeScope();

        // Register logging before anything else:
        {
            ILoggerFactory loggerFactory = scope.Resolve<ILoggerFactory>();
            Logging.RegisterLoggerFactory(loggerFactory);
        }

        var app = scope.Resolve<App>();

        // await app.RunMagiExampleAsync();
        // await app.RunCodeExampleAsync();
        // await app.RunSimpleChatExampleAsync();
        // await app.RunWebSearchExample();
        await app.RunOpenAIServerExampleAsync();
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
            containerBuilder.RegisterType<App>();
        }

        // Parse args as configuration:
        {
            Configuration configuration = BuildConfiguration(args);
            containerBuilder
                .RegisterInstance(configuration)
                .AsImplementedInterfaces();
        }

        var container = containerBuilder.Build();

        return container;
    }

    private static Configuration BuildConfiguration(CommandLineArgs args)
    {
        // string openAIEndpointUri = args[0];
        // bool logLlmRequests = args.Any(arg => arg == "-v");
        // string? fileSystemPromptDir = args.FirstOrDefault(a => a.StartsWith("--prompt-dir="))?.Split('=')[1];
        return new Configuration(new Uri(args.Uri))
        {
            LogRequestsToLlm = args.Verbose,
            PromptDirectory = args.PromptDir ?? "./Prompts",
        };
    }

    internal sealed class App
    {
        private readonly UserConsoleAgent userConsoleAgent;
        private readonly ICodeExecutor codeExecutor;
        private readonly IFileSystemPromptProviderConfig fileSystemPromptProviderConfig;
        private readonly ILoggingConfig loggingConfig;
        private readonly ILlmCompletionsClient llmCompletionsClient;
        private readonly IEmbeddingsClient embeddingsClient;
        private readonly IScraperClient scraperClient;
        private readonly CustomAgentBuilderFactory agentBuilderFactory;
        private readonly ICellRunner<ConversationThread> runner;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<App> logger;

        public App(
            UserConsoleAgent userConsoleAgent,
            CustomAgentBuilderFactory agentBuilderFactory,
            ICellRunner<ConversationThread> runner,
            IHttpClientFactory httpClientFactory,
            ICodeExecutor codeExecutor,
            IFileSystemPromptProviderConfig fileSystemPromptProviderConfig,
            ILoggingConfig loggingConfig,
            ILlmCompletionsClient llmCompletionsClient,
            IEmbeddingsClient embeddingsClient,
            IScraperClient scraperClient,
            ILogger<App> logger)
        {
            this.userConsoleAgent = userConsoleAgent;
            this.logger = logger;
            this.codeExecutor = codeExecutor;
            this.fileSystemPromptProviderConfig = fileSystemPromptProviderConfig;
            this.loggingConfig = loggingConfig;
            this.llmCompletionsClient = llmCompletionsClient;
            this.embeddingsClient = embeddingsClient;
            this.scraperClient = scraperClient;
            this.agentBuilderFactory = agentBuilderFactory;
            this.runner = runner;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task RunWebSearchExample()
        {
            var logger = this.GetLogger();
            logger.LogInformation("Starting up...");
            logger.LogInformation("Running web search example");

            if (this.loggingConfig.LogRequestsToLlm)
            {
                logger.LogWarning("Verbosity mode enabled, will log requests and responses to/from llm");
            }

            Cell<ConversationThread> definition = WebSearchExample.CreateDefinition(
                this.userConsoleAgent,
                this.httpClientFactory,
                this.embeddingsClient,
                this.scraperClient,
                this.agentBuilderFactory,
                this.fileSystemPromptProviderConfig);

            await this.runner.RunAsync(
                definition,
                new ConversationThread());
        }

        public async Task RunSimpleChatExampleAsync()
        {
            Cell<ConversationThread> definition = SimpleChatExample.CreateDefinition(
                this.userConsoleAgent, this.agentBuilderFactory);

            await this.runner.RunAsync(
                definition,
                new ConversationThread());
        }

        public async Task RunOpenAIServerExampleAsync()
        {
            await OpenAIServerExample.ServeAsync();
        }

        public async Task RunMagiExampleAsync()
        {
            Cell<ConversationThread> definition = MagiExample.CreateDefinition(
                this.userConsoleAgent, this.agentBuilderFactory);

            await this.runner.RunAsync(
                definition,
                new ConversationThread());
        }
    }

    private class CommandLineArgs
    {
        public CommandLineArgs(string uri, bool verbose, string promptDir)
        {
            this.Uri = uri;
            this.Verbose = verbose;
            this.PromptDir = promptDir;
        }

        public string Uri { get; }

        public bool Verbose { get; }

        public string PromptDir { get; }
    }
}
