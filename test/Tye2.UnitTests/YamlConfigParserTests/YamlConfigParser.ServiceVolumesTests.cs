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
        // ConfigServiceParser Tests — Volumes
        // =====================================================================

        [Fact]
        public void Service_Volumes_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    volumes:
      - name: data
        source: /host/data
        target: /container/data
");
            var app = parser.ParseConfigApplication();
            var vol = app.Services.Single().Volumes.Should().ContainSingle().Subject;
            vol.Name.Should().Be("data");
            vol.Source.Should().Be("/host/data");
            vol.Target.Should().Be("/container/data");
        }

        [Fact]
        public void Service_Volumes_UnrecognizedKey_Throws()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    volumes:
      - name: data
        unknown: value
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnrecognizedKey("unknown")}*");
        }

        [Fact]
        public void Service_Volumes_MustBeMappings()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    volumes:
      - justascalar
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString())}*");
        }

    }
}
