using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Tye2.Core.MsBuild;
using Xunit;

namespace Tye2.UnitTests;

public class SolutionFileTests
{
    private static readonly string TestAssetsRoot = Path.GetFullPath(
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Tye2.E2ETests", "testassets", "projects"));

    private static string GetSlnPath(string project, string slnName)
        => Path.Combine(TestAssetsRoot, project, slnName);

    // --- Parse single project ---

    [Fact]
    public void Parse_SingleProject_ReturnsOneProject()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        sln.ProjectsInOrder.Should().HaveCount(1);
        sln.ProjectsInOrder[0].ProjectName.Should().Be("test-project");
    }

    [Fact]
    public void Parse_SingleProject_ProjectGuidPopulated()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        sln.ProjectsInOrder[0].ProjectGuid.Should().Be("{7D3606B2-7B8E-4ABB-BE0A-E0B18285D8F5}");
    }

    [Fact]
    public void Parse_SingleProject_RelativePathIsCorrect()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        sln.ProjectsInOrder[0].RelativePath.Should().Contain("test-project.csproj");
    }

    [Fact]
    public void Parse_SingleProject_ProjectTypeIsMSBuild()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        sln.ProjectsInOrder[0].ProjectType.Should().Be(SolutionProjectType.KnownToBeMSBuildFormat);
    }

    // --- Parse multi-project ---

    [Fact]
    public void Parse_FrontendBackend_ReturnsTwoProjects()
    {
        var sln = SolutionFile.Parse(GetSlnPath("frontend-backend", "frontend-backend.sln"));

        sln.ProjectsInOrder.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_FrontendBackend_ProjectNamesCorrect()
    {
        var sln = SolutionFile.Parse(GetSlnPath("frontend-backend", "frontend-backend.sln"));

        var names = sln.ProjectsInOrder.Select(p => p.ProjectName).ToList();
        names.Should().Contain("backend");
        names.Should().Contain("frontend");
    }

    [Fact]
    public void Parse_MultiProject_ReturnsThreeProjects()
    {
        var sln = SolutionFile.Parse(GetSlnPath("multi-project", "multi-project.sln"));

        sln.ProjectsInOrder.Should().HaveCount(3);
        var names = sln.ProjectsInOrder.Select(p => p.ProjectName).ToList();
        names.Should().Contain("backend");
        names.Should().Contain("frontend");
        names.Should().Contain("worker");
    }

    [Fact]
    public void Parse_ProjectTypes_ThreeProjects()
    {
        var sln = SolutionFile.Parse(GetSlnPath("project-types", "project-types.sln"));

        sln.ProjectsInOrder.Should().HaveCount(3);
        var names = sln.ProjectsInOrder.Select(p => p.ProjectName).ToList();
        names.Should().Contain("app");
        names.Should().Contain("test-project");
        names.Should().Contain("class-library");
    }

    // --- ProjectsByGuid ---

    [Fact]
    public void Parse_ProjectsByGuid_LookupWorks()
    {
        var sln = SolutionFile.Parse(GetSlnPath("frontend-backend", "frontend-backend.sln"));

        sln.ProjectsByGuid.Should().ContainKey("{E900C6D9-7A87-49E3-93E5-97E6402E3939}");
        sln.ProjectsByGuid["{E900C6D9-7A87-49E3-93E5-97E6402E3939}"].ProjectName.Should().Be("backend");
    }

    // --- Solution configurations ---

    [Fact]
    public void Parse_SolutionConfigurations_Populated()
    {
        var sln = SolutionFile.Parse(GetSlnPath("frontend-backend", "frontend-backend.sln"));

        sln.SolutionConfigurations.Should().NotBeEmpty();
        var configNames = sln.SolutionConfigurations
            .Select(c => c.FullName)
            .ToList();
        configNames.Should().Contain("Debug|Any CPU");
        configNames.Should().Contain("Release|Any CPU");
    }

    [Fact]
    public void Parse_SolutionConfigurations_SixConfigs()
    {
        var sln = SolutionFile.Parse(GetSlnPath("frontend-backend", "frontend-backend.sln"));

        // Debug|Any CPU, Debug|x64, Debug|x86, Release|Any CPU, Release|x64, Release|x86
        sln.SolutionConfigurations.Should().HaveCount(6);
    }

    // --- Project configurations ---

    [Fact]
    public void Parse_ProjectConfigurations_Populated()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        var project = sln.ProjectsInOrder[0];
        project.ProjectConfigurations.Should().NotBeEmpty();
        project.ProjectConfigurations.Should().ContainKey("Debug|Any CPU");
    }

    [Fact]
    public void Parse_ProjectConfiguration_HasBuildFlag()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        var project = sln.ProjectsInOrder[0];
        var debugConfig = project.ProjectConfigurations["Debug|Any CPU"];
        debugConfig.IncludeInBuild.Should().BeTrue();
    }

    // --- Version ---

    [Fact]
    public void Parse_Version_IsFormat12()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        sln.Version.Should().Be(12);
    }

    [Fact]
    public void Parse_VisualStudioVersion_Is15()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        sln.VisualStudioVersion.Should().Be(15);
    }

    // --- ProjectShouldBuild ---

    [Fact]
    public void ProjectShouldBuild_NoFilter_AlwaysTrue()
    {
        var sln = SolutionFile.Parse(GetSlnPath("frontend-backend", "frontend-backend.sln"));

        // No solution filter, all projects should build
        foreach (var project in sln.ProjectsInOrder)
        {
            sln.ProjectShouldBuild(project.RelativePath).Should().BeTrue();
        }
    }

    // --- IsBuildableProject ---

    [Fact]
    public void IsBuildableProject_MSBuildProject_ReturnsTrue()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        var project = sln.ProjectsInOrder[0];
        project.ProjectType.Should().Be(SolutionProjectType.KnownToBeMSBuildFormat);
        SolutionFile.IsBuildableProject(project).Should().BeTrue();
    }

    // --- Solution with solution folder (create temp file) ---

    [Fact]
    public void Parse_SolutionFolder_IdentifiedCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""src"", ""src"", ""{A1B2C3D4-1234-5678-9012-ABCDEF012345}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""MyApp"", ""src\MyApp\MyApp.csproj"", ""{11111111-2222-3333-4444-555555555555}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{11111111-2222-3333-4444-555555555555}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{11111111-2222-3333-4444-555555555555}.Debug|Any CPU.Build.0 = Debug|Any CPU
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
		{11111111-2222-3333-4444-555555555555} = {A1B2C3D4-1234-5678-9012-ABCDEF012345}
	EndGlobalSection
EndGlobal";
            var slnPath = Path.Combine(tempDir, "test.sln");
            File.WriteAllText(slnPath, slnContent);

            var sln = SolutionFile.Parse(slnPath);

            sln.ProjectsInOrder.Should().HaveCount(2);

            var folder = sln.ProjectsInOrder.First(p => p.ProjectName == "src");
            folder.ProjectType.Should().Be(SolutionProjectType.SolutionFolder);
            SolutionFile.IsBuildableProject(folder).Should().BeFalse();

            var app = sln.ProjectsInOrder.First(p => p.ProjectName == "MyApp");
            app.ProjectType.Should().Be(SolutionProjectType.KnownToBeMSBuildFormat);
            app.ParentProjectGuid.Should().Be("{A1B2C3D4-1234-5678-9012-ABCDEF012345}");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Parse_NestedProjects_ParentGuidSet()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""src"", ""src"", ""{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Web"", ""src\Web\Web.csproj"", ""{11111111-1111-1111-1111-111111111111}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Api"", ""src\Api\Api.csproj"", ""{22222222-2222-2222-2222-222222222222}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
		{11111111-1111-1111-1111-111111111111} = {AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}
		{22222222-2222-2222-2222-222222222222} = {AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}
	EndGlobalSection
EndGlobal";
            var slnPath = Path.Combine(tempDir, "nested.sln");
            File.WriteAllText(slnPath, slnContent);

            var sln = SolutionFile.Parse(slnPath);

            var web = sln.ProjectsInOrder.First(p => p.ProjectName == "Web");
            var api = sln.ProjectsInOrder.First(p => p.ProjectName == "Api");

            web.ParentProjectGuid.Should().Be("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}");
            api.ParentProjectGuid.Should().Be("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // --- F# project type detection ---

    [Fact]
    public void Parse_FSharpProject_DetectedAsMSBuild()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{F2A71F9B-5D33-465A-A702-920D77279786}"") = ""FSharpApp"", ""FSharpApp\FSharpApp.fsproj"", ""{AAAAAAAA-1111-2222-3333-444444444444}""
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
            var slnPath = Path.Combine(tempDir, "fsharp.sln");
            File.WriteAllText(slnPath, slnContent);

            var sln = SolutionFile.Parse(slnPath);

            sln.ProjectsInOrder.Should().HaveCount(1);
            sln.ProjectsInOrder[0].ProjectType.Should().Be(SolutionProjectType.KnownToBeMSBuildFormat);
            sln.ProjectsInOrder[0].ProjectName.Should().Be("FSharpApp");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // --- Mixed project types ---

    [Fact]
    public void Parse_MixedProjectTypes_AllDetectedCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""CSharpApp"", ""CSharpApp\CSharpApp.csproj"", ""{11111111-1111-1111-1111-111111111111}""
EndProject
Project(""{F2A71F9B-5D33-465A-A702-920D77279786}"") = ""FSharpLib"", ""FSharpLib\FSharpLib.fsproj"", ""{22222222-2222-2222-2222-222222222222}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""Solution Items"", ""Solution Items"", ""{33333333-3333-3333-3333-333333333333}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
	EndGlobalSection
EndGlobal";
            var slnPath = Path.Combine(tempDir, "mixed.sln");
            File.WriteAllText(slnPath, slnContent);

            var sln = SolutionFile.Parse(slnPath);

            sln.ProjectsInOrder.Should().HaveCount(3);

            var csharp = sln.ProjectsInOrder.First(p => p.ProjectName == "CSharpApp");
            csharp.ProjectType.Should().Be(SolutionProjectType.KnownToBeMSBuildFormat);

            var fsharp = sln.ProjectsInOrder.First(p => p.ProjectName == "FSharpLib");
            fsharp.ProjectType.Should().Be(SolutionProjectType.KnownToBeMSBuildFormat);

            var folder = sln.ProjectsInOrder.First(p => p.ProjectName == "Solution Items");
            folder.ProjectType.Should().Be(SolutionProjectType.SolutionFolder);

            // Only 2 buildable projects (folder is not buildable)
            sln.ProjectsInOrder.Count(p => SolutionFile.IsBuildableProject(p)).Should().Be(2);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // --- AbsolutePath ---

    [Fact]
    public void Parse_AbsolutePath_IsRooted()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        var project = sln.ProjectsInOrder[0];
        Path.IsPathRooted(project.AbsolutePath).Should().BeTrue();
    }

    [Fact]
    public void Parse_AbsolutePath_EndsWithCsproj()
    {
        var sln = SolutionFile.Parse(GetSlnPath("single-project", "single-project.sln"));

        var project = sln.ProjectsInOrder[0];
        project.AbsolutePath.Should().EndWith("test-project.csproj");
    }

    // --- Error handling ---

    [Fact]
    public void Parse_NonExistentFile_Throws()
    {
        var act = () => SolutionFile.Parse(Path.Combine(Path.GetTempPath(), "nonexistent.sln"));

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_InvalidContent_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var slnPath = Path.Combine(tempDir, "invalid.sln");
            File.WriteAllText(slnPath, "this is not a valid solution file");

            var act = () => SolutionFile.Parse(slnPath);

            act.Should().Throw<Exception>();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // --- Solution filter (.slnf) ---

    [Fact]
    public void Parse_SolutionFilter_FiltersProjects()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            // Create a real .sln file
            var slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 16
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""AppA"", ""AppA\AppA.csproj"", ""{11111111-1111-1111-1111-111111111111}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""AppB"", ""AppB\AppB.csproj"", ""{22222222-2222-2222-2222-222222222222}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
	EndGlobalSection
EndGlobal";
            var slnPath = Path.Combine(tempDir, "test.sln");
            File.WriteAllText(slnPath, slnContent);

            // Create a .slnf that only includes AppA
            var slnfContent = @"{
  ""solution"": {
    ""path"": ""test.sln"",
    ""projects"": [
      ""AppA\\AppA.csproj""
    ]
  }
}";
            var slnfPath = Path.Combine(tempDir, "test.slnf");
            File.WriteAllText(slnfPath, slnfContent);

            var sln = SolutionFile.Parse(slnfPath);

            // All projects are still parsed
            sln.ProjectsInOrder.Should().HaveCount(2);

            // But the filter controls which should build
            sln.ProjectShouldBuild("AppA\\AppA.csproj").Should().BeTrue();
            sln.ProjectShouldBuild("AppB\\AppB.csproj").Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // --- Azure functions solution ---

    [Fact]
    public void Parse_AzureFunctions_ProjectsDetected()
    {
        var sln = SolutionFile.Parse(GetSlnPath("azure-functions", "frontend-backend.sln"));

        sln.ProjectsInOrder.Should().HaveCountGreaterThanOrEqualTo(2);
        var names = sln.ProjectsInOrder.Select(p => p.ProjectName).ToList();
        names.Should().Contain("frontend");
        names.Should().Contain("backend");
    }

    // --- Project order preserved ---

    [Fact]
    public void Parse_ProjectOrder_MatchesSlnOrder()
    {
        var sln = SolutionFile.Parse(GetSlnPath("multi-project", "multi-project.sln"));

        // multi-project.sln has: backend, frontend, worker in that order
        sln.ProjectsInOrder[0].ProjectName.Should().Be("backend");
        sln.ProjectsInOrder[1].ProjectName.Should().Be("frontend");
        sln.ProjectsInOrder[2].ProjectName.Should().Be("worker");
    }
}
