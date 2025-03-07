using System.Text;
using AgentFlow;
using AgentFlow.Config;
using AgentFlow.WorkSpace;

namespace Agentflow.Server.Persistence;

public sealed class DiskConversationPersistence : IConversationPersistenceWriter
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

        string requestDirPath = this.GetFullRequestDirPath(conversationDirPath, requestId);
        Directory.CreateDirectory(requestDirPath);

        int existingFilesCount = CountFilesInDir(requestDirPath);

        string requestFilePath = Path.Join(requestDirPath, $"{existingFilesCount}_{requestId.Value}.log");

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

    public Task StoreUserMessageAsync(ConversationId conversationId, IncomingRequestId requestId, StoredMessage message)
    {
        throw new NotImplementedException();
    }

    private string GetConversationParentDir()
    {
        return Path.Join(
            this.config.DiskLoggingPath,
            DateTime.Now.ToString("yyyy-MM-dd"));
    }

    private string GetFullConversationDirPath(ConversationId conversationId)
    {
        return Path.Join(
            this.config.DiskLoggingPath,
            DateTime.Now.ToString("yyyy-MM-dd"),
            conversationId.Value);
    }

    private string GetFullRequestDirPath(string conversationDirPath, IncomingRequestId incomingRequestId)
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
}
