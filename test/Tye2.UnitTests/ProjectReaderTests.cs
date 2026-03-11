using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests;

public class ProjectReaderTests
{
    private static readonly string TestAssetsRoot = Path.GetFullPath(
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Tye2.E2ETests", "testassets", "projects"));

    // --- EnumerateProjects ---

    [Fact]
    public void EnumerateProjects_SingleProject_ReturnsOne()
    {
        var slnFile = new FileInfo(Path.Combine(TestAssetsRoot, "single-project", "single-project.sln"));

        var projects = ProjectReader.EnumerateProjects(slnFile).ToList();

        projects.Should().HaveCount(1);
        projects[0].Name.Should().Be("test-project.csproj");
    }

    [Fact]
    public void EnumerateProjects_FrontendBackend_ReturnsTwo()
    {
        var slnFile = new FileInfo(Path.Combine(TestAssetsRoot, "frontend-backend", "frontend-backend.sln"));

        var projects = ProjectReader.EnumerateProjects(slnFile).ToList();

        projects.Should().HaveCount(2);
        var names = projects.Select(p => p.Name).ToList();
        names.Should().Contain("backend.csproj");
        names.Should().Contain("frontend.csproj");
    }

    [Fact]
    public void EnumerateProjects_MultiProject_ReturnsThree()
    {
        var slnFile = new FileInfo(Path.Combine(TestAssetsRoot, "multi-project", "multi-project.sln"));

        var projects = ProjectReader.EnumerateProjects(slnFile).ToList();

        projects.Should().HaveCount(3);
    }

    [Fact]
    public void EnumerateProjects_ReturnsCsprojOnly()
    {
        var slnFile = new FileInfo(Path.Combine(TestAssetsRoot, "frontend-backend", "frontend-backend.sln"));

        var projects = ProjectReader.EnumerateProjects(slnFile).ToList();

        projects.Should().AllSatisfy(p => p.Extension.Should().Be(".csproj"));
    }

    [Fact]
    public void EnumerateProjects_SkipsSolutionFolders()
    {
        // Create a temp .sln with a solution folder
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""src"", ""src"", ""{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""App"", ""src\App\App.csproj"", ""{11111111-2222-3333-4444-555555555555}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{11111111-2222-3333-4444-555555555555}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{11111111-2222-3333-4444-555555555555}.Debug|Any CPU.Build.0 = Debug|Any CPU
	EndGlobalSection
EndGlobal";
            var slnPath = Path.Combine(tempDir, "test.sln");
            File.WriteAllText(slnPath, slnContent);

            var projects = ProjectReader.EnumerateProjects(new FileInfo(slnPath)).ToList();

            // Only the .csproj project, not the solution folder
            projects.Should().HaveCount(1);
            projects[0].Name.Should().Be("App.csproj");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnumerateProjects_IncludesFsproj()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{F2A71F9B-5D33-465A-A702-920D77279786}"") = ""FsApp"", ""FsApp\FsApp.fsproj"", ""{AAAAAAAA-1111-2222-3333-444444444444}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{AAAAAAAA-1111-2222-3333-444444444444}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{AAAAAAAA-1111-2222-3333-444444444444}.Debug|Any CPU.Build.0 = Debug|Any CPU
	EndGlobalSection
EndGlobal";
            var slnPath = Path.Combine(tempDir, "test.sln");
            File.WriteAllText(slnPath, slnContent);

            var projects = ProjectReader.EnumerateProjects(new FileInfo(slnPath)).ToList();

            projects.Should().HaveCount(1);
            projects[0].Name.Should().Be("FsApp.fsproj");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnumerateProjects_SkipsVbproj()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") = ""VbApp"", ""VbApp\VbApp.vbproj"", ""{BBBBBBBB-1111-2222-3333-444444444444}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{BBBBBBBB-1111-2222-3333-444444444444}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{BBBBBBBB-1111-2222-3333-444444444444}.Debug|Any CPU.Build.0 = Debug|Any CPU
	EndGlobalSection
EndGlobal";
            var slnPath = Path.Combine(tempDir, "test.sln");
            File.WriteAllText(slnPath, slnContent);

            var projects = ProjectReader.EnumerateProjects(new FileInfo(slnPath)).ToList();

            // .vbproj is not supported
            projects.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnumerateProjects_ReturnsAbsolutePaths()
    {
        var slnFile = new FileInfo(Path.Combine(TestAssetsRoot, "single-project", "single-project.sln"));

        var projects = ProjectReader.EnumerateProjects(slnFile).ToList();

        projects[0].FullName.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(projects[0].FullName).Should().BeTrue();
    }

    // --- ReadProjectDetails ---

    [Fact]
    public void ReadProjectDetails_ParsesMetadataFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var metadataContent = @"AssemblyInformationalVersion:1.2.3
TargetFramework:net8.0
AssemblyName:MyApp
TargetPath:bin\Debug\net8.0\MyApp.dll
PublishDir:bin\Debug\net8.0\publish\
IntermediateOutputPath:obj\Debug\net8.0\
RunCommand:dotnet
RunArguments:run
MicrosoftNETPlatformLibrary:Microsoft.AspNetCore.App
UsingMicrosoftNETSdkWeb:true";

            var metadataFile = Path.Combine(tempDir, "metadata.txt");
            File.WriteAllText(metadataFile, metadataContent);

            var projectFile = new FileInfo(Path.Combine(tempDir, "MyApp.csproj"));
            File.WriteAllText(projectFile.FullName, "<Project />");

            var project = new DotnetProjectServiceBuilder("myapp", projectFile, ServiceSource.Configuration)
            {
                AssemblyName = "MyApp"
            };

            var output = new OutputContext(new TestConsole(), Verbosity.Debug);

            ProjectReader.ReadProjectDetails(output, project, metadataFile);

            project.Version.Should().Be("1.2.3");
            project.TargetFramework.Should().Be("net8.0");
            project.AssemblyName.Should().Be("MyApp");
            project.RunCommand.Should().Be("dotnet");
            project.RunArguments.Should().Be("run");
            project.IsAspNet.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ReadProjectDetails_InvalidVersion_UsesDefault()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var metadataContent = @"Version:not-a-version
TargetFramework:net8.0
AssemblyName:MyApp
TargetPath:bin\Debug\net8.0\MyApp.dll
PublishDir:bin\Debug\net8.0\publish\
IntermediateOutputPath:obj\Debug\net8.0\";

            var metadataFile = Path.Combine(tempDir, "metadata.txt");
            File.WriteAllText(metadataFile, metadataContent);

            var projectFile = new FileInfo(Path.Combine(tempDir, "MyApp.csproj"));
            File.WriteAllText(projectFile.FullName, "<Project />");

            var project = new DotnetProjectServiceBuilder("myapp", projectFile, ServiceSource.Configuration);

            var output = new OutputContext(new TestConsole(), Verbosity.Debug);

            ProjectReader.ReadProjectDetails(output, project, metadataFile);

            project.Version.Should().Be("0.1.0");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ReadProjectDetails_NullOutput_Throws()
    {
        var act = () => ProjectReader.ReadProjectDetails(null!, null!, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadProjectDetails_NullProject_Throws()
    {
        var output = new OutputContext(new TestConsole(), Verbosity.Debug);
        var act = () => ProjectReader.ReadProjectDetails(output, null!, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadProjectDetails_NullMetadataFile_Throws()
    {
        var output = new OutputContext(new TestConsole(), Verbosity.Debug);
        var project = new DotnetProjectServiceBuilder("test", new FileInfo("test.csproj"), ServiceSource.Configuration);
        var act = () => ProjectReader.ReadProjectDetails(output, project, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadProjectDetails_TargetFrameworks_Parsed()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var metadataContent = @"Version:1.0.0
TargetFramework:net8.0
TargetFrameworks:net6.0;net7.0;net8.0
AssemblyName:MultiTfm
TargetPath:bin\Debug\net8.0\MultiTfm.dll
PublishDir:bin\Debug\net8.0\publish\
IntermediateOutputPath:obj\Debug\net8.0\";

            var metadataFile = Path.Combine(tempDir, "metadata.txt");
            File.WriteAllText(metadataFile, metadataContent);

            var projectFile = new FileInfo(Path.Combine(tempDir, "MultiTfm.csproj"));
            File.WriteAllText(projectFile.FullName, "<Project />");

            var project = new DotnetProjectServiceBuilder("multitfm", projectFile, ServiceSource.Configuration);

            var output = new OutputContext(new TestConsole(), Verbosity.Debug);

            ProjectReader.ReadProjectDetails(output, project, metadataFile);

            project.TargetFrameworks.Should().HaveCount(3);
            project.TargetFrameworks.Should().Contain("net6.0");
            project.TargetFrameworks.Should().Contain("net8.0");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
