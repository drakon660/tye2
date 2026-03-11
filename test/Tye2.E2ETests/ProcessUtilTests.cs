using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tye2.Core;
using Tye2.Test.Infrastructure;
using Xunit;

namespace Tye2.E2ETests;

public class ProcessUtilTests
{
    [Fact]
    public async Task ExecuteAsync_DotnetBuild_ExecuteAsyncApp_ReturnsZero()
    {
        using var projectDirectory = TestHelpers.CopyTestProjectDirectory(Path.Combine("process-util", "execute-async-app"));
        var projectFile = Path.Combine(projectDirectory.DirectoryPath, "execute-async-app.csproj");

        var stdOut = new List<string>();
        var stdErr = new List<string>();

        var buildExitCode = await ProcessUtil.ExecuteAsync(
            "dotnet",
            $"build \"{projectFile}\" /nologo",
            workingDir: projectDirectory.DirectoryPath,
            stdOut: line => stdOut.Add(line),
            stdErr: line => stdErr.Add(line));

        Assert.Equal(0, buildExitCode);

        var outputAssembly = Path.Combine(projectDirectory.DirectoryPath, "bin", "Debug", "net8.0", "execute-async-app.dll");
        var tempRoot = Path.GetFullPath(Path.GetTempPath());
        var workingDir = Path.GetFullPath(projectDirectory.DirectoryPath);
        var builtAssemblyPath = Path.GetFullPath(outputAssembly);

        Assert.StartsWith(tempRoot, workingDir, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(workingDir, builtAssemblyPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            File.Exists(outputAssembly),
            $"Expected build output not found: {outputAssembly}{Environment.NewLine}stderr:{Environment.NewLine}{string.Join(Environment.NewLine, stdErr)}");
    }

    [Fact]
    public async Task RunAsync_DotnetBuild_ExecuteAsyncApp_ReturnsZero()
    {
        using var projectDirectory = TestHelpers.CopyTestProjectDirectory(Path.Combine("process-util", "execute-async-app"));
        var projectFile = Path.Combine(projectDirectory.DirectoryPath, "execute-async-app.csproj");

        var result = await ProcessUtil.RunAsync(
            "dotnet",
            $"build \"{projectFile}\" /nologo",
            workingDirectory: projectDirectory.DirectoryPath);

        Assert.Equal(0, result.ExitCode);

        var outputAssembly = Path.Combine(projectDirectory.DirectoryPath, "bin", "Debug", "net8.0", "execute-async-app.dll");
        var tempRoot = Path.GetFullPath(Path.GetTempPath());
        var workingDir = Path.GetFullPath(projectDirectory.DirectoryPath);
        var builtAssemblyPath = Path.GetFullPath(outputAssembly);

        Assert.StartsWith(tempRoot, workingDir, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(workingDir, builtAssemblyPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            File.Exists(outputAssembly),
            $"Expected build output not found: {outputAssembly}{Environment.NewLine}stderr:{Environment.NewLine}{result.StandardError}");
    }
}
