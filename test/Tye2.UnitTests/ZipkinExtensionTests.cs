using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Extensions.Zipkin;
using Xunit;

namespace Tye2.UnitTests
{
    public class ZipkinExtensionTests
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

        private static ExtensionConfiguration CreateConfig()
        {
            return new ExtensionConfiguration("zipkin");
        }

        // =====================================================================
        // LocalRun — service injection
        // =====================================================================

        [Fact]
        public async Task LocalRun_InjectsZipkinService()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);
            var extension = new ZipkinExtension();

            await extension.ProcessAsync(context, CreateConfig());

            app.Services.Should().ContainSingle(s => s.Name == "zipkin");
        }

        [Fact]
        public async Task LocalRun_ZipkinService_IsContainer()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ZipkinExtension().ProcessAsync(context, CreateConfig());

            var zipkin = app.Services.Single(s => s.Name == "zipkin");
            zipkin.Should().BeOfType<ContainerServiceBuilder>();
            ((ContainerServiceBuilder)zipkin).Image.Should().Be("openzipkin/zipkin");
        }

        [Fact]
        public async Task LocalRun_ZipkinService_HasCorrectBinding()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ZipkinExtension().ProcessAsync(context, CreateConfig());

            var zipkin = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "zipkin");
            var binding = zipkin.Bindings.Should().ContainSingle().Subject;
            binding.Port.Should().Be(9411);
            binding.ContainerPort.Should().Be(9411);
            binding.Protocol.Should().Be("http");
        }

        [Fact]
        public async Task LocalRun_SetsDistributedTraceProvider()
        {
            var app = CreateApp();
            var options = new HostOptions();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun, options);

            await new ZipkinExtension().ProcessAsync(context, CreateConfig());

            options.DistributedTraceProvider.Should().Be("zipkin=http://localhost:9411");
        }

        [Fact]
        public async Task LocalRun_ExistingTraceProvider_DoesNotOverwrite()
        {
            var app = CreateApp();
            var options = new HostOptions { DistributedTraceProvider = "custom=http://other:9999" };
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun, options);

            await new ZipkinExtension().ProcessAsync(context, CreateConfig());

            options.DistributedTraceProvider.Should().Be("custom=http://other:9999");
        }

        // =====================================================================
        // LocalRun — dependencies
        // =====================================================================

        [Fact]
        public async Task LocalRun_AddsDependencyToExistingServices()
        {
            var web = new ContainerServiceBuilder("web", "nginx", ServiceSource.Configuration);
            var app = CreateApp(web);
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ZipkinExtension().ProcessAsync(context, CreateConfig());

            web.Dependencies.Should().Contain("zipkin");
        }

        [Fact]
        public async Task LocalRun_ZipkinDoesNotDependOnItself()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ZipkinExtension().ProcessAsync(context, CreateConfig());

            var zipkin = app.Services.Single(s => s.Name == "zipkin");
            zipkin.Dependencies.Should().NotContain("zipkin");
        }

        // =====================================================================
        // Duplicate detection
        // =====================================================================

        [Fact]
        public async Task LocalRun_ZipkinAlreadyExists_SkipsInjection()
        {
            var existing = new ContainerServiceBuilder("zipkin", "custom/zipkin", ServiceSource.Configuration);
            var app = CreateApp(existing);
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ZipkinExtension().ProcessAsync(context, CreateConfig());

            app.Services.Where(s => s.Name == "zipkin").Should().HaveCount(1);
            ((ContainerServiceBuilder)app.Services.Single(s => s.Name == "zipkin")).Image.Should().Be("custom/zipkin");
        }

        // =====================================================================
        // Deploy — sidecar injection
        // =====================================================================

        [Fact]
        public async Task Deploy_AddsSidecarToProjects()
        {
            var project = new DotnetProjectServiceBuilder("web", new FileInfo("C:\\test\\web.csproj"), ServiceSource.Configuration)
            {
                AssemblyName = "web.dll",
            };
            project.Bindings.Add(new BindingBuilder { Protocol = "http" });
            var app = CreateApp(project);
            var context = CreateContext(app, ExtensionContext.OperationKind.Deploy);

            await new ZipkinExtension().ProcessAsync(context, CreateConfig());

            project.Sidecars.Should().ContainSingle(s => s.Name == "tye-diag-agent");
            var sidecar = project.Sidecars[0];
            sidecar.Args.Should().Contain("--provider:zipkin=service:zipkin");
            sidecar.Dependencies.Should().Contain("zipkin");
        }
    }
}
