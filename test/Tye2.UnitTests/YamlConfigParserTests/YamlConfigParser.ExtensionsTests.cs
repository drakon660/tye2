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
        // ConfigExtensionsParser Tests
        // =====================================================================

        [Fact]
        public void Extensions_SimpleExtension_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
extensions:
  - name: dapr
");
            var app = parser.ParseConfigApplication();
            app.Extensions.Should().ContainSingle()
                .Which.Should().ContainKey("name")
                .WhoseValue.Should().Be("dapr");
        }

        [Fact]
        public void Extensions_WithNestedConfig_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
extensions:
  - name: dapr
    log-level: debug
    components-path: ./components
");
            var app = parser.ParseConfigApplication();
            var ext = app.Extensions.Should().ContainSingle().Subject;
            ext["name"].Should().Be("dapr");
            ext["log-level"].Should().Be("debug");
            ext["components-path"].Should().Be("./components");
        }

        [Fact]
        public void Extensions_MultipleExtensions()
        {
            using var parser = new YamlParser(@"
extensions:
  - name: dapr
  - name: zipkin
  - name: seq
");
            var app = parser.ParseConfigApplication();
            app.Extensions.Should().HaveCount(3);
        }

        [Fact]
        public void Extensions_ScalarItem_Throws()
        {
            using var parser = new YamlParser(@"
extensions:
  - justascalar
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString())}*");
        }

        [Fact]
        public void Extensions_MustBeSequence()
        {
            using var parser = new YamlParser(@"
extensions: notasequence
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatExpectedYamlSequence("extensions")}*");
        }

    }
}
