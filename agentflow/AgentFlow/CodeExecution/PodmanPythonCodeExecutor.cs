using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AgentFlow.CodeExecution;

public class PodmanPythonCodeExecutor : ICodeExecutor
{
    private const string PythonImageName = "python:3.10-slim";

    private readonly string pythonEnvPath = "./pyenv";

    private readonly ILogger<PodmanPythonCodeExecutor> logger;

    public PodmanPythonCodeExecutor(ILogger<PodmanPythonCodeExecutor> logger)
    {
        this.logger = logger;
    }

    public Task<string> ExecuteCodeAsync(string code)
    {
        this.PullImage();
        this.logger.LogInformation("running code:\n{Code}", code);
        string codeResult = this.RunWithPython(code);
        this.logger.LogInformation("saw code result: {Result}", codeResult);

        return Task.FromResult(codeResult.Trim());
    }

    private void PullImage()
    {
        string output = this.RunWithBash($"podman pull {PythonImageName}");
        this.logger.LogInformation("Output of pulling image: {Output}", output);
    }

    private string RunWithPython(string pythonCode)
    {
        var dirInfo = Directory.CreateDirectory(this.pythonEnvPath);

        string runFile = Path.Combine(dirInfo.FullName, "run.py");

        File.WriteAllText(runFile, pythonCode);

        this.logger.LogInformation("Wrote code content to {RunFile}", runFile);

        string runCommand = CreateRunCommand(dirInfo.FullName, PythonImageName);

        this.logger.LogInformation("Running: {RunCommand}", runCommand);

        return this.RunWithBash(runCommand);
    }

    private static string CreateRunCommand(string pyEnvDir, string pythonImageName)
        => $"podman run --rm -v {pyEnvDir}:/pyenv {pythonImageName} python /pyenv/run.py";

    private string RunWithBash(string command)
    {
        string args = $"-c \"{command}\"";
        var psi = new ProcessStartInfo
        {
            FileName = "/usr/bin/bash",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not spawn a bash process.");

        string stdOut = process.StandardOutput.ReadToEnd();

        string stdErr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return $"{stdOut}{stdErr}";
    }
}
