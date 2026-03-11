using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using AwesomeAssertions;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests;

public class NextPortFinderTests
{
    [Fact]
    public void GetNextPort_ReturnsPositivePort()
    {
        var port = NextPortFinder.GetNextPort();

        port.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetNextPort_ReturnsValidPortRange()
    {
        var port = NextPortFinder.GetNextPort();

        port.Should().BeGreaterThan(0);
        port.Should().BeLessThanOrEqualTo(65535);
    }

    [Fact]
    public void GetNextPort_ReturnsDifferentPorts()
    {
        var port1 = NextPortFinder.GetNextPort();
        var port2 = NextPortFinder.GetNextPort();

        port1.Should().NotBe(port2);
    }

    [Fact]
    public void GetNextPort_PortIsAvailable()
    {
        var port = NextPortFinder.GetNextPort();

        // Should be able to bind to the port
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var act = () => socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
        act.Should().NotThrow();
    }

    [Fact]
    public void GetNextPort_MultipleCalls_AllUnique()
    {
        var ports = new HashSet<int>();
        for (int i = 0; i < 10; i++)
        {
            ports.Add(NextPortFinder.GetNextPort());
        }

        ports.Should().HaveCount(10);
    }

    [Fact]
    public void GetNextPort_ReturnsEphemeralPort()
    {
        var port = NextPortFinder.GetNextPort();

        // Ephemeral ports are typically > 1024
        port.Should().BeGreaterThan(1024);
    }
}
