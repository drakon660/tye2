using System;
using System.IO;
using AwesomeAssertions;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests
{
    public class ConfigFileFinderTests : IDisposable
    {
        private readonly string _tempDir;

        public ConfigFileFinderTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tye2_configfinder_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        // =====================================================================
        // tye.yaml / tye.yml detection
        // =====================================================================

        [Fact]
        public void TryFind_TyeYaml_Found()
        {
            File.WriteAllText(Path.Combine(_tempDir, "tye.yaml"), "name: test");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeTrue();
            filePath.Should().Contain("tye.yaml");
        }

        [Fact]
        public void TryFind_TyeYml_Found()
        {
            File.WriteAllText(Path.Combine(_tempDir, "tye.yml"), "name: test");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeTrue();
            filePath.Should().Contain("tye.yml");
        }

        // =====================================================================
        // docker-compose detection
        // =====================================================================

        [Fact]
        public void TryFind_DockerComposeYaml_Found()
        {
            File.WriteAllText(Path.Combine(_tempDir, "docker-compose.yaml"), "version: '3'");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeTrue();
            filePath.Should().Contain("docker-compose.yaml");
        }

        [Fact]
        public void TryFind_DockerComposeYml_Found()
        {
            File.WriteAllText(Path.Combine(_tempDir, "docker-compose.yml"), "version: '3'");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeTrue();
            filePath.Should().Contain("docker-compose.yml");
        }

        // =====================================================================
        // Project files
        // =====================================================================

        [Fact]
        public void TryFind_Csproj_Found()
        {
            File.WriteAllText(Path.Combine(_tempDir, "MyApp.csproj"), "<Project />");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeTrue();
            filePath.Should().Contain("MyApp.csproj");
        }

        [Fact]
        public void TryFind_Fsproj_Found()
        {
            File.WriteAllText(Path.Combine(_tempDir, "MyApp.fsproj"), "<Project />");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeTrue();
            filePath.Should().Contain("MyApp.fsproj");
        }

        [Fact]
        public void TryFind_Sln_Found()
        {
            File.WriteAllText(Path.Combine(_tempDir, "MySolution.sln"), "");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeTrue();
            filePath.Should().Contain("MySolution.sln");
        }

        // =====================================================================
        // Priority order: tye.yaml first
        // =====================================================================

        [Fact]
        public void TryFind_TyeYamlAndCsproj_PrefersYaml()
        {
            File.WriteAllText(Path.Combine(_tempDir, "tye.yaml"), "name: test");
            File.WriteAllText(Path.Combine(_tempDir, "MyApp.csproj"), "<Project />");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeTrue();
            filePath.Should().Contain("tye.yaml");
        }

        [Fact]
        public void TryFind_DockerComposeAndCsproj_PrefersCompose()
        {
            File.WriteAllText(Path.Combine(_tempDir, "docker-compose.yaml"), "version: '3'");
            File.WriteAllText(Path.Combine(_tempDir, "MyApp.csproj"), "<Project />");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeTrue();
            filePath.Should().Contain("docker-compose.yaml");
        }

        // =====================================================================
        // No matching files
        // =====================================================================

        [Fact]
        public void TryFind_EmptyDirectory_ReturnsFalseWithError()
        {
            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeFalse();
            error.Should().Contain("No project");
        }

        [Fact]
        public void TryFind_UnrelatedFiles_ReturnsFalse()
        {
            File.WriteAllText(Path.Combine(_tempDir, "readme.md"), "# Hello");
            File.WriteAllText(Path.Combine(_tempDir, "app.js"), "console.log('hi')");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeFalse();
        }

        // =====================================================================
        // Multiple matching files of same type
        // =====================================================================

        [Fact]
        public void TryFind_MultipleCsproj_ReturnsFalseWithError()
        {
            File.WriteAllText(Path.Combine(_tempDir, "App1.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(_tempDir, "App2.csproj"), "<Project />");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error);

            result.Should().BeFalse();
            error.Should().Contain("More than one");
        }

        // =====================================================================
        // Custom file formats
        // =====================================================================

        [Fact]
        public void TryFind_CustomFormats_UsesProvidedFormats()
        {
            File.WriteAllText(Path.Combine(_tempDir, "custom.config"), "data");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error,
                fileFormats: new[] { "*.config" });

            result.Should().BeTrue();
            filePath.Should().Contain("custom.config");
        }

        [Fact]
        public void TryFind_CustomFormats_IgnoresDefaultFormats()
        {
            File.WriteAllText(Path.Combine(_tempDir, "tye.yaml"), "name: test");

            var result = ConfigFileFinder.TryFindSupportedFile(_tempDir, out var filePath, out var error,
                fileFormats: new[] { "*.config" });

            result.Should().BeFalse();
        }
    }
}
