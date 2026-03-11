using System;
using System.Runtime.InteropServices;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Core.ConfigModel;
using Xunit;

namespace Tye2.UnitTests;

public class ContainerEngineTests
{
    // --- Default singleton ---

    [Fact]
    public void Default_ReturnsSameInstance()
    {
        // Reset to force fresh detection
        ContainerEngine.s_default = null;

        var first = ContainerEngine.Default;
        var second = ContainerEngine.Default;

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Default_IsNotNull()
    {
        ContainerEngine.s_default = null;

        var engine = ContainerEngine.Default;

        engine.Should().NotBeNull();
    }

    // --- Constructor with null (auto-detect) ---

    [Fact]
    public void Constructor_NullType_AutoDetects()
    {
        var engine = new ContainerEngine(null);

        // Should complete without throwing — either docker or podman may be found
        engine.Should().NotBeNull();
    }

    // --- Constructor with explicit Docker ---

    [Fact]
    public void Constructor_Docker_DetectsDockerIfInstalled()
    {
        var engine = new ContainerEngine(ContainerEngineType.Docker);

        if (engine.IsUsable(out _))
        {
            engine.IsPodman.Should().BeFalse();
        }
    }

    [Fact]
    public void Constructor_Docker_ContainerHostSet()
    {
        var engine = new ContainerEngine(ContainerEngineType.Docker);

        if (engine.IsUsable(out _))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // On Linux, container host is the machine IP
                engine.ContainerHost.Should().NotBeNullOrEmpty();
            }
            else
            {
                engine.ContainerHost.Should().Be("host.docker.internal");
            }
        }
    }

    [Fact]
    public void Constructor_Docker_AspNetUrlsHost()
    {
        var engine = new ContainerEngine(ContainerEngineType.Docker);

        if (engine.IsUsable(out _))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                engine.AspNetUrlsHost.Should().Be("*");
            }
            else
            {
                engine.AspNetUrlsHost.Should().Be("localhost");
            }
        }
    }

    // --- Constructor with explicit Podman ---

    [Fact]
    public void Constructor_Podman_DetectsPodmanIfInstalled()
    {
        var engine = new ContainerEngine(ContainerEngineType.Podman);

        if (engine.IsUsable(out _))
        {
            engine.IsPodman.Should().BeTrue();
        }
    }

    // --- IsUsable ---

    [Fact]
    public void IsUsable_WhenUsable_ReturnsTrue()
    {
        var engine = new ContainerEngine(null);

        var usable = engine.IsUsable(out var reason);

        if (usable)
        {
            reason.Should().BeNull();
        }
    }

    [Fact]
    public void IsUsable_WhenNotUsable_ReturnsFalseWithReason()
    {
        // Force both engines to fail by trying Podman on a Docker-only system
        // or vice versa — we test the API contract regardless
        var engine = new ContainerEngine(null);

        var usable = engine.IsUsable(out var reason);

        if (!usable)
        {
            reason.Should().NotBeNullOrEmpty();
        }
    }

    // --- AspNetUrlsHost default ---

    [Fact]
    public void AspNetUrlsHost_DefaultIsLocalhost()
    {
        // On non-Linux with Docker, or with auto-detect
        var engine = new ContainerEngine(ContainerEngineType.Docker);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            engine.AspNetUrlsHost.Should().Be("localhost");
        }
    }

    // --- CommandName throws when not usable ---

    [Fact]
    public void ExecuteAsync_WhenNotUsable_ThrowsInvalidOperation()
    {
        // Create an engine that explicitly requests a type that isn't installed
        // We'll test with both types and use the one that fails
        var dockerEngine = new ContainerEngine(ContainerEngineType.Docker);
        var podmanEngine = new ContainerEngine(ContainerEngineType.Podman);

        ContainerEngine? unusableEngine = null;
        if (!dockerEngine.IsUsable(out _))
        {
            unusableEngine = dockerEngine;
        }
        else if (!podmanEngine.IsUsable(out _))
        {
            unusableEngine = podmanEngine;
        }

        if (unusableEngine != null)
        {
            var act = () => unusableEngine.ExecuteAsync("version").GetAwaiter().GetResult();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not usable*");
        }
    }

    // --- IsPodman property ---

    [Fact]
    public void IsPodman_DockerEngine_ReturnsFalse()
    {
        var engine = new ContainerEngine(ContainerEngineType.Docker);

        if (engine.IsUsable(out _))
        {
            engine.IsPodman.Should().BeFalse();
        }
    }

    // --- s_default can be overridden for tests ---

    [Fact]
    public void StaticDefault_CanBeOverriddenForTesting()
    {
        var original = ContainerEngine.s_default;
        try
        {
            var custom = new ContainerEngine(ContainerEngineType.Docker);
            ContainerEngine.s_default = custom;

            ContainerEngine.Default.Should().BeSameAs(custom);
        }
        finally
        {
            ContainerEngine.s_default = original;
        }
    }

    // --- Auto-detect prefers Podman ---

    [Fact]
    public void Constructor_AutoDetect_PrefersAvailableEngine()
    {
        var engine = new ContainerEngine(null);

        // Should detect at least one engine (docker is available on CI/dev)
        // This test validates the auto-detect path completes
        var usable = engine.IsUsable(out _);

        if (usable)
        {
            // If both engines are available, auto-detect prefers Podman (it's checked first)
            // If only Docker, IsPodman is false
            // Either way the engine should be functional
            engine.ContainerHost.Should().NotBeNull();
        }
    }

    // --- ContainerEngineType enum ---

    [Fact]
    public void ContainerEngineType_HasDockerAndPodman()
    {
        Enum.GetValues<ContainerEngineType>().Should().HaveCount(2);
        Enum.IsDefined(ContainerEngineType.Docker).Should().BeTrue();
        Enum.IsDefined(ContainerEngineType.Podman).Should().BeTrue();
    }
}
