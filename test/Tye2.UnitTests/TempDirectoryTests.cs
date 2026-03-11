using System.IO;
using AwesomeAssertions;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests;

public class TempDirectoryTests
{
    [Fact]
    public void Create_ReturnsNonNull()
    {
        using var temp = TempDirectory.Create();

        temp.Should().NotBeNull();
    }

    [Fact]
    public void Create_DirectoryExists()
    {
        using var temp = TempDirectory.Create();

        Directory.Exists(temp.DirectoryPath).Should().BeTrue();
    }

    [Fact]
    public void Create_DirectoryPathIsAbsolute()
    {
        using var temp = TempDirectory.Create();

        Path.IsPathRooted(temp.DirectoryPath).Should().BeTrue();
    }

    [Fact]
    public void Create_DirectoryInfoMatchesPath()
    {
        using var temp = TempDirectory.Create();

        temp.DirectoryInfo.FullName.Should().Be(temp.DirectoryPath);
    }

    [Fact]
    public void Create_MultipleCalls_ReturnDifferentDirectories()
    {
        using var temp1 = TempDirectory.Create();
        using var temp2 = TempDirectory.Create();

        temp1.DirectoryPath.Should().NotBe(temp2.DirectoryPath);
    }

    [Fact]
    public void Dispose_DeletesDirectory()
    {
        string path;
        using (var temp = TempDirectory.Create())
        {
            path = temp.DirectoryPath;
            Directory.Exists(path).Should().BeTrue();
        }

        Directory.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void Dispose_DeletesFilesInDirectory()
    {
        string path;
        using (var temp = TempDirectory.Create())
        {
            path = temp.DirectoryPath;
            File.WriteAllText(Path.Combine(path, "test.txt"), "content");
        }

        Directory.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void Dispose_DeletesSubdirectories()
    {
        string path;
        using (var temp = TempDirectory.Create())
        {
            path = temp.DirectoryPath;
            var subDir = Path.Combine(path, "subdir");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "nested.txt"), "content");
        }

        Directory.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDirectoryInfo_SetsProperties()
    {
        var dirPath = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        var dirInfo = Directory.CreateDirectory(dirPath);
        try
        {
            var temp = new TempDirectory(dirInfo);

            temp.DirectoryPath.Should().Be(dirInfo.FullName);
            temp.DirectoryInfo.Should().BeSameAs(dirInfo);

            temp.Dispose();
        }
        finally
        {
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, true);
            }
        }
    }

    [Fact]
    public void Create_DirectoryIsUnderTempPath()
    {
        using var temp = TempDirectory.Create();

        temp.DirectoryPath.Should().StartWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar));
    }
}
