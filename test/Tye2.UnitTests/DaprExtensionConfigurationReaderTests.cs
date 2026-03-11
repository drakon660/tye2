using System.Collections.Generic;
using AwesomeAssertions;
using Tye2.Extensions.Dapr;
using Xunit;

namespace Tye2.UnitTests;

public class DaprExtensionConfigurationReaderTests
{
    [Fact]
    public void EmptyConfiguration_ReturnsDefaults()
    {
        var raw = new Dictionary<string, object>();

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.AppMaxConcurrency.Should().BeNull();
        config.AppProtocol.Should().BeNull();
        config.AppSsl.Should().BeNull();
        config.ComponentsPath.Should().BeNull();
        config.Config.Should().BeNull();
        config.EnableProfiling.Should().BeNull();
        config.HttpMaxRequestSize.Should().BeNull();
        config.LogLevel.Should().BeNull();
        config.PlacementPort.Should().BeNull();
        config.Services.Should().BeEmpty();
    }

    [Fact]
    public void AllCommonProperties_ParsedCorrectly()
    {
        var raw = new Dictionary<string, object>
        {
            ["app-max-concurrency"] = 10,
            ["app-protocol"] = "grpc",
            ["app-ssl"] = true,
            ["components-path"] = "./components/",
            ["config"] = "tracing",
            ["enable-profiling"] = true,
            ["http-max-request-size"] = 16,
            ["log-level"] = "debug",
            ["placement-port"] = 6050,
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.AppMaxConcurrency.Should().Be(10);
        config.AppProtocol.Should().Be("grpc");
        config.AppSsl.Should().BeTrue();
        config.ComponentsPath.Should().Be("./components/");
        config.Config.Should().Be("tracing");
        config.EnableProfiling.Should().BeTrue();
        config.HttpMaxRequestSize.Should().Be(16);
        config.LogLevel.Should().Be("debug");
        config.PlacementPort.Should().Be(6050);
    }

    [Fact]
    public void StringValues_ParsedAsStrings()
    {
        var raw = new Dictionary<string, object>
        {
            ["app-max-concurrency"] = "10",
            ["app-ssl"] = "true",
            ["placement-port"] = "6050",
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.AppMaxConcurrency.Should().Be(10);
        config.AppSsl.Should().BeTrue();
        config.PlacementPort.Should().Be(6050);
    }

    [Fact]
    public void InvalidIntValue_ReturnsNull()
    {
        var raw = new Dictionary<string, object>
        {
            ["app-max-concurrency"] = "not-a-number",
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.AppMaxConcurrency.Should().BeNull();
    }

    [Fact]
    public void InvalidBoolValue_ReturnsNull()
    {
        var raw = new Dictionary<string, object>
        {
            ["app-ssl"] = "not-a-bool",
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.AppSsl.Should().BeNull();
    }

    [Fact]
    public void NullValue_ReturnsNull()
    {
        var raw = new Dictionary<string, object>
        {
            ["config"] = null!,
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.Config.Should().BeNull();
    }

    [Fact]
    public void ServicesSection_ParsedCorrectly()
    {
        var raw = new Dictionary<string, object>
        {
            ["services"] = new Dictionary<string, object>
            {
                ["web"] = new Dictionary<string, object>
                {
                    ["app-id"] = "my-web-app",
                    ["enabled"] = true,
                    ["grpc-port"] = 50001,
                    ["http-port"] = 3500,
                    ["metrics-port"] = 9090,
                    ["profile-port"] = 7777,
                },
            },
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.Services.Should().ContainKey("web");
        var svc = config.Services["web"];
        svc.AppId.Should().Be("my-web-app");
        svc.Enabled.Should().BeTrue();
        svc.GrpcPort.Should().Be(50001);
        svc.HttpPort.Should().Be(3500);
        svc.MetricsPort.Should().Be(9090);
        svc.ProfilePort.Should().Be(7777);
    }

    [Fact]
    public void ServiceInheritsCommonProperties()
    {
        var raw = new Dictionary<string, object>
        {
            ["services"] = new Dictionary<string, object>
            {
                ["api"] = new Dictionary<string, object>
                {
                    ["app-protocol"] = "http",
                    ["log-level"] = "warn",
                    ["components-path"] = "./my-components/",
                },
            },
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        var svc = config.Services["api"];
        svc.AppProtocol.Should().Be("http");
        svc.LogLevel.Should().Be("warn");
        svc.ComponentsPath.Should().Be("./my-components/");
    }

    [Fact]
    public void MultipleServices_AllParsed()
    {
        var raw = new Dictionary<string, object>
        {
            ["services"] = new Dictionary<string, object>
            {
                ["orders"] = new Dictionary<string, object>
                {
                    ["app-id"] = "orders-svc",
                    ["http-port"] = 3500,
                },
                ["products"] = new Dictionary<string, object>
                {
                    ["app-id"] = "products-svc",
                    ["http-port"] = 3501,
                },
                ["store"] = new Dictionary<string, object>
                {
                    ["app-id"] = "store-svc",
                },
            },
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.Services.Should().HaveCount(3);
        config.Services["orders"].AppId.Should().Be("orders-svc");
        config.Services["orders"].HttpPort.Should().Be(3500);
        config.Services["products"].AppId.Should().Be("products-svc");
        config.Services["products"].HttpPort.Should().Be(3501);
        config.Services["store"].AppId.Should().Be("store-svc");
        config.Services["store"].HttpPort.Should().BeNull();
    }

    [Fact]
    public void ServicesCaseInsensitiveLookup()
    {
        var raw = new Dictionary<string, object>
        {
            ["services"] = new Dictionary<string, object>
            {
                ["WebApp"] = new Dictionary<string, object>
                {
                    ["app-id"] = "webapp",
                },
            },
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.Services.Should().ContainKey("webapp");
        config.Services.Should().ContainKey("WEBAPP");
        config.Services.Should().ContainKey("WebApp");
    }

    [Fact]
    public void ServicesWithNonDictionaryValue_Skipped()
    {
        var raw = new Dictionary<string, object>
        {
            ["services"] = new Dictionary<string, object>
            {
                ["valid"] = new Dictionary<string, object>
                {
                    ["app-id"] = "valid-svc",
                },
                ["invalid"] = "not-a-dictionary",
            },
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.Services.Should().HaveCount(1);
        config.Services.Should().ContainKey("valid");
    }

    [Fact]
    public void ServicesKeyNotDictionary_NoServices()
    {
        var raw = new Dictionary<string, object>
        {
            ["services"] = "not-a-dictionary",
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.Services.Should().BeEmpty();
    }

    [Fact]
    public void GlobalAndServiceConfig_BothParsed()
    {
        var raw = new Dictionary<string, object>
        {
            ["log-level"] = "debug",
            ["config"] = "tracing",
            ["components-path"] = "./components/",
            ["services"] = new Dictionary<string, object>
            {
                ["web"] = new Dictionary<string, object>
                {
                    ["app-id"] = "web-app",
                    ["log-level"] = "info",
                },
            },
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        // Global level
        config.LogLevel.Should().Be("debug");
        config.Config.Should().Be("tracing");
        config.ComponentsPath.Should().Be("./components/");

        // Service level overrides global
        config.Services["web"].LogLevel.Should().Be("info");
        config.Services["web"].AppId.Should().Be("web-app");
        // Service doesn't inherit global - each is parsed independently
        config.Services["web"].Config.Should().BeNull();
    }

    [Fact]
    public void ServiceEnabledFalse_ParsedCorrectly()
    {
        var raw = new Dictionary<string, object>
        {
            ["services"] = new Dictionary<string, object>
            {
                ["disabled-svc"] = new Dictionary<string, object>
                {
                    ["enabled"] = false,
                },
            },
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.Services["disabled-svc"].Enabled.Should().BeFalse();
    }

    [Fact]
    public void BoolFalseAsString_ParsedCorrectly()
    {
        var raw = new Dictionary<string, object>
        {
            ["app-ssl"] = "false",
            ["enable-profiling"] = "False",
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.AppSsl.Should().BeFalse();
        config.EnableProfiling.Should().BeFalse();
    }

    [Fact]
    public void UnknownKeys_Ignored()
    {
        var raw = new Dictionary<string, object>
        {
            ["unknown-key"] = "value",
            ["another-unknown"] = 42,
            ["config"] = "tracing",
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.Config.Should().Be("tracing");
    }

    [Fact]
    public void RealWorldPubSubConfig_ParsedCorrectly()
    {
        // Mirrors samples/dapr/pub-sub/tye.yaml extension config
        var raw = new Dictionary<string, object>
        {
            ["log-level"] = "debug",
            ["components-path"] = "./components/",
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.LogLevel.Should().Be("debug");
        config.ComponentsPath.Should().Be("./components/");
    }

    [Fact]
    public void RealWorldE2EConfig_ParsedCorrectly()
    {
        // Mirrors test/Tye2.E2ETests/testassets/projects/dapr/tye.yaml extension config
        var raw = new Dictionary<string, object>
        {
            ["config"] = "tracing",
            ["log-level"] = "debug",
        };

        var config = DaprExtensionConfigurationReader.ReadConfiguration(raw);

        config.Config.Should().Be("tracing");
        config.LogLevel.Should().Be("debug");
    }
}
