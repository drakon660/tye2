using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Core.ConfigModel;
using Tye2.Core.Serialization;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;


namespace Tye2.UnitTests
{
    public partial class YamlConfigParserTests
    {
        // =====================================================================
        // ConfigServiceParser Tests — Bindings
        // =====================================================================

        [Fact]
        public void Service_Bindings_AllFields_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    bindings:
      - name: web
        port: 8080
        containerPort: 80
        host: 0.0.0.0
        protocol: https
        connectionString: Server=localhost
        routes:
          - /api
          - /health
");
            var app = parser.ParseConfigApplication();
            var binding = app.Services.Single().Bindings.Should().ContainSingle().Subject;
            binding.Name.Should().Be("web");
            binding.Port.Should().Be(8080);
            binding.ContainerPort.Should().Be(80);
            binding.Host.Should().Be("0.0.0.0");
            binding.Protocol.Should().Be("https");
            binding.ConnectionString.Should().Be("Server=localhost");
            binding.Routes.Should().BeEquivalentTo(new[] { "/api", "/health" });
        }

        [Fact]
        public void Service_Bindings_MultipleBindings()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    bindings:
      - name: http
        port: 80
      - name: https
        port: 443
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Bindings.Should().HaveCount(2);
        }

        [Fact]
        public void Service_Bindings_Port_MustBeInteger()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    bindings:
      - port: abc
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatMustBeAnInteger("port")}*");
        }

        [Fact]
        public void Service_Bindings_ContainerPort_MustBeInteger()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    bindings:
      - containerPort: abc
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatMustBeAnInteger("containerPort")}*");
        }

        [Fact]
        public void Service_Bindings_UnrecognizedKey_Throws()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    bindings:
      - unknown: value
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnrecognizedKey("unknown")}*");
        }

        [Fact]
        public void Service_Bindings_Routes_MustBeSequence()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    bindings:
      - routes: /api
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatExpectedYamlSequence("routes")}*");
        }

        [Fact]
        public void Service_Bindings_MustBeMappings()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    bindings:
      - justascalar
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString())}*");
        }

    }
}
