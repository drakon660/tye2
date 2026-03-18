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
        // ConfigServiceParser Tests — BuildProperties
        // =====================================================================

        [Fact]
        public void Service_BuildProperties_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    buildProperties:
      - name: Configuration
        value: Release
      - name: Platform
        value: x64
");
            var app = parser.ParseConfigApplication();
            var props = app.Services.Single().BuildProperties;
            props.Should().HaveCount(2);
            props[0].Name.Should().Be("Configuration");
            props[0].Value.Should().Be("Release");
            props[1].Name.Should().Be("Platform");
            props[1].Value.Should().Be("x64");
        }

        [Fact]
        public void Service_BuildProperties_MustBeSequence()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    buildProperties: abc
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatExpectedYamlSequence("buildProperties")}*");
        }

        [Fact]
        public void Service_BuildProperties_UnrecognizedKey_Throws()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    buildProperties:
      - name: Conf
        unknown: val
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnrecognizedKey("unknown")}*");
        }

    }
}
