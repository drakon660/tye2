using System;
using System.IO;
using AwesomeAssertions;
using Xunit;

namespace Tye2.UnitTests
{
    public class TempFileTests
    {
        [Fact]
        public void Create_CreatesExistingTempFile()
        {
            using var tempFile = TempFile.Create();

            File.Exists(tempFile.FilePath).Should().BeTrue();
        }

        [Fact]
        public void Dispose_DeletesFileCreatedByCreate()
        {
            var tempFile = TempFile.Create();
            var filePath = tempFile.FilePath;

            tempFile.Dispose();

            File.Exists(filePath).Should().BeFalse();
        }

        [Fact]
        public void Dispose_WithCustomPath_DeletesFile()
        {
            var filePath = Path.Combine(Path.GetTempPath(), $"tye2-tempfile-{Guid.NewGuid():N}.txt");
            File.WriteAllText(filePath, "temp");

            var tempFile = new TempFile(filePath);
            tempFile.Dispose();

            File.Exists(filePath).Should().BeFalse();
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var tempFile = TempFile.Create();

            tempFile.Dispose();
            Action act = tempFile.Dispose;

            act.Should().NotThrow();
        }
    }
}
