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
        // Edge Cases
        // =====================================================================

        [Fact]
        public void EmptyYaml_OnlyName_Works()
        {
            using var parser = new YamlParser("name: minimal");
            var app = parser.ParseConfigApplication();
            app.Name.Should().Be("minimal");
            app.Services.Should().BeEmpty();
            app.Ingress.Should().BeEmpty();
            app.Extensions.Should().BeEmpty();
        }

        [Fact]
        public void Service_EmptyBindingsList_Works()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    bindings: []
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Bindings.Should().BeEmpty();
        }

        [Fact]
        public void Ingress_HostOnly_Rule()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    rules:
      - host: example.com
        service: web
");
            var app = parser.ParseConfigApplication();
            var rule = app.Ingress.Single().Rules.Single();
            rule.Host.Should().Be("example.com");
            rule.Path.Should().BeNull();
        }

        [Fact]
        public void Ingress_PathOnly_Rule()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing
    rules:
      - path: /api
        service: api
");
            var app = parser.ParseConfigApplication();
            var rule = app.Ingress.Single().Rules.Single();
            rule.Host.Should().BeNull();
            rule.Path.Should().Be("/api");
        }

        [Fact]
        public void Service_Env_SequenceItem_NotMappingOrScalar_Throws()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    env:
      - - nested
        - sequence
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage("*Sequence*");
        }

        [Fact]
        public void FullConfig_AllSections()
        {
            using var parser = new YamlParser(@"
name: full-app
namespace: production
network: app-network
registry: myregistry.io
containerEngine: docker
dashboardPort: 8080
extensions:
  - name: zipkin
ingress:
  - name: gateway
    replicas: 2
    bindings:
      - port: 80
        protocol: http
    rules:
      - path: /api
        service: api
      - host: web.example.com
        service: web
    tags:
      - public
services:
  - name: api
    project: Api/Api.csproj
    replicas: 3
    bindings:
      - port: 5000
    env:
      - DB_HOST=localhost
    tags:
      - backend
  - name: web
    image: nginx:latest
    bindings:
      - port: 80
");
            var app = parser.ParseConfigApplication();
            app.Name.Should().Be("full-app");
            app.Namespace.Should().Be("production");
            app.Network.Should().Be("app-network");
            app.Registry!.Hostname.Should().Be("myregistry.io");
            app.ContainerEngineType.Should().Be(ContainerEngineType.Docker);
            app.DashboardPort.Should().Be(8080);
            app.Extensions.Should().ContainSingle();
            app.Ingress.Should().ContainSingle();
            app.Services.Should().HaveCount(2);
            app.Ingress[0].Rules.Should().HaveCount(2);
        }
    }
}
