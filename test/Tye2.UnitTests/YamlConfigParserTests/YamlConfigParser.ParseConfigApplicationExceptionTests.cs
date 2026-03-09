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
        // YamlParser.ParseConfigApplication — YamlException handling
        // =====================================================================

        [Fact]
        public void ParseConfigApplication_SemanticError_WithoutFileInfo_IncludesLineAndColumn()
        {
            using var parser = new YamlParser("{ invalid: yaml: content: [}");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage("*YAML semantic error*Line*Col*")
                .WithInnerException<SemanticErrorException>();
        }

        [Fact]
        public void ParseConfigApplication_SemanticError_WithFileInfo_IncludesFileNameAndPosition()
        {
            var fileInfo = new FileInfo(Path.Join(Directory.GetCurrentDirectory(), "bad.yaml"));
            using var parser = new YamlParser("{ invalid: yaml: content: [}", fileInfo);
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage("*Unable to parse 'bad.yaml'*YAML semantic error*Line*Col*")
                .WithInnerException<SemanticErrorException>();
        }

        [Fact]
        public void ParseConfigApplication_YamlException_WithoutFileInfo_ThrowsTyeYamlException()
        {
            // Duplicate keys trigger generic YamlException (not SemanticErrorException)
            using var parser = new YamlParser("name: a\nname: b");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage("*Unable to parse YAML*See inner exception*");
        }

        [Fact]
        public void ParseConfigApplication_YamlException_WithFileInfo_IncludesFileName()
        {
            var fileInfo = new FileInfo(Path.Join(Directory.GetCurrentDirectory(), "fake-tye.yaml"));
            using var parser = new YamlParser("name: a\nname: b", fileInfo);
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage("*Unable to parse 'fake-tye.yaml'*See inner exception*");
        }

        [Fact]
        public void ParseConfigApplication_EmptyDocument_ThrowsOnDocumentAccess()
        {
            using var parser = new YamlParser("");
            var act = () => parser.ParseConfigApplication();
            act.Should().ThrowExactly<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ParseConfigApplication_RootIsSequence_WithFileInfo_ThrowsWithFileName()
        {
            var fileInfo = new FileInfo(Path.Join(Directory.GetCurrentDirectory(), "bad-tye.yaml"));
            using var parser = new YamlParser("- item1\n- item2", fileInfo);
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Sequence.ToString())}*");
        }

        [Fact]
        public void ParseConfigApplication_RootIsScalar_ThrowsUnexpectedType()
        {
            using var parser = new YamlParser("justascalar");
            var act = () => parser.ParseConfigApplication();
            act.Should().Throw<TyeYamlException>()
                .WithMessage($"*{CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString())}*");
        }

    }
}
