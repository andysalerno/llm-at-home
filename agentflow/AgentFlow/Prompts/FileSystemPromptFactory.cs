﻿using AgentFlow.Config;
using AgentFlow.Generic;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Prompts;

public sealed class FileSystemPromptFactoryProvider : IFactoryProvider<Prompt, PromptName>
{
    private readonly IPromptParser parser;
    private readonly IFileSystemPromptProviderConfig config;

    public FileSystemPromptFactoryProvider(IPromptParser parser, IFileSystemPromptProviderConfig config)
    {
        this.parser = parser;
        this.config = config;
    }

    public IFactory<Prompt> GetFactory(PromptName name)
    {
        return new FileSystemPromptFactory(name, this.parser, this.config);
    }
}

public class FileSystemPromptFactory : IFactory<Prompt>
{
    private readonly string promptDirectoryLocalPath;
    private readonly PromptName promptName;
    private readonly IPromptParser parser;
    private readonly ILogger<FileSystemPromptFactory> logger;

    public FileSystemPromptFactory(PromptName promptName, IPromptParser parser, IFileSystemPromptProviderConfig config)
    {
        this.promptDirectoryLocalPath = config.PromptDirectory;
        this.promptName = promptName;
        this.parser = parser;
        this.logger = this.GetLogger();
    }

    public Prompt Create()
    {
        // prompt names are assumed to be txt file names:
        string txtFileName = $"{this.promptName.Value}.txt";

        var file = Path.Combine(this.promptDirectoryLocalPath, txtFileName);

        string fullPath = Path.GetFullPath(file);

        this.logger.LogInformation("Looking for prompt file: {FullPath}", fullPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Prompt file not found: {fullPath}");
        }

        var text = File.ReadAllText(fullPath).Trim().Replace("\r\n", "\n", StringComparison.Ordinal);

        return this.parser.Parse(text);
    }
}
