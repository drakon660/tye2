using System.Diagnostics;
using AwesomeAssertions;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests;

public class ProcessUtilTests
{
    [Fact]
    public void TryGetProcessStartTimeUtcTicks_CurrentProcess_ReturnsExpectedTicks()
    {
        using var process = Process.GetCurrentProcess();

        var result = ProcessUtil.TryGetProcessStartTimeUtcTicks(process.Id, out var startTimeUtcTicks);

        result.Should().BeTrue();
        startTimeUtcTicks.Should().Be(process.StartTime.ToUniversalTime().Ticks);
    }

    [Fact]
    public void TryGetProcessStartTimeUtcTicks_InvalidPid_ReturnsFalse()
    {
        var result = ProcessUtil.TryGetProcessStartTimeUtcTicks(int.MaxValue, out var startTimeUtcTicks);

        result.Should().BeFalse();
        startTimeUtcTicks.Should().Be(0);
    }
}
