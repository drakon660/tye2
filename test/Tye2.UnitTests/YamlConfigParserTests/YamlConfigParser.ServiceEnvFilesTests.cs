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
        // ConfigServiceParser Tests — Env Files
        // =====================================================================

        [Fact]
        public void Service_EnvFile_MustBeSequence()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    env_file: ./file.env
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatExpectedYamlSequence("env_file")}*");
        }

        [Fact]
        public void Service_EnvFile_FileNotFound_Throws()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    env_file:
      - ./nonexistent.env
", new FileInfo(Path.Join(Directory.GetCurrentDirectory(), "testassets", "tye.yaml")));

            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage("*nonexistent.env*");
        }

        [Fact]
        public void Service_EnvFile_ParsesCorrectly()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    env_file:
      - ./envfile_a.env
", new FileInfo(Path.Join(Directory.GetCurrentDirectory(), "testassets", "tye.yaml")));

            var app = parser.ParseConfigApplication();
            app.Services.Single().Configuration.Should().NotBeEmpty();
        }

    }
}
