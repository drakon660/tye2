using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Core.ConfigModel;
using Xunit;

namespace Tye2.UnitTests;

public class CliCommandTests : IDisposable
{
    private readonly string _tempDir;

    public CliCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"tye2_cli_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    #region Command Structure Tests

    [Fact]
    public void BuildCommand_HasCorrectName()
    {
        var command = Program.CreateBuildCommand();
        command.Name.Should().Be("build");
    }

    [Fact]
    public void BuildCommand_HasCorrectDescription()
    {
        var command = Program.CreateBuildCommand();
        command.Description.Should().Be("build containers for the application");
    }

    [Fact]
    public void BuildCommand_HasExpectedOptions()
    {
        var command = Program.CreateBuildCommand();
        var optionNames = command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("interactive");
        optionNames.Should().Contain("tags");
        optionNames.Should().Contain("verbosity");
        optionNames.Should().Contain("framework");
    }

    [Fact]
    public void BuildCommand_HasPathArgument()
    {
        var command = Program.CreateBuildCommand();
        command.Arguments.Should().HaveCount(1);
        command.Arguments.First().Name.Should().Be("path");
    }

    [Fact]
    public void DeployCommand_HasCorrectName()
    {
        var command = Program.CreateDeployCommand();
        command.Name.Should().Be("deploy");
    }

    [Fact]
    public void DeployCommand_HasExpectedOptions()
    {
        var command = Program.CreateDeployCommand();
        var optionNames = command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("interactive");
        optionNames.Should().Contain("verbosity");
        optionNames.Should().Contain("namespace");
        optionNames.Should().Contain("framework");
        optionNames.Should().Contain("tags");
        optionNames.Should().Contain("force");
    }

    [Fact]
    public void PushCommand_HasCorrectName()
    {
        var command = Program.CreatePushCommand();
        command.Name.Should().Be("push");
    }

    [Fact]
    public void PushCommand_HasExpectedOptions()
    {
        var command = Program.CreatePushCommand();
        var optionNames = command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("interactive");
        optionNames.Should().Contain("verbosity");
        optionNames.Should().Contain("tags");
        optionNames.Should().Contain("framework");
        optionNames.Should().Contain("force");
    }

    [Fact]
    public void GenerateCommand_HasCorrectName()
    {
        var command = Program.CreateGenerateCommand();
        command.Name.Should().Be("generate");
    }

    [Fact]
    public void GenerateCommand_IsHidden()
    {
        var command = Program.CreateGenerateCommand();
        command.IsHidden.Should().BeTrue();
    }

    [Fact]
    public void GenerateCommand_HasExpectedOptions()
    {
        var command = Program.CreateGenerateCommand();
        var optionNames = command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("interactive");
        optionNames.Should().Contain("verbosity");
        optionNames.Should().Contain("namespace");
        optionNames.Should().Contain("framework");
        optionNames.Should().Contain("tags");
    }

    [Fact]
    public void UndeployCommand_HasCorrectName()
    {
        var command = Program.CreateUndeployCommand();
        command.Name.Should().Be("undeploy");
    }

    [Fact]
    public void UndeployCommand_HasWhatIfOption()
    {
        var command = Program.CreateUndeployCommand();
        var optionNames = command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("what-if");
    }

    [Fact]
    public void UndeployCommand_HasExpectedOptions()
    {
        var command = Program.CreateUndeployCommand();
        var optionNames = command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("namespace");
        optionNames.Should().Contain("interactive");
        optionNames.Should().Contain("verbosity");
        optionNames.Should().Contain("tags");
    }

    [Fact]
    public void AllPublicCommands_HaveHandlers()
    {
        Program.CreateBuildCommand().Handler.Should().NotBeNull();
        Program.CreateDeployCommand().Handler.Should().NotBeNull();
        Program.CreatePushCommand().Handler.Should().NotBeNull();
        Program.CreateGenerateCommand().Handler.Should().NotBeNull();
        Program.CreateUndeployCommand().Handler.Should().NotBeNull();
    }

    #endregion

    #region StandardOptions Tests

    [Fact]
    public void StandardOptions_Environment_HasDefaultValue()
    {
        var option = StandardOptions.Environment;
        option.Argument.HasDefaultValue.Should().BeTrue();
    }

    [Fact]
    public void StandardOptions_Environment_HasShortAlias()
    {
        var option = StandardOptions.Environment;
        option.Aliases.Should().Contain("-e");
        option.Aliases.Should().Contain("--environment");
    }

    [Fact]
    public void StandardOptions_Tags_IsNotRequired()
    {
        var option = StandardOptions.Tags;
        option.Required.Should().BeFalse();
    }

    [Fact]
    public void StandardOptions_Framework_HasShortAlias()
    {
        var option = StandardOptions.Framework;
        option.Aliases.Should().Contain("-f");
        option.Aliases.Should().Contain("--framework");
    }

    [Fact]
    public void StandardOptions_Interactive_HasShortAlias()
    {
        var option = StandardOptions.Interactive;
        option.Aliases.Should().Contain("-i");
        option.Aliases.Should().Contain("--interactive");
    }

    [Fact]
    public void StandardOptions_Verbosity_HasShortAlias()
    {
        var option = StandardOptions.Verbosity;
        option.Aliases.Should().Contain("-v");
        option.Aliases.Should().Contain("--verbosity");
    }

    [Fact]
    public void StandardOptions_Verbosity_HasDefaultValue()
    {
        var option = StandardOptions.Verbosity;
        option.Argument.HasDefaultValue.Should().BeTrue();
    }

    [Fact]
    public void StandardOptions_Namespace_HasShortAlias()
    {
        var option = StandardOptions.Namespace;
        option.Aliases.Should().Contain("-n");
        option.Aliases.Should().Contain("--namespace");
    }

    [Fact]
    public void StandardOptions_NoDefaultOptions_HasAlias()
    {
        var option = StandardOptions.NoDefaultOptions;
        option.Aliases.Should().Contain("--no-default");
    }

    [Fact]
    public void StandardOptions_CreateForce_HasCorrectDescription()
    {
        var option = StandardOptions.CreateForce("test description");
        option.Description.Should().Be("test description");
        option.Aliases.Should().Contain("--force");
    }

    [Fact]
    public void StandardOptions_Outputs_HasSuggestions()
    {
        var option = StandardOptions.Outputs;
        option.Aliases.Should().Contain("-o");
        option.Aliases.Should().Contain("--outputs");
    }

    [Fact]
    public void StandardOptions_Outputs_HasDefaultValue()
    {
        var option = StandardOptions.Outputs;
        option.Argument.HasDefaultValue.Should().BeTrue();
    }

    [Fact]
    public void StandardOptions_Project_HasShortAlias()
    {
        var option = StandardOptions.Project;
        option.Aliases.Should().Contain("-p");
        option.Aliases.Should().Contain("--project");
    }

    #endregion

    #region ApplicationFactoryFilter Tests

    [Fact]
    public void ApplicationFactoryFilter_NullTags_ReturnsNull()
    {
        var filter = ApplicationFactoryFilter.GetApplicationFactoryFilter(null!);
        filter.Should().BeNull();
    }

    [Fact]
    public void ApplicationFactoryFilter_EmptyTags_ReturnsNull()
    {
        var filter = ApplicationFactoryFilter.GetApplicationFactoryFilter(Array.Empty<string>());
        filter.Should().BeNull();
    }

    [Fact]
    public void ApplicationFactoryFilter_WithTags_ReturnsFilter()
    {
        var filter = ApplicationFactoryFilter.GetApplicationFactoryFilter(new[] { "frontend" });
        filter.Should().NotBeNull();
        filter!.ServicesFilter.Should().NotBeNull();
        filter.IngressFilter.Should().NotBeNull();
    }

    [Fact]
    public void ApplicationFactoryFilter_ServicesFilter_MatchesTag()
    {
        var filter = ApplicationFactoryFilter.GetApplicationFactoryFilter(new[] { "frontend" });
        var service = new ConfigService { Name = "web" };
        service.Tags.Add("frontend");

        filter!.ServicesFilter!(service).Should().BeTrue();
    }

    [Fact]
    public void ApplicationFactoryFilter_ServicesFilter_DoesNotMatchUntagged()
    {
        var filter = ApplicationFactoryFilter.GetApplicationFactoryFilter(new[] { "frontend" });
        var service = new ConfigService { Name = "api" };
        service.Tags.Add("backend");

        filter!.ServicesFilter!(service).Should().BeFalse();
    }

    [Fact]
    public void ApplicationFactoryFilter_MultipleTags_MatchesAny()
    {
        var filter = ApplicationFactoryFilter.GetApplicationFactoryFilter(new[] { "frontend", "backend" });
        var service = new ConfigService { Name = "api" };
        service.Tags.Add("backend");

        filter!.ServicesFilter!(service).Should().BeTrue();
    }

    #endregion

    #region ConfigFileFinder Tests

    [Fact]
    public void ConfigFileFinder_EmptyDirectory_ReturnsFalse()
    {
        var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var errorMessage);
        result.Should().BeFalse();
        errorMessage.Should().Contain("No project");
    }

    [Fact]
    public void ConfigFileFinder_SingleCsproj_FindsIt()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.csproj"), "<Project />");

        var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var errorMessage);
        result.Should().BeTrue();
        filePath.Should().EndWith("test.csproj");
    }

    [Fact]
    public void ConfigFileFinder_TyeYaml_FoundFirst()
    {
        File.WriteAllText(Path.Combine(_tempDir, "tye.yaml"), "name: test");
        File.WriteAllText(Path.Combine(_tempDir, "test.csproj"), "<Project />");

        var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var errorMessage);
        result.Should().BeTrue();
        filePath.Should().EndWith("tye.yaml");
    }

    [Fact]
    public void ConfigFileFinder_MultipleCsproj_ReturnsError()
    {
        File.WriteAllText(Path.Combine(_tempDir, "a.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(_tempDir, "b.csproj"), "<Project />");

        var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var errorMessage);
        result.Should().BeFalse();
        errorMessage.Should().Contain("More than one");
    }

    [Fact]
    public void ConfigFileFinder_SingleSln_FindsIt()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.sln"), "");

        var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var errorMessage);
        result.Should().BeTrue();
        filePath.Should().EndWith("test.sln");
    }

    [Fact]
    public void ConfigFileFinder_CustomFileFormats_UsesProvided()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.csproj"), "<Project />");

        var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var errorMessage, new[] { "*.sln" });
        result.Should().BeFalse(); // only looking for .sln, won't find .csproj
    }

    [Fact]
    public void ConfigFileFinder_FsprojFile_FindsIt()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.fsproj"), "<Project />");

        var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var errorMessage);
        result.Should().BeTrue();
        filePath.Should().EndWith("test.fsproj");
    }

    [Fact]
    public void ConfigFileFinder_DockerCompose_FindsIt()
    {
        File.WriteAllText(Path.Combine(_tempDir, "docker-compose.yaml"), "version: '3'");

        var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var errorMessage);
        result.Should().BeTrue();
        filePath.Should().EndWith("docker-compose.yaml");
    }

    #endregion

    #region InitHost Tests

    [Fact]
    public void InitHost_CreateTyeFileContent_NoPath_ReturnsTemplate()
    {
        var (content, outputPath) = InitHost.CreateTyeFileContent(null, force: false);

        content.Should().Contain("tye2 application configuration file");
        content.Should().Contain("services:");
        content.Should().Contain("myservice");
        outputPath.Should().Be("tye.yaml");
    }

    [Fact]
    public void InitHost_CreateTyeFileContent_WithCsproj_GeneratesFromProject()
    {
        var csproj = Path.Combine(_tempDir, "MyApp.csproj");
        File.WriteAllText(csproj, @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var (content, outputPath) = InitHost.CreateTyeFileContent(new FileInfo(csproj), force: false);

        content.Should().Contain("tye2 application configuration file");
        outputPath.Should().Contain(_tempDir);
        outputPath.Should().EndWith("tye.yaml");
    }

    [Fact]
    public void InitHost_CreateTyeFileContent_ExistingTyeYaml_NoForce_Throws()
    {
        var csproj = Path.Combine(_tempDir, "MyApp.csproj");
        File.WriteAllText(csproj, "<Project />");
        File.WriteAllText(Path.Combine(_tempDir, "tye.yaml"), "name: existing");

        var action = () => InitHost.CreateTyeFileContent(new FileInfo(csproj), force: false);

        action.Should().Throw<CommandException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void InitHost_CreateTyeFile_WritesFile()
    {
        var subDir = Path.Combine(_tempDir, "init_test");
        Directory.CreateDirectory(subDir);
        var originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(subDir);

            var outputPath = InitHost.CreateTyeFile(null, force: false);
            var fullOutputPath = Path.Combine(subDir, outputPath);

            outputPath.Should().Be("tye.yaml");
            File.Exists(fullOutputPath).Should().BeTrue();
            var content = File.ReadAllText(fullOutputPath);
            content.Should().Contain("services:");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    #endregion

    #region ContainerRegistry Tests

    [Fact]
    public void ContainerRegistry_Constructor_SetsProperties()
    {
        var registry = new ContainerRegistry("myregistry.azurecr.io", "my-secret");

        registry.Hostname.Should().Be("myregistry.azurecr.io");
        registry.PullSecret.Should().Be("my-secret");
    }

    [Fact]
    public void ContainerRegistry_NullHostname_Throws()
    {
        var action = () => new ContainerRegistry(null!, null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ContainerRegistry_NullPullSecret_IsAllowed()
    {
        var registry = new ContainerRegistry("dockerhub", null);
        registry.PullSecret.Should().BeNull();
    }

    #endregion

    #region CommandException Tests

    [Fact]
    public void CommandException_MessageOnly()
    {
        var ex = new CommandException("test error");
        ex.Message.Should().Be("test error");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void CommandException_WithInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new CommandException("test error", inner);
        ex.Message.Should().Be("test error");
        ex.InnerException.Should().BeSameAs(inner);
    }

    #endregion

    #region Verbosity Enum Tests

    [Theory]
    [InlineData(Verbosity.Quiet)]
    [InlineData(Verbosity.Info)]
    [InlineData(Verbosity.Debug)]
    public void Verbosity_AllValues_AreDefined(Verbosity verbosity)
    {
        Enum.IsDefined(typeof(Verbosity), verbosity).Should().BeTrue();
    }

    #endregion

    #region DefaultOptionsMiddleware Environment Variable Tests

    [Fact]
    public void DefaultOptionsMiddleware_EnvVarName_UsesCommandNameUppercase()
    {
        // Verify the naming convention: TYE_{COMMAND}_ARGS
        var envVarName = $"TYE_BUILD_ARGS";
        Environment.SetEnvironmentVariable(envVarName, "--verbosity debug", EnvironmentVariableTarget.Process);
        try
        {
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            envValue.Should().Be("--verbosity debug");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null, EnvironmentVariableTarget.Process);
        }
    }

    #endregion

    #region Command Parsing Integration Tests

    [Fact]
    public void BuildCommand_ParsesVerbosityOption()
    {
        var command = Program.CreateBuildCommand();
        var parser = new CommandLineBuilder(new RootCommand { command }).Build();

        var result = parser.Parse("build --verbosity debug nonexistent.csproj");

        result.Errors.Should().NotContain(e => e.Message.Contains("verbosity"));
    }

    [Fact]
    public void BuildCommand_ParsesFrameworkOption()
    {
        var command = Program.CreateBuildCommand();
        var parser = new CommandLineBuilder(new RootCommand { command }).Build();

        var result = parser.Parse("build --framework net8.0 nonexistent.csproj");

        result.Errors.Should().NotContain(e => e.Message.Contains("framework"));
    }

    [Fact]
    public void BuildCommand_ParsesInteractiveOption()
    {
        var command = Program.CreateBuildCommand();
        var parser = new CommandLineBuilder(new RootCommand { command }).Build();

        var result = parser.Parse("build --interactive nonexistent.csproj");

        result.Errors.Should().NotContain(e => e.Message.Contains("interactive"));
    }

    [Fact]
    public void BuildCommand_ParsesTagsOption()
    {
        var command = Program.CreateBuildCommand();
        var parser = new CommandLineBuilder(new RootCommand { command }).Build();

        var result = parser.Parse("build --tags frontend nonexistent.csproj");

        result.Errors.Should().NotContain(e => e.Message.Contains("tags"));
    }

    [Fact]
    public void DeployCommand_ParsesNamespaceOption()
    {
        var command = Program.CreateDeployCommand();
        var parser = new CommandLineBuilder(new RootCommand { command }).Build();

        var result = parser.Parse("deploy --namespace my-namespace nonexistent.csproj");

        result.Errors.Should().NotContain(e => e.Message.Contains("namespace"));
    }

    [Fact]
    public void DeployCommand_ParsesForceOption()
    {
        var command = Program.CreateDeployCommand();
        var parser = new CommandLineBuilder(new RootCommand { command }).Build();

        var result = parser.Parse("deploy --force nonexistent.csproj");

        result.Errors.Should().NotContain(e => e.Message.Contains("force"));
    }

    [Fact]
    public void UndeployCommand_ParsesWhatIfOption()
    {
        var command = Program.CreateUndeployCommand();
        var parser = new CommandLineBuilder(new RootCommand { command }).Build();

        var result = parser.Parse("undeploy --what-if nonexistent.csproj");

        result.Errors.Should().NotContain(e => e.Message.Contains("what-if"));
    }

    #endregion
}
