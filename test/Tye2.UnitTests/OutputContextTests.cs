using System;
using System.CommandLine;
using System.CommandLine.IO;
using AwesomeAssertions;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests
{
    public class OutputContextTests
    {
        private static (OutputContext output, TestConsole console) Create(Verbosity verbosity = Verbosity.Debug)
        {
            var console = new TestConsole();
            var output = new OutputContext(console, verbosity);
            return (output, console);
        }

        // =====================================================================
        // WriteInfoLine
        // =====================================================================

        [Fact]
        public void WriteInfoLine_DebugVerbosity_WritesMessage()
        {
            var (output, console) = Create(Verbosity.Debug);

            output.WriteInfoLine("hello");

            console.Out.ToString().Should().Contain("hello");
        }

        [Fact]
        public void WriteInfoLine_InfoVerbosity_WritesMessage()
        {
            var (output, console) = Create(Verbosity.Info);

            output.WriteInfoLine("hello");

            console.Out.ToString().Should().Contain("hello");
        }

        // =====================================================================
        // WriteDebugLine
        // =====================================================================

        [Fact]
        public void WriteDebugLine_DebugVerbosity_WritesMessage()
        {
            var (output, console) = Create(Verbosity.Debug);

            output.WriteDebugLine("debug msg");

            console.Out.ToString().Should().Contain("debug msg");
        }

        [Fact]
        public void WriteDebugLine_InfoVerbosity_SuppressesMessage()
        {
            var (output, console) = Create(Verbosity.Info);

            output.WriteDebugLine("debug msg");

            console.Out.ToString().Should().NotContain("debug msg");
        }

        // =====================================================================
        // WriteAlwaysLine
        // =====================================================================

        [Fact]
        public void WriteAlwaysLine_InfoVerbosity_WritesMessage()
        {
            var (output, console) = Create(Verbosity.Info);

            output.WriteAlwaysLine("always");

            console.Out.ToString().Should().Contain("always");
        }

        // =====================================================================
        // WriteCommandLine
        // =====================================================================

        [Fact]
        public void WriteCommandLine_DebugVerbosity_WritesProcessAndArgs()
        {
            var (output, console) = Create(Verbosity.Debug);

            output.WriteCommandLine("dotnet", "build -c Release");

            var text = console.Out.ToString()!;
            text.Should().Contain("> dotnet build -c Release");
        }

        [Fact]
        public void WriteCommandLine_InfoVerbosity_SuppressesOutput()
        {
            var (output, console) = Create(Verbosity.Info);

            output.WriteCommandLine("dotnet", "build");

            console.Out.ToString().Should().NotContain("dotnet");
        }

        // =====================================================================
        // BeginStep / EndStep
        // =====================================================================

        [Fact]
        public void BeginStep_WritesTitle()
        {
            var (output, console) = Create(Verbosity.Info);

            using var step = output.BeginStep("Building...");

            console.Out.ToString().Should().Contain("Building...");
        }

        [Fact]
        public void BeginStep_IndentsSubsequentWrites()
        {
            var (output, console) = Create(Verbosity.Info);

            using (output.BeginStep("Step 1"))
            {
                output.WriteInfoLine("indented");
            }

            var lines = console.Out.ToString()!.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            // First line is "Step 1", second should be indented
            var indentedLine = lines[1];
            indentedLine.Should().StartWith("    ");
            indentedLine.Should().Contain("indented");
        }

        [Fact]
        public void BeginStep_AfterDispose_IndentResets()
        {
            var (output, console) = Create(Verbosity.Info);

            using (output.BeginStep("Step"))
            {
                var step = output.BeginStep("Step");
                step.MarkComplete();
                step.Dispose();
            }

            output.WriteInfoLine("after");

            var text = console.Out.ToString()!;
            // "after" should appear without indentation
            text.Should().Contain("\nafter");
        }

        [Fact]
        public void StepTracker_MarkComplete_SetsMessage()
        {
            var (output, _) = Create();

            var step = output.BeginStep("Test Step");
            step.Completed.Should().BeFalse();

            step.MarkComplete("Done!");
            step.Completed.Should().BeTrue();
            step.Message.Should().Be("Done!");
        }

        [Fact]
        public void StepTracker_MarkComplete_DefaultMessage()
        {
            var (output, _) = Create();

            var step = output.BeginStep("Building");
            step.MarkComplete();

            step.Message.Should().Be("Done Building");
        }

        [Fact]
        public void StepTracker_Title_ReturnsTitle()
        {
            var (output, _) = Create();

            var step = output.BeginStep("My Step");

            step.Title.Should().Be("My Step");
            step.Dispose();
        }

        [Fact]
        public void StepTracker_DoubleDispose_NoError()
        {
            var (output, _) = Create();

            var step = output.BeginStep("Test");
            step.Dispose();
            step.Dispose(); // Should not throw
        }

        // =====================================================================
        // Nested steps
        // =====================================================================

        [Fact]
        public void NestedSteps_IncreaseIndent()
        {
            var (output, console) = Create(Verbosity.Info);

            using (output.BeginStep("Outer"))
            {
                using (output.BeginStep("Inner"))
                {
                    output.WriteInfoLine("deep");
                }
            }

            var text = console.Out.ToString()!;
            // "deep" should be indented by 8 spaces (2 levels)
            text.Should().Contain("        deep");
        }

        // =====================================================================
        // Constructor validation
        // =====================================================================

        [Fact]
        public void Constructor_NullConsole_Throws()
        {
            var act = () => new OutputContext(null!, Verbosity.Info);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void BeginStep_NullTitle_Throws()
        {
            var (output, _) = Create();

            var act = () => output.BeginStep(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        // =====================================================================
        // CapturedCommandOutput
        // =====================================================================

        [Fact]
        public void CapturedOutput_StdOut_DebugVerbosity_WritesGrayOutput()
        {
            var (output, console) = Create(Verbosity.Debug);

            var captured = output.Capture();
            captured.StdOut("stdout line");

            console.Out.ToString().Should().Contain("stdout line");
        }

        [Fact]
        public void CapturedOutput_StdOut_InfoVerbosity_SuppressesOutput()
        {
            var (output, console) = Create(Verbosity.Info);

            var captured = output.Capture();
            captured.StdOut("stdout line");

            console.Out.ToString().Should().NotContain("stdout line");
        }

        [Fact]
        public void CapturedOutput_StdErr_InfoVerbosity_WritesOutput()
        {
            var (output, console) = Create(Verbosity.Info);

            var captured = output.Capture();
            captured.StdErr("error line");

            console.Out.ToString().Should().Contain("error line");
        }
    }
}
