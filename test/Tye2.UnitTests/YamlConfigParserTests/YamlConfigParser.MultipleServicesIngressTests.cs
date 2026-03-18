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
        // Multiple Services / Ingress
        // =====================================================================

        [Fact]
        public void MultipleServices_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
services:
  - name: frontend
    project: Frontend/Frontend.csproj
  - name: backend
    project: Backend/Backend.csproj
  - name: redis
    image: redis:latest
");
            var app = parser.ParseConfigApplication();
            app.Services.Should().HaveCount(3);
            app.Services[0].Name.Should().Be("frontend");
            app.Services[1].Name.Should().Be("backend");
            app.Services[2].Name.Should().Be("redis");
        }

        [Fact]
        public void MultipleIngress_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
ingress:
  - name: public
    rules:
      - path: /api
        service: api
  - name: internal
    rules:
      - path: /admin
        service: admin
");
            var app = parser.ParseConfigApplication();
            app.Ingress.Should().HaveCount(2);
            app.Ingress[0].Name.Should().Be("public");
            app.Ingress[1].Name.Should().Be("internal");
        }

    }
}
