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
        // ConfigApplicationParser Tests
        // =====================================================================

        [Fact]
        public void Application_Name_ParsedCorrectly()
        {
            using var parser = new YamlParser("name: my-app");
            var app = parser.ParseConfigApplication();
            app.Name.Should().Be("my-app");
        }

        [Fact]
        public void Application_Solution_ParsedCorrectly()
        {
            using var parser = new YamlParser("solution: my-solution.sln");
            var app = parser.ParseConfigApplication();
            app.BuildSolution.Should().Be("my-solution.sln");
        }

        [Fact]
        public void Application_Namespace_ParsedCorrectly()
        {
            using var parser = new YamlParser("namespace: my-namespace");
            var app = parser.ParseConfigApplication();
            app.Namespace.Should().Be("my-namespace");
        }

        [Fact]
        public void Application_DashboardPort_ParsedCorrectly()
        {
            using var parser = new YamlParser("dashboardPort: 9999");
            var app = parser.ParseConfigApplication();
            app.DashboardPort.Should().Be(9999);
        }

        [Fact]
        public void Application_DashboardPort_MustBeInteger()
        {
            using var parser = new YamlParser("dashboardPort: abc");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatMustBeAnInteger("dashboardPort")}*");
        }

        [Theory]
        [InlineData("docker", ContainerEngineType.Docker)]
        [InlineData("Docker", ContainerEngineType.Docker)]
        [InlineData("DOCKER", ContainerEngineType.Docker)]
        [InlineData("podman", ContainerEngineType.Podman)]
        [InlineData("Podman", ContainerEngineType.Podman)]
        [InlineData("PODMAN", ContainerEngineType.Podman)]
        public void Application_ContainerEngine_CaseInsensitive(string value, ContainerEngineType expected)
        {
            using var parser = new YamlParser($"containerEngine: {value}");
            var app = parser.ParseConfigApplication();
            app.ContainerEngineType.Should().Be(expected);
        }

        [Fact]
        public void Application_ContainerEngine_UnknownValue_Throws()
        {
            using var parser = new YamlParser("containerEngine: containerd");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage("*Unknown container engine*containerd*");
        }

        [Fact]
        public void Application_AllTopLevelFields_ParsedTogether()
        {
            using var parser = new YamlParser(@"
name: full-app
solution: full.sln
namespace: prod
network: my-net
containerEngine: docker
dashboardPort: 8080
");
            var app = parser.ParseConfigApplication();
            app.Name.Should().Be("full-app");
            app.BuildSolution.Should().Be("full.sln");
            app.Namespace.Should().Be("prod");
            app.Network.Should().Be("my-net");
            app.ContainerEngineType.Should().Be(ContainerEngineType.Docker);
            app.DashboardPort.Should().Be(8080);
        }

        [Fact]
        public void Application_EmptyServices_InitializesCollections()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc1
");
            var app = parser.ParseConfigApplication();
            var svc = app.Services.Should().ContainSingle().Subject;
            svc.Bindings.Should().NotBeNull().And.BeEmpty();
            svc.Configuration.Should().NotBeNull().And.BeEmpty();
            svc.Volumes.Should().NotBeNull().And.BeEmpty();
            svc.Tags.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Application_EmptyIngress_InitializesCollections()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: ing1
");
            var app = parser.ParseConfigApplication();
            var ing = app.Ingress.Should().ContainSingle().Subject;
            ing.Bindings.Should().NotBeNull();
            ing.Rules.Should().NotBeNull();
            ing.Tags.Should().NotBeNull();
        }

    }
}
