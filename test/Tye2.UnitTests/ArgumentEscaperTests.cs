using System.Collections.Generic;
using AwesomeAssertions;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests
{
    public class ArgumentEscaperTests
    {
        // =====================================================================
        // Simple arguments (no escaping needed)
        // =====================================================================

        [Fact]
        public void EscapeAndConcatenate_EmptyList_ReturnsEmptyString()
        {
            ArgumentEscaper.EscapeAndConcatenate(new List<string>()).Should().BeEmpty();
        }

        [Fact]
        public void EscapeAndConcatenate_SingleArg_NoQuotes()
        {
            ArgumentEscaper.EscapeAndConcatenate(new[] { "hello" }).Should().Be("hello");
        }

        [Fact]
        public void EscapeAndConcatenate_MultipleSimpleArgs_JoinedWithSpace()
        {
            ArgumentEscaper.EscapeAndConcatenate(new[] { "one", "two", "three" }).Should().Be("one two three");
        }

        // =====================================================================
        // Whitespace handling (needs quoting)
        // =====================================================================

        [Fact]
        public void EscapeAndConcatenate_ArgWithSpace_Quoted()
        {
            ArgumentEscaper.EscapeAndConcatenate(new[] { "hello world" }).Should().Be("\"hello world\"");
        }

        [Fact]
        public void EscapeAndConcatenate_ArgWithTab_Quoted()
        {
            ArgumentEscaper.EscapeAndConcatenate(new[] { "hello\tworld" }).Should().Be("\"hello\tworld\"");
        }

        [Fact]
        public void EscapeAndConcatenate_ArgWithNewline_Quoted()
        {
            ArgumentEscaper.EscapeAndConcatenate(new[] { "hello\nworld" }).Should().Be("\"hello\nworld\"");
        }

        // =====================================================================
        // Already quoted strings
        // =====================================================================

        [Fact]
        public void EscapeAndConcatenate_AlreadyQuoted_PreservesQuotes()
        {
            // Already quoted string: internal quotes get escaped, but no extra surrounding quotes added
            var result = ArgumentEscaper.EscapeAndConcatenate(new[] { "\"already quoted\"" });
            result.Should().Be("\\\"already quoted\\\"");
        }

        // =====================================================================
        // Backslash handling
        // =====================================================================

        [Fact]
        public void EscapeAndConcatenate_TrailingBackslash_NoQuoting_PreservedAsIs()
        {
            ArgumentEscaper.EscapeAndConcatenate(new[] { "path\\" }).Should().Be("path\\");
        }

        [Fact]
        public void EscapeAndConcatenate_TrailingBackslash_WithSpace_Escaped()
        {
            // When quoted, trailing backslashes are doubled to prevent escaping the closing quote
            var result = ArgumentEscaper.EscapeAndConcatenate(new[] { "path with\\" });
            result.Should().Be("\"path with\\\\\"");
        }

        [Fact]
        public void EscapeAndConcatenate_BackslashBeforeQuote_Escaped()
        {
            // Backslash before quote: backslash doubled + quote escaped
            var result = ArgumentEscaper.EscapeAndConcatenate(new[] { "say\\\"hi" });
            result.Should().Be("say\\\\\\\"hi");
        }

        [Fact]
        public void EscapeAndConcatenate_InternalBackslashes_PreservedWhenNotBeforeQuote()
        {
            ArgumentEscaper.EscapeAndConcatenate(new[] { "C:\\Users\\test" }).Should().Be("C:\\Users\\test");
        }

        // =====================================================================
        // Embedded quotes
        // =====================================================================

        [Fact]
        public void EscapeAndConcatenate_EmbeddedQuote_Escaped()
        {
            var result = ArgumentEscaper.EscapeAndConcatenate(new[] { "say\"hi" });
            result.Should().Be("say\\\"hi");
        }

        // =====================================================================
        // Empty and special strings
        // =====================================================================

        [Fact]
        public void EscapeAndConcatenate_EmptyString_ReturnsEmpty()
        {
            ArgumentEscaper.EscapeAndConcatenate(new[] { "" }).Should().Be("");
        }

        [Fact]
        public void EscapeAndConcatenate_SingleCharacter_NoQuoting()
        {
            ArgumentEscaper.EscapeAndConcatenate(new[] { "a" }).Should().Be("a");
        }

        // =====================================================================
        // Mixed arguments
        // =====================================================================

        [Fact]
        public void EscapeAndConcatenate_MixedArgs_CorrectEscaping()
        {
            var result = ArgumentEscaper.EscapeAndConcatenate(new[]
            {
                "--flag",
                "simple",
                "has space",
                "C:\\path\\to\\file",
            });
            result.Should().Be("--flag simple \"has space\" C:\\path\\to\\file");
        }

        [Fact]
        public void EscapeAndConcatenate_RealWorldExample_DockerArgs()
        {
            var result = ArgumentEscaper.EscapeAndConcatenate(new[]
            {
                "run",
                "-d",
                "--name",
                "my-container",
                "-p",
                "8080:80",
                "nginx:latest",
            });
            result.Should().Be("run -d --name my-container -p 8080:80 nginx:latest");
        }

        [Fact]
        public void EscapeAndConcatenate_PathWithSpaces_Quoted()
        {
            var result = ArgumentEscaper.EscapeAndConcatenate(new[]
            {
                "--project",
                "C:\\Program Files\\My App\\app.csproj",
            });
            result.Should().Be("--project \"C:\\Program Files\\My App\\app.csproj\"");
        }
    }
}
