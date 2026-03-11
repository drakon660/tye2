using System.Threading.Tasks;
using AwesomeAssertions;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests;

public class GitDetectorTests
{
    [Fact]
    public void Instance_IsSingleton()
    {
        var first = GitDetector.Instance;
        var second = GitDetector.Instance;

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Instance_IsNotNull()
    {
        GitDetector.Instance.Should().NotBeNull();
    }

    [Fact]
    public async Task IsGitInstalled_ReturnsTrue_WhenGitAvailable()
    {
        // Git is installed on this dev machine
        var result = await GitDetector.Instance.IsGitInstalled.Value;

        result.Should().BeTrue();
    }

    [Fact]
    public void IsGitInstalled_IsLazy()
    {
        var lazy = GitDetector.Instance.IsGitInstalled;

        lazy.Should().NotBeNull();
        // The lazy should be consistent across calls
        GitDetector.Instance.IsGitInstalled.Should().BeSameAs(lazy);
    }

    [Fact]
    public async Task IsGitInstalled_ReturnsSameResult_OnRepeatedCalls()
    {
        var first = await GitDetector.Instance.IsGitInstalled.Value;
        var second = await GitDetector.Instance.IsGitInstalled.Value;

        first.Should().Be(second);
    }
}
