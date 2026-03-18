using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Extensions.Seq;
using Xunit;

namespace Tye2.UnitTests
{
    public class SeqExtensionTests
    {
        private static readonly FileInfo DummySource = new("C:\\test\\tye.yaml");

        private static ApplicationBuilder CreateApp(params ServiceBuilder[] services)
        {
            var app = new ApplicationBuilder(DummySource, "test-app", new ContainerEngine(null), null);
            foreach (var svc in services)
            {
                app.Services.Add(svc);
            }
            return app;
        }

        private static ExtensionContext CreateContext(ApplicationBuilder app, ExtensionContext.OperationKind operation, HostOptions? options = null)
        {
            var console = new TestConsole();
            var output = new OutputContext(console, Verbosity.Debug);
            return new ExtensionContext(app, options ?? new HostOptions(), output, operation);
        }

        private static ExtensionConfiguration CreateConfig(Dictionary<string, object>? data = null)
        {
            var config = new ExtensionConfiguration("seq");
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    config.Data[kvp.Key] = kvp.Value;
                }
            }
            return config;
        }

        // =====================================================================
        // LocalRun — service injection
        // =====================================================================

        [Fact]
        public async Task LocalRun_InjectsSeqService()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            app.Services.Should().ContainSingle(s => s.Name == "seq");
        }

        [Fact]
        public async Task LocalRun_SeqService_IsContainerWithCorrectImage()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            var seq = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "seq");
            seq.Image.Should().Be("datalust/seq");
        }

        [Fact]
        public async Task LocalRun_SeqService_HasCorrectBinding()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            var seq = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "seq");
            var binding = seq.Bindings.Should().ContainSingle().Subject;
            binding.Port.Should().Be(5341);
            binding.ContainerPort.Should().Be(80);
            binding.Protocol.Should().Be("http");
        }

        [Fact]
        public async Task LocalRun_SeqService_HasAcceptEulaEnvVar()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            var seq = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "seq");
            seq.EnvironmentVariables.Should().ContainSingle(e => e.Name == "ACCEPT_EULA");
            seq.EnvironmentVariables[0].Value.Should().Be("Y");
        }

        [Fact]
        public async Task LocalRun_SetsLoggingProvider()
        {
            var app = CreateApp();
            var options = new HostOptions();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun, options);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            options.LoggingProvider.Should().Be("seq=http://localhost:5341");
        }

        [Fact]
        public async Task LocalRun_ExistingLoggingProvider_DoesNotOverwrite()
        {
            var app = CreateApp();
            var options = new HostOptions { LoggingProvider = "custom=http://other:1234" };
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun, options);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            options.LoggingProvider.Should().Be("custom=http://other:1234");
        }

        // =====================================================================
        // logPath volume
        // =====================================================================

        [Fact]
        public async Task LocalRun_WithLogPath_AddsVolume()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);
            var config = CreateConfig(new Dictionary<string, object> { ["logPath"] = "/var/logs/seq" });

            await new SeqExtension().ProcessAsync(context, config);

            var seq = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "seq");
            seq.Volumes.Should().ContainSingle();
            seq.Volumes[0].Target.Should().Be("/data");
        }

        [Fact]
        public async Task LocalRun_WithoutLogPath_NoVolume()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            var seq = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "seq");
            seq.Volumes.Should().BeEmpty();
        }

        // =====================================================================
        // Dependencies
        // =====================================================================

        [Fact]
        public async Task LocalRun_AddsDependencyToExistingServices()
        {
            var web = new ContainerServiceBuilder("web", "nginx", ServiceSource.Configuration);
            var app = CreateApp(web);
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            web.Dependencies.Should().Contain("seq");
        }

        // =====================================================================
        // Duplicate detection
        // =====================================================================

        [Fact]
        public async Task LocalRun_SeqAlreadyExists_SkipsInjection()
        {
            var existing = new ContainerServiceBuilder("seq", "custom/seq", ServiceSource.Configuration);
            var app = CreateApp(existing);
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            app.Services.Where(s => s.Name == "seq").Should().HaveCount(1);
        }

        // =====================================================================
        // Deploy
        // =====================================================================

        [Fact]
        public async Task Deploy_AddsSidecarToProjects()
        {
            var project = new DotnetProjectServiceBuilder("web", new FileInfo("C:\\test\\web.csproj"), ServiceSource.Configuration)
            {
                AssemblyName = "web.dll",
            };
            var app = CreateApp(project);
            var context = CreateContext(app, ExtensionContext.OperationKind.Deploy);

            await new SeqExtension().ProcessAsync(context, CreateConfig());

            project.Sidecars.Should().ContainSingle(s => s.Name == "tye-diag-agent");
            var sidecar = project.Sidecars[0];
            sidecar.Args.Should().Contain("--provider:seq=service:seq");
            sidecar.Dependencies.Should().Contain("seq");
        }
    }
}
