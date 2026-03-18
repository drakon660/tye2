using AwesomeAssertions;
using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests
{
    public class DockerfileGeneratorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly OutputContext _output;
        private readonly FileInfo _dummySource;

        public DockerfileGeneratorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tye2_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _output = new OutputContext(new TestConsole(), Verbosity.Debug);
            _dummySource = new FileInfo(Path.Combine(_tempDir, "tye.yaml"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private ApplicationBuilder CreateApp(ContainerRegistry? registry = null)
        {
            return new ApplicationBuilder(_dummySource, "test-app", new ContainerEngine(null), null)
            {
                Registry = registry,
            };
        }

        private DotnetProjectServiceBuilder CreateProject(
            string name = "myservice",
            string assemblyName = "MyService",
            string targetFrameworkName = "net",
            string targetFrameworkVersion = "8.0",
            string targetFramework = "net8.0",
            bool isAspNet = true,
            string? version = null,
            string? args = null)
        {
            var projectFile = new FileInfo(Path.Combine(_tempDir, $"{name}.csproj"));
            File.WriteAllText(projectFile.FullName, "<Project />");
            return new DotnetProjectServiceBuilder(name, projectFile, ServiceSource.Configuration)
            {
                AssemblyName = assemblyName,
                TargetFrameworkName = targetFrameworkName,
                TargetFrameworkVersion = targetFrameworkVersion,
                TargetFramework = targetFramework,
                IsAspNet = isAspNet,
                Version = version ?? null!,
                Args = args,
            };
        }

        // =====================================================================
        // TagIs50OrNewer
        // =====================================================================

        [Theory]
        [InlineData("latest", true)]
        [InlineData("5.0", true)]
        [InlineData("6.0", true)]
        [InlineData("7.0", true)]
        [InlineData("8.0", true)]
        [InlineData("9.0", true)]
        [InlineData("3.1", false)]
        [InlineData("2.1", false)]
        [InlineData("1.0", false)]
        public void TagIs50OrNewer_ReturnsExpectedResult(string tag, bool expected)
        {
            DockerfileGenerator.TagIs50OrNewer(tag).Should().Be(expected);
        }

        [Fact]
        public void TagIs50OrNewer_InvalidTag_ThrowsCommandException()
        {
            var ex = ((Action)(() => DockerfileGenerator.TagIs50OrNewer("not-a-version"))).Should().Throw<CommandException>().Which;
            ex.Message.Should().Contain("not-a-version");
        }

        // =====================================================================
        // ApplyContainerDefaults (DotnetProjectServiceBuilder) - Null Checks
        // =====================================================================

        [Fact]
        public void ApplyContainerDefaults_NullApplication_ThrowsArgumentNullException()
        {
            var project = CreateProject();
            var container = new ContainerInfo();

            var ex = ((Action)(() => DockerfileGenerator.ApplyContainerDefaults(null!, project, container))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("application");
        }

        [Fact]
        public void ApplyContainerDefaults_NullProject_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var container = new ContainerInfo();

            var ex = ((Action)(() => DockerfileGenerator.ApplyContainerDefaults(app, (DotnetProjectServiceBuilder)null!, container))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("project");
        }

        [Fact]
        public void ApplyContainerDefaults_NullContainer_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var project = CreateProject();

            var ex = ((Action)(() => DockerfileGenerator.ApplyContainerDefaults(app, project, null!))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("container");
        }

        // =====================================================================
        // ApplyContainerDefaults - BaseImageTag
        // =====================================================================

        [Fact]
        public void ApplyContainerDefaults_NetCoreApp_SetsBaseImageTagFromVersion()
        {
            var app = CreateApp();
            var project = CreateProject(targetFrameworkName: "netcoreapp", targetFrameworkVersion: "3.1");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BaseImageTag.Should().Be("3.1");
        }

        [Fact]
        public void ApplyContainerDefaults_Net_SetsBaseImageTagFromVersion()
        {
            var app = CreateApp();
            var project = CreateProject(targetFrameworkName: "net", targetFrameworkVersion: "8.0");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BaseImageTag.Should().Be("8.0");
        }

        [Fact]
        public void ApplyContainerDefaults_PresetBaseImageTag_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject(targetFrameworkName: "net", targetFrameworkVersion: "8.0");
            var container = new ContainerInfo { BaseImageTag = "7.0" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BaseImageTag.Should().Be("7.0");
        }

        [Fact]
        public void ApplyContainerDefaults_UnsupportedTFM_ThrowsCommandException()
        {
            var app = CreateApp();
            var project = CreateProject(
                targetFrameworkName: "netstandard",
                targetFrameworkVersion: "2.0",
                targetFramework: "netstandard2.0");
            var container = new ContainerInfo();

            var ex = ((Action)(() => DockerfileGenerator.ApplyContainerDefaults(app, project, container))).Should().Throw<CommandException>().Which;
            ex.Message.Should().Contain("netstandard2.0");
        }

        // =====================================================================
        // ApplyContainerDefaults - BaseImageName
        // =====================================================================

        [Theory]
        [InlineData("8.0", true, "mcr.microsoft.com/dotnet/aspnet")]
        [InlineData("8.0", false, "mcr.microsoft.com/dotnet/runtime")]
        [InlineData("5.0", true, "mcr.microsoft.com/dotnet/aspnet")]
        [InlineData("5.0", false, "mcr.microsoft.com/dotnet/runtime")]
        [InlineData("3.1", true, "mcr.microsoft.com/dotnet/core/aspnet")]
        [InlineData("3.1", false, "mcr.microsoft.com/dotnet/core/runtime")]
        public void ApplyContainerDefaults_SetsCorrectBaseImageName(string version, bool isAspNet, string expectedImage)
        {
            var app = CreateApp();
            var project = CreateProject(
                targetFrameworkName: version == "3.1" ? "netcoreapp" : "net",
                targetFrameworkVersion: version,
                isAspNet: isAspNet);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BaseImageName.Should().Be(expectedImage);
        }

        [Fact]
        public void ApplyContainerDefaults_PresetBaseImageName_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo { BaseImageName = "my-custom-image" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BaseImageName.Should().Be("my-custom-image");
        }

        // =====================================================================
        // ApplyContainerDefaults - BuildImageName / BuildImageTag
        // =====================================================================

        [Theory]
        [InlineData("8.0", "mcr.microsoft.com/dotnet/sdk")]
        [InlineData("5.0", "mcr.microsoft.com/dotnet/sdk")]
        [InlineData("3.1", "mcr.microsoft.com/dotnet/core/sdk")]
        public void ApplyContainerDefaults_SetsCorrectBuildImageName(string version, string expectedSdk)
        {
            var app = CreateApp();
            var project = CreateProject(
                targetFrameworkName: version == "3.1" ? "netcoreapp" : "net",
                targetFrameworkVersion: version);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BuildImageName.Should().Be(expectedSdk);
        }

        [Fact]
        public void ApplyContainerDefaults_SetsBuildImageTagFromVersion()
        {
            var app = CreateApp();
            var project = CreateProject(targetFrameworkVersion: "8.0");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BuildImageTag.Should().Be("8.0");
        }

        [Fact]
        public void ApplyContainerDefaults_PresetBuildImageTag_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject(targetFrameworkVersion: "8.0");
            var container = new ContainerInfo { BuildImageTag = "7.0" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BuildImageTag.Should().Be("7.0");
        }

        [Fact]
        public void ApplyContainerDefaults_PresetBuildImageName_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo { BuildImageName = "my-sdk" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BuildImageName.Should().Be("my-sdk");
        }

        // =====================================================================
        // ApplyContainerDefaults - ImageName / ImageTag
        // =====================================================================

        [Fact]
        public void ApplyContainerDefaults_NoRegistry_SetsImageNameFromProjectName()
        {
            var app = CreateApp();
            var project = CreateProject(name: "MyService");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.ImageName.Should().Be("myservice");
        }

        [Fact]
        public void ApplyContainerDefaults_WithRegistry_PrefixesImageName()
        {
            var registry = new ContainerRegistry("myregistry.azurecr.io", null);
            var app = CreateApp(registry: registry);
            var project = CreateProject(name: "MyService");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.ImageName.Should().Be("myregistry.azurecr.io/myservice");
        }

        [Fact]
        public void ApplyContainerDefaults_PresetImageName_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo { ImageName = "custom-name" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.ImageName.Should().Be("custom-name");
        }

        [Fact]
        public void ApplyContainerDefaults_WithVersion_SetsImageTag()
        {
            var app = CreateApp();
            var project = CreateProject(version: "1.2.3");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.ImageTag.Should().Be("1.2.3");
        }

        [Fact]
        public void ApplyContainerDefaults_VersionWithPlus_ReplacedWithHyphen()
        {
            var app = CreateApp();
            var project = CreateProject(version: "1.0.0+build123");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.ImageTag.Should().Be("1.0.0-build123");
        }

        [Fact]
        public void ApplyContainerDefaults_NullVersion_DefaultsToLatest()
        {
            var app = CreateApp();
            var project = CreateProject(version: null);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.ImageTag.Should().Be("latest");
        }

        [Fact]
        public void ApplyContainerDefaults_PresetImageTag_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject(version: "1.0.0");
            var container = new ContainerInfo { ImageTag = "my-tag" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.ImageTag.Should().Be("my-tag");
        }

        // =====================================================================
        // ApplyContainerDefaults - Environment Variable
        // =====================================================================

        [Fact]
        public void ApplyContainerDefaults_AddsDisableColorsEnvVar()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            var envVar = project.EnvironmentVariables.Should().ContainSingle(e => e.Name == "DOTNET_LOGGING__CONSOLE__DISABLECOLORS").Subject;
            envVar.Value.Should().Be("true");
        }

        // =====================================================================
        // ApplyContainerDefaults - All Preset Values
        // =====================================================================

        [Fact]
        public void ApplyContainerDefaults_AllPreset_NothingOverwritten()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo
            {
                BaseImageName = "custom-base",
                BaseImageTag = "7.0",
                BuildImageName = "custom-sdk",
                BuildImageTag = "6.0",
                ImageName = "custom-image",
                ImageTag = "custom-tag",
            };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BaseImageName.Should().Be("custom-base");
            container.BaseImageTag.Should().Be("7.0");
            container.BuildImageName.Should().Be("custom-sdk");
            container.BuildImageTag.Should().Be("6.0");
            container.ImageName.Should().Be("custom-image");
            container.ImageTag.Should().Be("custom-tag");
        }

        // =====================================================================
        // ApplyContainerDefaults (DockerFileServiceBuilder)
        // =====================================================================

        [Fact]
        public void ApplyContainerDefaults_DockerFile_NoRegistry_SetsImageName()
        {
            var app = CreateApp();
            var dockerSvc = new DockerFileServiceBuilder("MyApp", "myapp", ServiceSource.Configuration);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, dockerSvc, container);

            container.ImageName.Should().Be("myapp");
        }

        [Fact]
        public void ApplyContainerDefaults_DockerFile_WithRegistry_PrefixesImageName()
        {
            var registry = new ContainerRegistry("registry.io", null);
            var app = CreateApp(registry: registry);
            var dockerSvc = new DockerFileServiceBuilder("MyApp", "myapp", ServiceSource.Configuration);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, dockerSvc, container);

            container.ImageName.Should().Be("registry.io/myapp");
        }

        [Fact]
        public void ApplyContainerDefaults_DockerFile_DefaultsImageTagToLatest()
        {
            var app = CreateApp();
            var dockerSvc = new DockerFileServiceBuilder("MyApp", "myapp", ServiceSource.Configuration);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, dockerSvc, container);

            container.ImageTag.Should().Be("latest");
        }

        [Fact]
        public void ApplyContainerDefaults_DockerFile_PresetImageTag_DoesNotOverwrite()
        {
            var app = CreateApp();
            var dockerSvc = new DockerFileServiceBuilder("MyApp", "myapp", ServiceSource.Configuration);
            var container = new ContainerInfo { ImageTag = "v2" };

            DockerfileGenerator.ApplyContainerDefaults(app, dockerSvc, container);

            container.ImageTag.Should().Be("v2");
        }

        [Fact]
        public void ApplyContainerDefaults_DockerFile_PresetImageName_DoesNotOverwrite()
        {
            var app = CreateApp();
            var dockerSvc = new DockerFileServiceBuilder("MyApp", "myapp", ServiceSource.Configuration);
            var container = new ContainerInfo { ImageName = "custom" };

            DockerfileGenerator.ApplyContainerDefaults(app, dockerSvc, container);

            container.ImageName.Should().Be("custom");
        }

        // =====================================================================
        // WriteDockerfileAsync - Null Checks
        // =====================================================================

        [Fact]
        public async Task WriteDockerfileAsync_NullOutput_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo();

            var ex = (await ((Func<Task>)(() => DockerfileGenerator.WriteDockerfileAsync(null!, app, project, container, "file.txt"))).Should().ThrowAsync<ArgumentNullException>()).Which;
            ex.ParamName.Should().Be("output");
        }

        [Fact]
        public async Task WriteDockerfileAsync_NullApplication_ThrowsArgumentNullException()
        {
            var project = CreateProject();
            var container = new ContainerInfo();

            var ex = (await ((Func<Task>)(() => DockerfileGenerator.WriteDockerfileAsync(_output, null!, project, container, "file.txt"))).Should().ThrowAsync<ArgumentNullException>()).Which;
            ex.ParamName.Should().Be("application");
        }

        [Fact]
        public async Task WriteDockerfileAsync_NullProject_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var container = new ContainerInfo();

            var ex = (await ((Func<Task>)(() => DockerfileGenerator.WriteDockerfileAsync(_output, app, null!, container, "file.txt"))).Should().ThrowAsync<ArgumentNullException>()).Which;
            ex.ParamName.Should().Be("project");
        }

        [Fact]
        public async Task WriteDockerfileAsync_NullContainer_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var project = CreateProject();

            var ex = (await ((Func<Task>)(() => DockerfileGenerator.WriteDockerfileAsync(_output, app, project, null!, "file.txt"))).Should().ThrowAsync<ArgumentNullException>()).Which;
            ex.ParamName.Should().Be("container");
        }

        [Fact]
        public async Task WriteDockerfileAsync_NullFilePath_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo();

            var ex = (await ((Func<Task>)(() => DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, null!))).Should().ThrowAsync<ArgumentNullException>()).Which;
            ex.ParamName.Should().Be("filePath");
        }

        // =====================================================================
        // WriteDockerfileAsync - Multiphase Dockerfile
        // =====================================================================

        [Fact]
        public async Task WriteDockerfileAsync_Multiphase_GeneratesCorrectDockerfile()
        {
            var app = CreateApp();
            var project = CreateProject(assemblyName: "WebApp");
            var container = new ContainerInfo
            {
                UseMultiphaseDockerfile = true,
                BaseImageName = "mcr.microsoft.com/dotnet/aspnet",
                BaseImageTag = "8.0",
                BuildImageName = "mcr.microsoft.com/dotnet/sdk",
                BuildImageTag = "8.0",
            };
            var filePath = Path.Combine(_tempDir, "Dockerfile");

            await DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, filePath);

            var content = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
            content.Should().Contain("FROM mcr.microsoft.com/dotnet/sdk:8.0 as SDK");
            content.Should().Contain("WORKDIR /src");
            content.Should().Contain("COPY . .");
            content.Should().Contain("RUN dotnet publish -c Release -o /out");
            content.Should().Contain("FROM mcr.microsoft.com/dotnet/aspnet:8.0 as RUNTIME");
            content.Should().Contain("WORKDIR /app");
            content.Should().Contain("COPY --from=SDK /out .");
            content.Should().Contain("ENTRYPOINT [\"dotnet\", \"WebApp.dll\"]");
        }

        [Fact]
        public async Task WriteDockerfileAsync_Multiphase_WithArgs_IncludesCMD()
        {
            var app = CreateApp();
            var project = CreateProject(assemblyName: "WebApp", args: "--urls http://+:80");
            var container = new ContainerInfo
            {
                UseMultiphaseDockerfile = true,
                BaseImageName = "mcr.microsoft.com/dotnet/aspnet",
                BaseImageTag = "8.0",
                BuildImageName = "mcr.microsoft.com/dotnet/sdk",
                BuildImageTag = "8.0",
            };
            var filePath = Path.Combine(_tempDir, "Dockerfile");

            await DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, filePath);

            var content = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
            content.Should().Contain("CMD [\"--urls http://+:80\"]");
        }

        [Fact]
        public async Task WriteDockerfileAsync_Multiphase_WithoutArgs_NoCMD()
        {
            var app = CreateApp();
            var project = CreateProject(assemblyName: "WebApp", args: null);
            var container = new ContainerInfo
            {
                UseMultiphaseDockerfile = true,
                BaseImageName = "mcr.microsoft.com/dotnet/aspnet",
                BaseImageTag = "8.0",
                BuildImageName = "mcr.microsoft.com/dotnet/sdk",
                BuildImageTag = "8.0",
            };
            var filePath = Path.Combine(_tempDir, "Dockerfile");

            await DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, filePath);

            var content = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
            content.Should().NotContain("CMD");
        }

        [Fact]
        public async Task WriteDockerfileAsync_NullUseMultiphase_DefaultsToMultiphase()
        {
            var app = CreateApp();
            var project = CreateProject(assemblyName: "WebApp");
            var container = new ContainerInfo
            {
                UseMultiphaseDockerfile = null,
                BaseImageName = "mcr.microsoft.com/dotnet/aspnet",
                BaseImageTag = "8.0",
                BuildImageName = "mcr.microsoft.com/dotnet/sdk",
                BuildImageTag = "8.0",
            };
            var filePath = Path.Combine(_tempDir, "Dockerfile");

            await DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, filePath);

            var content = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
            content.Should().Contain("as SDK");
            content.Should().Contain("as RUNTIME");
        }

        // =====================================================================
        // WriteDockerfileAsync - Local Publish Dockerfile
        // =====================================================================

        [Fact]
        public async Task WriteDockerfileAsync_LocalPublish_GeneratesCorrectDockerfile()
        {
            var app = CreateApp();
            var project = CreateProject(assemblyName: "WorkerApp");
            var container = new ContainerInfo
            {
                UseMultiphaseDockerfile = false,
                BaseImageName = "mcr.microsoft.com/dotnet/runtime",
                BaseImageTag = "8.0",
            };
            var filePath = Path.Combine(_tempDir, "Dockerfile");

            await DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, filePath);

            var content = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
            content.Should().Contain("FROM mcr.microsoft.com/dotnet/runtime:8.0");
            content.Should().Contain("WORKDIR /app");
            content.Should().Contain("COPY . /app");
            content.Should().Contain("ENTRYPOINT [\"dotnet\", \"WorkerApp.dll\"]");
            content.Should().NotContain("as SDK");
            content.Should().NotContain("as RUNTIME");
        }

        [Fact]
        public async Task WriteDockerfileAsync_LocalPublish_WithArgs_IncludesCMD()
        {
            var app = CreateApp();
            var project = CreateProject(assemblyName: "WorkerApp", args: "--verbose");
            var container = new ContainerInfo
            {
                UseMultiphaseDockerfile = false,
                BaseImageName = "mcr.microsoft.com/dotnet/runtime",
                BaseImageTag = "8.0",
            };
            var filePath = Path.Combine(_tempDir, "Dockerfile");

            await DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, filePath);

            var content = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
            content.Should().Contain("CMD [\"--verbose\"]");
        }

        [Fact]
        public async Task WriteDockerfileAsync_LocalPublish_WithoutArgs_NoCMD()
        {
            var app = CreateApp();
            var project = CreateProject(assemblyName: "WorkerApp", args: null);
            var container = new ContainerInfo
            {
                UseMultiphaseDockerfile = false,
                BaseImageName = "mcr.microsoft.com/dotnet/runtime",
                BaseImageTag = "8.0",
            };
            var filePath = Path.Combine(_tempDir, "Dockerfile");

            await DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, filePath);

            var content = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
            content.Should().NotContain("CMD");
        }

        [Fact]
        public async Task WriteDockerfileAsync_WhitespaceArgs_NoCMD()
        {
            var app = CreateApp();
            var project = CreateProject(assemblyName: "WorkerApp", args: "   ");
            var container = new ContainerInfo
            {
                UseMultiphaseDockerfile = false,
                BaseImageName = "mcr.microsoft.com/dotnet/runtime",
                BaseImageTag = "8.0",
            };
            var filePath = Path.Combine(_tempDir, "Dockerfile");

            await DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, filePath);

            var content = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
            content.Should().NotContain("CMD");
        }

        // =====================================================================
        // WriteDockerfileAsync - File Encoding
        // =====================================================================

        [Fact]
        public async Task WriteDockerfileAsync_WritesUtf8WithoutBOM()
        {
            var app = CreateApp();
            var project = CreateProject(assemblyName: "WebApp");
            var container = new ContainerInfo
            {
                UseMultiphaseDockerfile = false,
                BaseImageName = "mcr.microsoft.com/dotnet/runtime",
                BaseImageTag = "8.0",
            };
            var filePath = Path.Combine(_tempDir, "Dockerfile");

            await DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, filePath);

            var bytes = await File.ReadAllBytesAsync(filePath, TestContext.Current.CancellationToken);
            // UTF8 BOM is 0xEF, 0xBB, 0xBF
            (bytes.Length >= 3).Should().BeTrue("File should have content");
            var hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            hasBom.Should().BeFalse("Dockerfile should not have UTF-8 BOM");
        }

        // =====================================================================
        // ApplyContainerDefaults - Old .NET Core paths
        // =====================================================================

        [Fact]
        public void ApplyContainerDefaults_Net31_AspNet_UsesCorePath()
        {
            var app = CreateApp();
            var project = CreateProject(
                targetFrameworkName: "netcoreapp",
                targetFrameworkVersion: "3.1",
                isAspNet: true);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BaseImageName.Should().Be("mcr.microsoft.com/dotnet/core/aspnet");
            container.BuildImageName.Should().Be("mcr.microsoft.com/dotnet/core/sdk");
        }

        [Fact]
        public void ApplyContainerDefaults_Net60_NonAspNet_UsesRuntimePath()
        {
            var app = CreateApp();
            var project = CreateProject(
                targetFrameworkName: "net",
                targetFrameworkVersion: "6.0",
                isAspNet: false);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            container.BaseImageName.Should().Be("mcr.microsoft.com/dotnet/runtime");
            container.BuildImageName.Should().Be("mcr.microsoft.com/dotnet/sdk");
        }
    }
}






