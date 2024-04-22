using AgentFlow.Config;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Prompts;

public class FileSystemPromptProvider
{
    private readonly string promptDirectoryLocalPath;
    private readonly string promptName;
    private readonly ILogger<FileSystemPromptProvider> logger;

    public FileSystemPromptProvider(string promptName, IFileSystemPromptProviderConfig config)
    {
        this.promptDirectoryLocalPath = config.PromptDirectory;
        this.promptName = promptName;
        this.logger = this.GetLogger();
    }

    public Prompt Get()
    {
        // prompt names are assumed to be txt file names:
        string txtFileName = $"{this.promptName}.txt";

        var file = Path.Combine(this.promptDirectoryLocalPath, txtFileName);

        string fullPath = Path.GetFullPath(file);

        this.logger.LogInformation("Looking for prompt file: {FullPath}", fullPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Prompt file not found: {fullPath}");
        }

        var text = File.ReadAllText(fullPath).Trim().Replace("\r\n", "\n", StringComparison.Ordinal);

        return new Prompt(text);
    }
}
