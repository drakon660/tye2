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
                Version = version,
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
            Assert.Equal(expected, DockerfileGenerator.TagIs50OrNewer(tag));
        }

        [Fact]
        public void TagIs50OrNewer_InvalidTag_ThrowsCommandException()
        {
            var ex = Assert.Throws<CommandException>(() => DockerfileGenerator.TagIs50OrNewer("not-a-version"));
            Assert.Contains("not-a-version", ex.Message);
        }

        // =====================================================================
        // ApplyContainerDefaults (DotnetProjectServiceBuilder) - Null Checks
        // =====================================================================

        [Fact]
        public void ApplyContainerDefaults_NullApplication_ThrowsArgumentNullException()
        {
            var project = CreateProject();
            var container = new ContainerInfo();

            var ex = Assert.Throws<ArgumentNullException>(() =>
                DockerfileGenerator.ApplyContainerDefaults(null!, project, container));
            Assert.Equal("application", ex.ParamName);
        }

        [Fact]
        public void ApplyContainerDefaults_NullProject_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var container = new ContainerInfo();

            var ex = Assert.Throws<ArgumentNullException>(() =>
                DockerfileGenerator.ApplyContainerDefaults(app, (DotnetProjectServiceBuilder)null!, container));
            Assert.Equal("project", ex.ParamName);
        }

        [Fact]
        public void ApplyContainerDefaults_NullContainer_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var project = CreateProject();

            var ex = Assert.Throws<ArgumentNullException>(() =>
                DockerfileGenerator.ApplyContainerDefaults(app, project, null!));
            Assert.Equal("container", ex.ParamName);
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

            Assert.Equal("3.1", container.BaseImageTag);
        }

        [Fact]
        public void ApplyContainerDefaults_Net_SetsBaseImageTagFromVersion()
        {
            var app = CreateApp();
            var project = CreateProject(targetFrameworkName: "net", targetFrameworkVersion: "8.0");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("8.0", container.BaseImageTag);
        }

        [Fact]
        public void ApplyContainerDefaults_PresetBaseImageTag_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject(targetFrameworkName: "net", targetFrameworkVersion: "8.0");
            var container = new ContainerInfo { BaseImageTag = "7.0" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("7.0", container.BaseImageTag);
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

            var ex = Assert.Throws<CommandException>(() =>
                DockerfileGenerator.ApplyContainerDefaults(app, project, container));
            Assert.Contains("netstandard2.0", ex.Message);
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

            Assert.Equal(expectedImage, container.BaseImageName);
        }

        [Fact]
        public void ApplyContainerDefaults_PresetBaseImageName_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo { BaseImageName = "my-custom-image" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("my-custom-image", container.BaseImageName);
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

            Assert.Equal(expectedSdk, container.BuildImageName);
        }

        [Fact]
        public void ApplyContainerDefaults_SetsBuildImageTagFromVersion()
        {
            var app = CreateApp();
            var project = CreateProject(targetFrameworkVersion: "8.0");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("8.0", container.BuildImageTag);
        }

        [Fact]
        public void ApplyContainerDefaults_PresetBuildImageTag_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject(targetFrameworkVersion: "8.0");
            var container = new ContainerInfo { BuildImageTag = "7.0" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("7.0", container.BuildImageTag);
        }

        [Fact]
        public void ApplyContainerDefaults_PresetBuildImageName_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo { BuildImageName = "my-sdk" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("my-sdk", container.BuildImageName);
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

            Assert.Equal("myservice", container.ImageName);
        }

        [Fact]
        public void ApplyContainerDefaults_WithRegistry_PrefixesImageName()
        {
            var registry = new ContainerRegistry("myregistry.azurecr.io", null);
            var app = CreateApp(registry: registry);
            var project = CreateProject(name: "MyService");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("myregistry.azurecr.io/myservice", container.ImageName);
        }

        [Fact]
        public void ApplyContainerDefaults_PresetImageName_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo { ImageName = "custom-name" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("custom-name", container.ImageName);
        }

        [Fact]
        public void ApplyContainerDefaults_WithVersion_SetsImageTag()
        {
            var app = CreateApp();
            var project = CreateProject(version: "1.2.3");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("1.2.3", container.ImageTag);
        }

        [Fact]
        public void ApplyContainerDefaults_VersionWithPlus_ReplacedWithHyphen()
        {
            var app = CreateApp();
            var project = CreateProject(version: "1.0.0+build123");
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("1.0.0-build123", container.ImageTag);
        }

        [Fact]
        public void ApplyContainerDefaults_NullVersion_DefaultsToLatest()
        {
            var app = CreateApp();
            var project = CreateProject(version: null);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("latest", container.ImageTag);
        }

        [Fact]
        public void ApplyContainerDefaults_PresetImageTag_DoesNotOverwrite()
        {
            var app = CreateApp();
            var project = CreateProject(version: "1.0.0");
            var container = new ContainerInfo { ImageTag = "my-tag" };

            DockerfileGenerator.ApplyContainerDefaults(app, project, container);

            Assert.Equal("my-tag", container.ImageTag);
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

            var envVar = Assert.Single(project.EnvironmentVariables.Where(
                e => e.Name == "DOTNET_LOGGING__CONSOLE__DISABLECOLORS"));
            Assert.Equal("true", envVar.Value);
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

            Assert.Equal("custom-base", container.BaseImageName);
            Assert.Equal("7.0", container.BaseImageTag);
            Assert.Equal("custom-sdk", container.BuildImageName);
            Assert.Equal("6.0", container.BuildImageTag);
            Assert.Equal("custom-image", container.ImageName);
            Assert.Equal("custom-tag", container.ImageTag);
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

            Assert.Equal("myapp", container.ImageName);
        }

        [Fact]
        public void ApplyContainerDefaults_DockerFile_WithRegistry_PrefixesImageName()
        {
            var registry = new ContainerRegistry("registry.io", null);
            var app = CreateApp(registry: registry);
            var dockerSvc = new DockerFileServiceBuilder("MyApp", "myapp", ServiceSource.Configuration);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, dockerSvc, container);

            Assert.Equal("registry.io/myapp", container.ImageName);
        }

        [Fact]
        public void ApplyContainerDefaults_DockerFile_DefaultsImageTagToLatest()
        {
            var app = CreateApp();
            var dockerSvc = new DockerFileServiceBuilder("MyApp", "myapp", ServiceSource.Configuration);
            var container = new ContainerInfo();

            DockerfileGenerator.ApplyContainerDefaults(app, dockerSvc, container);

            Assert.Equal("latest", container.ImageTag);
        }

        [Fact]
        public void ApplyContainerDefaults_DockerFile_PresetImageTag_DoesNotOverwrite()
        {
            var app = CreateApp();
            var dockerSvc = new DockerFileServiceBuilder("MyApp", "myapp", ServiceSource.Configuration);
            var container = new ContainerInfo { ImageTag = "v2" };

            DockerfileGenerator.ApplyContainerDefaults(app, dockerSvc, container);

            Assert.Equal("v2", container.ImageTag);
        }

        [Fact]
        public void ApplyContainerDefaults_DockerFile_PresetImageName_DoesNotOverwrite()
        {
            var app = CreateApp();
            var dockerSvc = new DockerFileServiceBuilder("MyApp", "myapp", ServiceSource.Configuration);
            var container = new ContainerInfo { ImageName = "custom" };

            DockerfileGenerator.ApplyContainerDefaults(app, dockerSvc, container);

            Assert.Equal("custom", container.ImageName);
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

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DockerfileGenerator.WriteDockerfileAsync(null!, app, project, container, "file.txt"));
            Assert.Equal("output", ex.ParamName);
        }

        [Fact]
        public async Task WriteDockerfileAsync_NullApplication_ThrowsArgumentNullException()
        {
            var project = CreateProject();
            var container = new ContainerInfo();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DockerfileGenerator.WriteDockerfileAsync(_output, null!, project, container, "file.txt"));
            Assert.Equal("application", ex.ParamName);
        }

        [Fact]
        public async Task WriteDockerfileAsync_NullProject_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var container = new ContainerInfo();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DockerfileGenerator.WriteDockerfileAsync(_output, app, null!, container, "file.txt"));
            Assert.Equal("project", ex.ParamName);
        }

        [Fact]
        public async Task WriteDockerfileAsync_NullContainer_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var project = CreateProject();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DockerfileGenerator.WriteDockerfileAsync(_output, app, project, null!, "file.txt"));
            Assert.Equal("container", ex.ParamName);
        }

        [Fact]
        public async Task WriteDockerfileAsync_NullFilePath_ThrowsArgumentNullException()
        {
            var app = CreateApp();
            var project = CreateProject();
            var container = new ContainerInfo();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DockerfileGenerator.WriteDockerfileAsync(_output, app, project, container, null!));
            Assert.Equal("filePath", ex.ParamName);
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

            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("FROM mcr.microsoft.com/dotnet/sdk:8.0 as SDK", content);
            Assert.Contains("WORKDIR /src", content);
            Assert.Contains("COPY . .", content);
            Assert.Contains("RUN dotnet publish -c Release -o /out", content);
            Assert.Contains("FROM mcr.microsoft.com/dotnet/aspnet:8.0 as RUNTIME", content);
            Assert.Contains("WORKDIR /app", content);
            Assert.Contains("COPY --from=SDK /out .", content);
            Assert.Contains("ENTRYPOINT [\"dotnet\", \"WebApp.dll\"]", content);
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

            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("CMD [\"--urls http://+:80\"]", content);
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

            var content = await File.ReadAllTextAsync(filePath);
            Assert.DoesNotContain("CMD", content);
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

            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("as SDK", content);
            Assert.Contains("as RUNTIME", content);
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

            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("FROM mcr.microsoft.com/dotnet/runtime:8.0", content);
            Assert.Contains("WORKDIR /app", content);
            Assert.Contains("COPY . /app", content);
            Assert.Contains("ENTRYPOINT [\"dotnet\", \"WorkerApp.dll\"]", content);
            Assert.DoesNotContain("as SDK", content);
            Assert.DoesNotContain("as RUNTIME", content);
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

            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("CMD [\"--verbose\"]", content);
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

            var content = await File.ReadAllTextAsync(filePath);
            Assert.DoesNotContain("CMD", content);
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

            var content = await File.ReadAllTextAsync(filePath);
            Assert.DoesNotContain("CMD", content);
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

            var bytes = await File.ReadAllBytesAsync(filePath);
            // UTF8 BOM is 0xEF, 0xBB, 0xBF
            Assert.True(bytes.Length >= 3, "File should have content");
            var hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            Assert.False(hasBom, "Dockerfile should not have UTF-8 BOM");
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

            Assert.Equal("mcr.microsoft.com/dotnet/core/aspnet", container.BaseImageName);
            Assert.Equal("mcr.microsoft.com/dotnet/core/sdk", container.BuildImageName);
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

            Assert.Equal("mcr.microsoft.com/dotnet/runtime", container.BaseImageName);
            Assert.Equal("mcr.microsoft.com/dotnet/sdk", container.BuildImageName);
        }
    }
}
