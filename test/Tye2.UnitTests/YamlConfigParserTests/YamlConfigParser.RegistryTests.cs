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
        // ConfigRegistryParser Tests
        // =====================================================================

        [Fact]
        public void Registry_ScalarFormat_ParsedCorrectly()
        {
            using var parser = new YamlParser("registry: myregistry.azurecr.io");
            var app = parser.ParseConfigApplication();
            app.Registry.Should().NotBeNull();
            app.Registry!.Hostname.Should().Be("myregistry.azurecr.io");
            app.Registry.PullSecret.Should().BeNull();
        }

        [Fact]
        public void Registry_MappingFormat_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
registry:
  name: myregistry.azurecr.io
  pullSecret: my-secret
");
            var app = parser.ParseConfigApplication();
            app.Registry!.Hostname.Should().Be("myregistry.azurecr.io");
            app.Registry.PullSecret.Should().Be("my-secret");
        }

        [Fact]
        public void Registry_MappingFormat_WithoutPullSecret()
        {
            using var parser = new YamlParser(@"
registry:
  name: myregistry.azurecr.io
");
            var app = parser.ParseConfigApplication();
            app.Registry!.Hostname.Should().Be("myregistry.azurecr.io");
            app.Registry.PullSecret.Should().BeNull();
        }

        [Fact]
        public void Registry_MappingFormat_UnrecognizedKey_Throws()
        {
            using var parser = new YamlParser(@"
registry:
  name: myregistry.azurecr.io
  unknown: value
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnrecognizedKey("unknown")}*");
        }

    }
}
