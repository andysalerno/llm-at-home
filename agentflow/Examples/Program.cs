using System.CommandLine;
using AgentFlow.Agents;
using AgentFlow.Config;
using AgentFlow.Examples;
using Autofac;
using Microsoft.Extensions.Logging;

namespace AgentFlow;

public static class Program
{
    public static async Task Main(string[] args)
    {
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

        var promptDir = new Option<string>(
            name: "-p",
            getDefaultValue: () => "./Prompts",
            description: "directory containing prompt files");
        promptDir.AddAlias("--prompt-dir");

        var diskLoggingDir = new Option<string>(
            name: "-d",
            getDefaultValue: () => "./LlmRequestLogs",
            description: "logging dir for requests sent to LLM");
        diskLoggingDir.AddAlias("--request-logging-dir");

        var instructionStrategy = new Option<InstructionStrategy>(
            name: "-s",
            getDefaultValue: () => InstructionStrategy.InlineSystemMessage,
            description: "instruction strategy to use");
        instructionStrategy.AddAlias("--instruction-strategy");

        command.AddArgument(uriArg);
        command.AddArgument(modelName);
        command.AddOption(verbose);
        command.AddOption(promptDir);
        command.AddOption(diskLoggingDir);
        command.AddOption(instructionStrategy);

        command.SetHandler(
            RunBenchmarkAsync,
            uriArg,
            modelName,
            verbose,
            promptDir,
            diskLoggingDir,
            instructionStrategy);

        return command;
    }

    private static Command BuildServerCommand()
    {
        // TODO: deduplicate these args between server and benchmark
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

        var promptDir = new Option<string>(
            name: "-p",
            getDefaultValue: () => "./Prompts",
            description: "directory containing prompt files");
        promptDir.AddAlias("--prompt-dir");

        var diskLoggingDir = new Option<string>(
            name: "-d",
            getDefaultValue: () => "./LlmRequestLogs",
            description: "logging dir for requests sent to LLM");
        diskLoggingDir.AddAlias("--request-logging-dir");

        var instructionStrategy = new Option<InstructionStrategy>(
            name: "-s",
            getDefaultValue: () => InstructionStrategy.InlineSystemMessage,
            description: "instruction strategy to use");
        instructionStrategy.AddAlias("--instruction-strategy");

        command.AddArgument(uriArg);
        command.AddArgument(embeddingsUriArg);
        command.AddArgument(scraperUriArg);
        command.AddArgument(modelName);
        command.AddOption(verbose);
        command.AddOption(promptDir);
        command.AddOption(diskLoggingDir);
        command.AddOption(instructionStrategy);

        command.SetHandler(
            RunServerAsync,
            uriArg,
            embeddingsUriArg,
            scraperUriArg,
            modelName,
            verbose,
            promptDir,
            diskLoggingDir,
            instructionStrategy);

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
        string promptDir,
        string diskLoggingDir,
        InstructionStrategy instructionsStrategy)
    {
        promptDir = promptDir ?? throw new ArgumentNullException(nameof(promptDir));

        var commandLineArgs = new CommandLineArgs(
            uri,
            uri,
            uri,
            modelName,
            verbose,
            promptDir,
            diskLoggingDir,
            instructionsStrategy);

        Configuration configuration = BuildConfiguration(commandLineArgs);

        IContainer container = ConfigureContainer(configuration);

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
        string promptDir,
        string diskLoggingDir,
        InstructionStrategy instructionsStrategy)
    {
        promptDir = promptDir ?? throw new ArgumentNullException(nameof(promptDir));

        var commandLineArgs = new CommandLineArgs(
            uri,
            embeddingsUri,
            scraperUri,
            modelName,
            verbose,
            promptDir,
            diskLoggingDir,
            instructionsStrategy);

        Configuration configuration = BuildConfiguration(commandLineArgs);

        IContainer container = ConfigureContainer(configuration);

        await using var scope = container.BeginLifetimeScope();

        // Register logging before anything else:
        {
            ILoggerFactory loggerFactory = scope.Resolve<ILoggerFactory>();
            Logging.RegisterLoggerFactory(loggerFactory);
        }

        var example = scope.Resolve<OpenAIServerWebSearchExample>();

        Logging.Factory
            .CreateLogger<Configuration>()
            .LogInformation("Starting up with configuration: {Configuration}", configuration);

        await example.RunAsync();
    }

    private static IContainer ConfigureContainer(Configuration configuration)
    {
        var containerBuilder = new ContainerBuilder();

        // Register dotnet ServiceCollection builtins:
        containerBuilder.RegisterModule<LocalEnvironmentModule>();

        // Register the types from this project:
        containerBuilder.RegisterModule<DependencyModule>();

        containerBuilder
            .RegisterInstance(configuration)
            .AsImplementedInterfaces();

        return containerBuilder.Build();
    }

    private static Configuration BuildConfiguration(CommandLineArgs args)
        => new Configuration(
            new Uri(args.Uri),
            new Uri(args.EmbeddingsUri),
            new Uri(args.ScraperUri),
            args.Verbose,
            args.PromptDir,
            args.ModelName,
            args.DiskLoggingDir);

    private sealed record CommandLineArgs(
        string Uri,
        string EmbeddingsUri,
        string ScraperUri,
        string ModelName,
        bool Verbose,
        string PromptDir,
        string DiskLoggingDir,
        InstructionStrategy InstructionStrategy);
}
