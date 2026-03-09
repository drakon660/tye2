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
        // ConfigServiceParser Tests — DockerFileArgs
        // =====================================================================

        [Fact]
        public void Service_DockerFileArgs_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    dockerFileArgs:
      - ARG1: value1
      - ARG2: value2
");
            var app = parser.ParseConfigApplication();
            var args = app.Services.Single().DockerFileArgs;
            args.Should().HaveCount(2);
            args["ARG1"].Should().Be("value1");
            args["ARG2"].Should().Be("value2");
        }

        [Fact]
        public void Service_DockerFileArgs_MustBeSequence()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    dockerFileArgs: abc
");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatExpectedYamlSequence("dockerFileArgs")}*");
        }

    }
}
