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
        // ConfigServiceParser Tests — Environment Configuration
        // =====================================================================

        [Fact]
        public void Service_Env_MixedSyntax_MappingAndCompact()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    env:
      - name: EXPLICIT
        value: explicit_val
      - COMPACT=compact_val
");
            var app = parser.ParseConfigApplication();
            var config = app.Services.Single().Configuration;
            config.Should().HaveCount(2);
            config.First(c => c.Name == "EXPLICIT").Value.Should().Be("explicit_val");
            config.First(c => c.Name == "COMPACT").Value.Should().Be("compact_val");
        }

        [Fact]
        public void Service_Env_Compact_TrimQuotes()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    env:
      - VAR1=""quoted value""
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Configuration.Single().Value.Should().Be("quoted value");
        }

        [Fact]
        public void Service_Env_Compact_EmptyValue()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    env:
      - VAR1=
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Configuration.Single().Value.Should().BeEmpty();
        }

        [Fact]
        public void Service_Env_Compact_TrimWhitespace()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    env:
      - KEY = value
");
            var app = parser.ParseConfigApplication();
            var cfg = app.Services.Single().Configuration.Single();
            cfg.Name.Should().Be("KEY");
            cfg.Value.Should().Be("value");
        }

        [Fact]
        public void Service_Env_ReferenceFromEnvironment()
        {
            var uniqueKey = $"TYE_TEST_{Guid.NewGuid():N}";
            Environment.SetEnvironmentVariable(uniqueKey, "env_value");
            try
            {
                using var parser = new YamlParser($@"
services:
  - name: svc
    env:
      - {uniqueKey}
");
                var app = parser.ParseConfigApplication();
                app.Services.Single().Configuration.Single().Value.Should().Be("env_value");
            }
            finally
            {
                Environment.SetEnvironmentVariable(uniqueKey, null);
            }
        }

        [Fact]
        public void Service_Env_ReferenceFromEnvironment_NotSet_EmptyString()
        {
            var uniqueKey = $"TYE_TEST_NONEXISTENT_{Guid.NewGuid():N}";
            using var parser = new YamlParser($@"
services:
  - name: svc
    env:
      - {uniqueKey}
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Configuration.Single().Value.Should().BeEmpty();
        }

        [Fact]
        public void Service_Env_MappingWithNameOnly_FallsBackToEnvironment()
        {
            var uniqueKey = $"TYE_TEST_{Guid.NewGuid():N}";
            Environment.SetEnvironmentVariable(uniqueKey, "from_env");
            try
            {
                using var parser = new YamlParser($@"
services:
  - name: svc
    env:
      - name: {uniqueKey}
");
                var app = parser.ParseConfigApplication();
                app.Services.Single().Configuration.Single().Value.Should().Be("from_env");
            }
            finally
            {
                Environment.SetEnvironmentVariable(uniqueKey, null);
            }
        }

        [Fact]
        public void Service_Env_ConfigurationAlias_Works()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    configuration:
      - name: VAR
        value: val
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Configuration.Single().Value.Should().Be("val");
        }

        [Fact]
        public void Service_Env_UnrecognizedMappingKey_Throws()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    env:
      - name: VAR
        unknown: val
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnrecognizedKey("unknown")}*");
        }

    }
}
