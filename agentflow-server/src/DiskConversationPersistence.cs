using System.Collections.Immutable;
using System.Text;
using AgentFlow;
using AgentFlow.Config;
using AgentFlow.WorkSpace;

namespace Agentflow.Server.Persistence;

public sealed class DiskConversationPersistence : IConversationPersistenceWriter, IConversationPersistenceReader
{
    private readonly IChatRequestDiskLoggerConfig config;

    public DiskConversationPersistence(IChatRequestDiskLoggerConfig config)
    {
        this.config = config;
    }

    public async Task StoreLlmRequestAsync(
        ConversationId conversationId,
        IncomingRequestId requestId,
        StoredLlmRequest request)
    {
        var logger = this.GetLogger();

        // Format:
        // 0_<conversation>
        //   /0_<incoming_request>
        //     /0_incoming_request.log
        //     /1_<llm_request>.log
        //     /2_<llm_request>.log
        //     /3_<llm_request>.log
        //     /4_outgoing_response.log
        //   /1_<incoming_request>
        //     /0_incoming_request.log
        //     /1_<llm_request>.log
        //     /2_<llm_request>.log
        //     /3_<llm_request>.log
        //     /4_outgoing_response.log
        Directory.CreateDirectory(this.GetConversationParentDir());

        string conversationDirPath = this.GetFullConversationDirPath(conversationId);
        Directory.CreateDirectory(conversationDirPath);

        string requestDirPath = this.GetFullRequestDirPath(conversationId, requestId);
        Directory.CreateDirectory(requestDirPath);

        int existingFilesCount = CountFilesInDir(requestDirPath);

        string requestFilePath = Path.Join(requestDirPath, $"{existingFilesCount}.log");

        logger.LogInformation(
            "Logging chat request to disk at path: {Path}", requestFilePath);

        Directory.CreateDirectory(
            Path.GetDirectoryName(requestDirPath) ?? throw new InvalidOperationException("Could not create directory"));

        var logContentBuilder = new StringBuilder();

        foreach (var message in request.Input.Concat([request.Output]))
        {
            logContentBuilder.Append($"<|{message.Role}|>\n");
            logContentBuilder.Append(message.Content);
            logContentBuilder.Append("<|end|>\n");
        }

        await File.WriteAllTextAsync(requestFilePath, logContentBuilder.ToString());
    }

    public async Task StoreUserMessageAsync(
        ConversationId conversationId,
        IncomingRequestId requestId,
        StoredMessage message)
    {
        string conversationDirPath = this.GetFullRequestDirPath(conversationId, requestId);

        var pathToWrite = Path.Join(conversationDirPath, "incoming_request.log");

        this.GetLogger().LogInformation(
            "Logging user message to disk at path: {Path}", pathToWrite);

        await File.WriteAllTextAsync(pathToWrite, message.Content);
    }

    public Task<ImmutableArray<StoredMessage>> ReadUserMessagesAsync(ConversationId conversationId)
        => throw new NotImplementedException();

    public Task<ImmutableArray<StoredLlmRequest>> ReadLlmRequestsAsync(ConversationId conversationId)
        => throw new NotImplementedException();

    private string GetConversationParentDir()
    {
        // return Path.Join(
        //     this.config.DiskLoggingPath,
        //     DateTime.Now.ToString("yyyy-MM-dd"));
        return this.config.DiskLoggingPath;
    }

    private string GetFullConversationDirPath(ConversationId conversationId)
    {
        return Path.Join(
            this.config.DiskLoggingPath,
            conversationId.Value);
    }

    private string GetFullRequestDirPath(ConversationId conversationId, IncomingRequestId incomingRequestId)
    {
        // List all the directories in the conversation directory:
        string conversationDirPath = this.GetFullConversationDirPath(conversationId);
        var existingRequestDirs = Directory.GetDirectories(conversationDirPath)
            .Select(dir => new { requestId = dir.Split('_').Last(), dir = dir })
            .ToImmutableArray();

        if (existingRequestDirs.FirstOrDefault(
            x => x.requestId == incomingRequestId.Value) is { dir: string existingDir })
        {
            return existingDir;
        }

        int existingRequestDirsCount = existingRequestDirs.Length;
        return Path.Join(
            conversationDirPath,
            $"{existingRequestDirsCount}_{incomingRequestId.Value}");
    }

    private string GetFullRequestDirPathOld(string conversationDirPath, IncomingRequestId incomingRequestId)
    {
        int existingRequestDirsCount = CountDirsInDir(conversationDirPath);

        string maybeExistingPath = Path.Join(
            conversationDirPath,
            $"{existingRequestDirsCount - 1}_{incomingRequestId.Value}");

        if (Directory.Exists(maybeExistingPath))
        {
            return maybeExistingPath;
        }

        return Path.Join(
            conversationDirPath,
            $"{existingRequestDirsCount}_{incomingRequestId.Value}");
    }

    private static int CountFilesInDir(string dirPath)
    {
        return Directory.GetFiles(dirPath).Length;
    }

    private static int CountDirsInDir(string dirPath)
    {
        return Directory.GetDirectories(dirPath).Length;
    }

    public Task<ImmutableArray<ConversationId>> ReadAllConversationIdsAsync()
    {
        // read all the Conversation IDs from the disk:
        string parentDirPath = this.GetConversationParentDir();

        if (!Directory.Exists(parentDirPath))
        {
            return Task.FromResult(ImmutableArray<ConversationId>.Empty);
        }

        var conversationIds = Directory.GetDirectories(parentDirPath)
            .Select(dir => Path.GetFileName(dir))
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => new ConversationId(name))
            .ToImmutableArray();

        return Task.FromResult(conversationIds);
    }
}
