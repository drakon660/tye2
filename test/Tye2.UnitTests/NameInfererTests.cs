using System.IO;
using AwesomeAssertions;
using Tye2.Core.ConfigModel;
using Xunit;

namespace Tye2.UnitTests
{
    public class NameInfererTests
    {
        // =====================================================================
        // Null input
        // =====================================================================

        [Fact]
        public void InferApplicationName_Null_ReturnsNull()
        {
            NameInferer.InferApplicationName(null!).Should().BeNull();
        }

        // =====================================================================
        // .sln files
        // =====================================================================

        [Fact]
        public void InferApplicationName_SlnFile_ReturnsNameWithoutExtension()
        {
            var fileInfo = new FileInfo("C:\\projects\\MySolution.sln");
            NameInferer.InferApplicationName(fileInfo).Should().Be("mysolution");
        }

        // =====================================================================
        // .csproj files
        // =====================================================================

        [Fact]
        public void InferApplicationName_CsprojFile_ReturnsNameWithoutExtension()
        {
            var fileInfo = new FileInfo("C:\\projects\\MyApp\\MyApp.csproj");
            NameInferer.InferApplicationName(fileInfo).Should().Be("myapp");
        }

        // =====================================================================
        // .fsproj files
        // =====================================================================

        [Fact]
        public void InferApplicationName_FsprojFile_ReturnsNameWithoutExtension()
        {
            var fileInfo = new FileInfo("C:\\projects\\MyFSharpApp\\MyFSharpApp.fsproj");
            NameInferer.InferApplicationName(fileInfo).Should().Be("myfsharpapp");
        }

        // =====================================================================
        // YAML/other files — uses directory name
        // =====================================================================

        [Fact]
        public void InferApplicationName_YamlFile_ReturnsDirectoryName()
        {
            var fileInfo = new FileInfo("C:\\projects\\MyProject\\tye.yaml");
            NameInferer.InferApplicationName(fileInfo).Should().Be("myproject");
        }

        [Fact]
        public void InferApplicationName_YmlFile_ReturnsDirectoryName()
        {
            var fileInfo = new FileInfo("C:\\projects\\WebApp\\tye.yml");
            NameInferer.InferApplicationName(fileInfo).Should().Be("webapp");
        }

        [Fact]
        public void InferApplicationName_DockerComposeFile_ReturnsDirectoryName()
        {
            var fileInfo = new FileInfo("C:\\projects\\Services\\docker-compose.yaml");
            NameInferer.InferApplicationName(fileInfo).Should().Be("services");
        }

        // =====================================================================
        // Case normalization
        // =====================================================================

        [Fact]
        public void InferApplicationName_MixedCase_ReturnsLowerCase()
        {
            var fileInfo = new FileInfo("C:\\projects\\MyBigProject.csproj");
            NameInferer.InferApplicationName(fileInfo).Should().Be("mybigproject");
        }

        [Fact]
        public void InferApplicationName_UpperCaseDir_ReturnsLowerCase()
        {
            var fileInfo = new FileInfo("C:\\projects\\MY_PROJECT\\tye.yaml");
            NameInferer.InferApplicationName(fileInfo).Should().Be("my_project");
        }
    }
}
