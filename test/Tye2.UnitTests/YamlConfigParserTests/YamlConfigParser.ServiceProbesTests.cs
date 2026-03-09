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
        // ConfigServiceParser Tests — Probes (Liveness/Readiness)
        // =====================================================================

        [Theory]
        [InlineData("liveness")]
        [InlineData("readiness")]
        public void Service_Probe_AllFields_ParsedCorrectly(string probeType)
        {
            using var parser = new YamlParser($@"
services:
  - name: svc
    {probeType}:
      http:
        path: /health
        port: 8080
        protocol: https
        headers:
          - name: Authorization
            value: Bearer token123
      initialDelay: 5
      period: 10
      timeout: 3
      successThreshold: 2
      failureThreshold: 5
");
            var app = parser.ParseConfigApplication();
            var svc = app.Services.Single();
            var probe = probeType == "liveness" ? svc.Liveness : svc.Readiness;

            probe.Should().NotBeNull();
            probe!.Http.Should().NotBeNull();
            probe.Http!.Path.Should().Be("/health");
            probe.Http.Port.Should().Be(8080);
            probe.Http.Protocol.Should().Be("https");
            probe.Http.Headers.Should().ContainSingle();
            probe.Http.Headers![0].Key.Should().Be("Authorization");
            probe.Http.Headers[0].Value.Should().Be("Bearer token123");
            probe.InitialDelay.Should().Be(5);
            probe.Period.Should().Be(10);
            probe.Timeout.Should().Be(3);
            probe.SuccessThreshold.Should().Be(2);
            probe.FailureThreshold.Should().Be(5);
        }

        [Fact]
        public void Service_Probe_BothLivenessAndReadiness()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    liveness:
      http:
        path: /live
    readiness:
      http:
        path: /ready
");
            var app = parser.ParseConfigApplication();
            var svc = app.Services.Single();
            svc.Liveness.Should().NotBeNull();
            svc.Readiness.Should().NotBeNull();
            svc.Liveness!.Http!.Path.Should().Be("/live");
            svc.Readiness!.Http!.Path.Should().Be("/ready");
        }

        [Fact]
        public void Service_Probe_MultipleHeaders()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    liveness:
      http:
        path: /health
        headers:
          - name: X-Custom
            value: custom-value
          - name: Authorization
            value: Basic abc
");
            var app = parser.ParseConfigApplication();
            var headers = app.Services.Single().Liveness!.Http!.Headers;
            headers.Should().HaveCount(2);
            headers![0].Key.Should().Be("X-Custom");
            headers[1].Key.Should().Be("Authorization");
        }

        [Fact]
        public void Service_Probe_Header_MissingName_Throws()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    liveness:
      http:
        path: /health
        headers:
          - value: some-value
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatExpectedYamlScalar("name")}*");
        }

        [Fact]
        public void Service_Probe_Header_MissingValue_Throws()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    liveness:
      http:
        path: /health
        headers:
          - name: X-Custom
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatExpectedYamlScalar("value")}*");
        }

        [Fact]
        public void Service_Probe_HttpProber_UnrecognizedKey_Throws()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    liveness:
      http:
        path: /health
        unknown: value
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnrecognizedKey("unknown")}*");
        }

        [Fact]
        public void Service_Probe_InitialDelay_ZeroIsValid()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    liveness:
      initialDelay: 0
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Liveness!.InitialDelay.Should().Be(0);
        }

    }
}
