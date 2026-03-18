using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Core.ConfigModel;
using Xunit;

namespace Tye2.UnitTests
{
    public class ConfigFactoryTests : IDisposable
    {
        private readonly string _tempDir;

        public ConfigFactoryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tye2_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private FileInfo CreateTempFile(string fileName, string content)
        {
            var path = Path.Combine(_tempDir, fileName);
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, content);
            return new FileInfo(path);
        }

        // =====================================================================
        // FromFile — Extension Routing
        // =====================================================================

        [Fact]
        public void FromFile_YamlExtension_ParsesAsTyeYaml()
        {
            var file = CreateTempFile("tye.yaml", @"
name: my-app
services:
  - name: web
    image: nginx
");
            var app = ConfigFactory.FromFile(file);
            app.Name.Should().Be("my-app");
            app.Services.Should().ContainSingle()
                .Which.Name.Should().Be("web");
        }

        [Fact]
        public void FromFile_YmlExtension_ParsesAsTyeYaml()
        {
            var file = CreateTempFile("tye.yml", @"
name: yml-app
services:
  - name: api
    image: myapi
");
            var app = ConfigFactory.FromFile(file);
            app.Name.Should().Be("yml-app");
        }

        [Fact]
        public void FromFile_CsprojExtension_ParsesAsProject()
        {
            var file = CreateTempFile("MyApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().ContainSingle()
                .Which.Name.Should().Be("myapp");
        }

        [Fact]
        public void FromFile_FsprojExtension_ParsesAsProject()
        {
            var file = CreateTempFile("MyFSharpApp.fsproj", @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().ContainSingle()
                .Which.Name.Should().Be("myfsharpapp");
        }

        [Fact]
        public void FromFile_UnsupportedExtension_ThrowsCommandException()
        {
            var file = CreateTempFile("readme.txt", "hello");
            var act = () => ConfigFactory.FromFile(file);
            act.Should().Throw<CommandException>()
                .WithMessage("*readme.txt*not a supported format*");
        }

        [Theory]
        [InlineData(".json")]
        [InlineData(".xml")]
        [InlineData(".toml")]
        [InlineData(".md")]
        public void FromFile_VariousUnsupportedExtensions_ThrowsCommandException(string ext)
        {
            var file = CreateTempFile($"config{ext}", "content");
            var act = () => ConfigFactory.FromFile(file);
            act.Should().Throw<CommandException>();
        }

        // =====================================================================
        // FromFile — Docker Compose Detection
        // =====================================================================

        [Fact]
        public void FromFile_DockerComposeYaml_ParsesAsDockerCompose()
        {
            var file = CreateTempFile("docker-compose.yaml", @"
version: '3'
services:
  redis:
    image: redis:latest
    ports:
      - 6379:6379
");
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().Contain(s => s.Name == "redis");
        }

        [Fact]
        public void FromFile_DockerComposeYml_ParsesAsDockerCompose()
        {
            var file = CreateTempFile("docker-compose.yml", @"
version: '3'
services:
  postgres:
    image: postgres
    ports:
      - 5432:5432
");
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().Contain(s => s.Name == "postgres");
        }

        [Fact]
        public void FromFile_DockerCompose_CaseInsensitiveDetection()
        {
            var file = CreateTempFile("Docker-Compose.yaml", @"
version: '3'
services:
  app:
    image: myapp
");
            var app = ConfigFactory.FromFile(file);
            app.Should().NotBeNull();
        }

        [Fact]
        public void FromFile_DockerComposeInName_ParsesAsDockerCompose()
        {
            var file = CreateTempFile("my-docker-compose-dev.yaml", @"
version: '3'
services:
  web:
    image: nginx
");
            var app = ConfigFactory.FromFile(file);
            app.Should().NotBeNull();
        }

        [Fact]
        public void FromFile_NonDockerComposeYaml_ParsesAsTyeYaml()
        {
            var file = CreateTempFile("my-services.yaml", @"
name: not-compose
services:
  - name: svc
    image: myimage
");
            var app = ConfigFactory.FromFile(file);
            app.Name.Should().Be("not-compose");
        }

        // =====================================================================
        // FromProject — .csproj / .fsproj
        // =====================================================================

        [Fact]
        public void FromProject_SetsSourceToFile()
        {
            var file = CreateTempFile("WebApp.csproj", @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");
            var app = ConfigFactory.FromFile(file);
            app.Source.FullName.Should().Be(file.FullName);
        }

        [Fact]
        public void FromProject_InfersApplicationName()
        {
            var file = CreateTempFile("WebApp.csproj", "<Project />");
            var app = ConfigFactory.FromFile(file);
            app.Name.Should().Be("webapp");
        }

        [Fact]
        public void FromProject_ServiceNameIsNormalized()
        {
            var file = CreateTempFile("My.Web.App.csproj", "<Project />");
            var app = ConfigFactory.FromFile(file);
            app.Services.Single().Name.Should().Be("my-web-app");
        }

        [Fact]
        public void FromProject_ServiceProjectPathUsesForwardSlashes()
        {
            var file = CreateTempFile("App.csproj", "<Project />");
            var app = ConfigFactory.FromFile(file);
            app.Services.Single().Project.Should().NotContain("\\");
        }

        [Fact]
        public void FromProject_CreatesExactlyOneService()
        {
            var file = CreateTempFile("SingleService.csproj", "<Project />");
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().ContainSingle();
        }

        // =====================================================================
        // FromSolution — .sln
        // =====================================================================

        [Fact]
        public void FromSolution_SetsSourceToFile()
        {
            var file = CreateSolutionWithProject("TestSln", hasLaunchSettings: true);
            var app = ConfigFactory.FromFile(file);
            app.Source.FullName.Should().Be(file.FullName);
        }

        [Fact]
        public void FromSolution_InfersApplicationName()
        {
            var file = CreateSolutionWithProject("MySolution", hasLaunchSettings: true);
            var app = ConfigFactory.FromFile(file);
            app.Name.Should().Be("mysolution");
        }

        [Fact]
        public void FromSolution_IncludesProjectWithLaunchSettings()
        {
            var file = CreateSolutionWithProject("WithLaunch", hasLaunchSettings: true);
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().ContainSingle();
        }

        [Fact]
        public void FromSolution_IncludesProjectWithOutputTypeExe()
        {
            var file = CreateSolutionWithProject("WithExe", hasLaunchSettings: false, outputTypeExe: true);
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().ContainSingle();
        }

        [Fact]
        public void FromSolution_ExcludesClassLibraryWithoutLaunchSettings()
        {
            var file = CreateSolutionWithProject("LibOnly", hasLaunchSettings: false, outputTypeExe: false);
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().BeEmpty();
        }

        [Fact]
        public void FromSolution_ServiceNameIsNormalized()
        {
            var file = CreateSolutionWithProject("NormTest", hasLaunchSettings: true, projectName: "My.Web.Api");
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().ContainSingle()
                .Which.Name.Should().Be("my-web-api");
        }

        [Fact]
        public void FromSolution_MultipleProjects_OnlyRunnableIncluded()
        {
            var slnName = "MultiProj";
            var slnDir = Path.Combine(_tempDir, slnName);
            Directory.CreateDirectory(slnDir);

            // Runnable project (has launchSettings)
            var runnableDir = Path.Combine(slnDir, "WebApp");
            Directory.CreateDirectory(runnableDir);
            File.WriteAllText(Path.Combine(runnableDir, "WebApp.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
            var propsDir = Path.Combine(runnableDir, "Properties");
            Directory.CreateDirectory(propsDir);
            File.WriteAllText(Path.Combine(propsDir, "launchSettings.json"), "{}");

            // Console app (has OutputType exe)
            var consoleDir = Path.Combine(slnDir, "Worker");
            Directory.CreateDirectory(consoleDir);
            File.WriteAllText(Path.Combine(consoleDir, "Worker.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>exe</OutputType></PropertyGroup></Project>");

            // Class library (should be excluded)
            var libDir = Path.Combine(slnDir, "SharedLib");
            Directory.CreateDirectory(libDir);
            File.WriteAllText(Path.Combine(libDir, "SharedLib.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

            var slnContent = GenerateSlnContent(new[]
            {
                ("WebApp", "WebApp/WebApp.csproj"),
                ("Worker", "Worker/Worker.csproj"),
                ("SharedLib", "SharedLib/SharedLib.csproj"),
            });
            var slnFile = Path.Combine(slnDir, $"{slnName}.sln");
            File.WriteAllText(slnFile, slnContent);

            var app = ConfigFactory.FromFile(new FileInfo(slnFile));
            app.Services.Should().HaveCount(2);
            app.Services.Select(s => s.Name).Should().BeEquivalentTo("webapp", "worker");
        }

        // =====================================================================
        // NormalizeServiceName (tested indirectly via FromProject)
        // =====================================================================

        [Theory]
        [InlineData("MyApp", "myapp")]
        [InlineData("My.Web.App", "my-web-app")]
        [InlineData("App_With_Underscores", "app-with-underscores")]
        [InlineData("App With Spaces", "app-with-spaces")]
        [InlineData("UPPER", "upper")]
        [InlineData("already-normalized", "already-normalized")]
        [InlineData("Multi...Dots", "multi-dots")]
        public void NormalizeServiceName_VariousInputs(string projectName, string expectedServiceName)
        {
            var file = CreateTempFile($"{projectName}.csproj", "<Project />");
            var app = ConfigFactory.FromFile(file);
            app.Services.Single().Name.Should().Be(expectedServiceName);
        }

        // =====================================================================
        // Extension Case Insensitivity
        // =====================================================================

        [Fact]
        public void FromFile_ExtensionCaseInsensitive_Yaml()
        {
            var file = CreateTempFile("tye.YAML", "name: upper-yaml");
            var app = ConfigFactory.FromFile(file);
            app.Name.Should().Be("upper-yaml");
        }

        [Fact]
        public void FromFile_ExtensionCaseInsensitive_Csproj()
        {
            var file = CreateTempFile("App.CSPROJ", "<Project />");
            var app = ConfigFactory.FromFile(file);
            app.Services.Should().ContainSingle();
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private FileInfo CreateSolutionWithProject(
            string solutionName,
            bool hasLaunchSettings,
            bool outputTypeExe = false,
            string projectName = "TestProject")
        {
            var slnDir = Path.Combine(_tempDir, solutionName);
            Directory.CreateDirectory(slnDir);

            var projectDir = Path.Combine(slnDir, projectName);
            Directory.CreateDirectory(projectDir);

            var outputTypeTag = outputTypeExe ? "<OutputType>exe</OutputType>" : "";
            var csprojContent = $"<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net8.0</TargetFramework>{outputTypeTag}</PropertyGroup></Project>";
            File.WriteAllText(Path.Combine(projectDir, $"{projectName}.csproj"), csprojContent);

            if (hasLaunchSettings)
            {
                var propsDir = Path.Combine(projectDir, "Properties");
                Directory.CreateDirectory(propsDir);
                File.WriteAllText(Path.Combine(propsDir, "launchSettings.json"), "{}");
            }

            var slnContent = GenerateSlnContent(new[] { (projectName, $"{projectName}/{projectName}.csproj") });
            var slnPath = Path.Combine(slnDir, $"{solutionName}.sln");
            File.WriteAllText(slnPath, slnContent);

            return new FileInfo(slnPath);
        }

        private static string GenerateSlnContent((string name, string path)[] projects)
        {
            var projectEntries = string.Join(Environment.NewLine, projects.Select(p =>
                $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{p.name}\", \"{p.path}\", \"{{{Guid.NewGuid()}}}\"\nEndProject"));

            return $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
{projectEntries}
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
EndGlobal
";
        }
    }
}
