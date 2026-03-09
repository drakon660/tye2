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
        // ConfigIngressParser Tests
        // =====================================================================

        [Fact]
        public void Ingress_Name_ConvertedToLowercase()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: MyIngress
");
            var app = parser.ParseConfigApplication();
            app.Ingress.Single().Name.Should().Be("myingress");
        }

        [Fact]
        public void Ingress_Replicas_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    replicas: 5
");
            var app = parser.ParseConfigApplication();
            app.Ingress.Single().Replicas.Should().Be(5);
        }

        [Fact]
        public void Ingress_Replicas_ZeroIsValid()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    replicas: 0
");
            var app = parser.ParseConfigApplication();
            app.Ingress.Single().Replicas.Should().Be(0);
        }

        [Fact]
        public void Ingress_Rules_AllFields_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    rules:
      - host: example.com
        path: /api
        preservePath: true
        service: backend
");
            var app = parser.ParseConfigApplication();
            var rule = app.Ingress.Single().Rules.Should().ContainSingle().Subject;
            rule.Host.Should().Be("example.com");
            rule.Path.Should().Be("/api");
            rule.PreservePath.Should().BeTrue();
            rule.Service.Should().Be("backend");
        }

        [Fact]
        public void Ingress_Rules_ServiceName_ConvertedToLowercase()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    rules:
      - service: MyBackend
");
            var app = parser.ParseConfigApplication();
            app.Ingress.Single().Rules.Single().Service.Should().Be("mybackend");
        }

        [Fact]
        public void Ingress_Rules_PreservePath_MustBeBoolean()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    rules:
      - preservePath: notabool
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatMustBeABoolean("preservePath")}*");
        }

        [Fact]
        public void Ingress_Rules_UnrecognizedKey_Throws()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    rules:
      - unknown: value
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnrecognizedKey("unknown")}*");
        }

        [Fact]
        public void Ingress_Rules_MultipleRules()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    rules:
      - path: /api
        service: api
      - host: web.example.com
        service: web
");
            var app = parser.ParseConfigApplication();
            app.Ingress.Single().Rules.Should().HaveCount(2);
        }

        [Fact]
        public void Ingress_Bindings_AllFields_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    bindings:
      - name: web
        port: 443
        protocol: https
        ip: 192.168.1.1
");
            var app = parser.ParseConfigApplication();
            var binding = app.Ingress.Single().Bindings.Should().ContainSingle().Subject;
            binding.Name.Should().Be("web");
            binding.Port.Should().Be(443);
            binding.Protocol.Should().Be("https");
            binding.IPAddress.Should().Be("192.168.1.1");
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("0.0.0.0")]
        [InlineData("192.168.1.100")]
        [InlineData("::1")]
        [InlineData("*")]
        [InlineData("localhost")]
        [InlineData("Localhost")]
        public void Ingress_Bindings_IP_ValidValues(string ip)
        {
            using var parser = new YamlParser($@"
ingress:
  - name: ing
    bindings:
      - ip: ""{ip}""
");
            var app = parser.ParseConfigApplication();
            app.Ingress.Single().Bindings.Single().IPAddress.Should().Be(ip);
        }

        [Theory]
        [InlineData("notanip")]
        [InlineData("999.999.999.999")]
        public void Ingress_Bindings_IP_InvalidValues_Throws(string ip)
        {
            using var parser = new YamlParser($@"
ingress:
  - name: ing
    bindings:
      - ip: {ip}
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatMustBeAnIPAddress("ip")}*");
        }

        [Fact]
        public void Ingress_Bindings_UnrecognizedKey_Throws()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    bindings:
      - unknown: value
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnrecognizedKey("unknown")}*");
        }

    }
}
