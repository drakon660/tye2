using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Extensions.Elastic;
using Xunit;

namespace Tye2.UnitTests
{
    public class ElasticStackExtensionTests
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
            var config = new ExtensionConfiguration("elastic");
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
        public async Task LocalRun_InjectsElasticService()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            app.Services.Should().ContainSingle(s => s.Name == "elastic");
        }

        [Fact]
        public async Task LocalRun_ElasticService_IsContainerWithCorrectImage()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            var elastic = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "elastic");
            elastic.Image.Should().Be("sebp/elk");
        }

        [Fact]
        public async Task LocalRun_ElasticService_HasKibanaAndHttpBindings()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            var elastic = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "elastic");
            elastic.Bindings.Should().HaveCount(2);

            var kibana = elastic.Bindings.Single(b => b.Name == "kibana");
            kibana.Port.Should().Be(5601);
            kibana.ContainerPort.Should().Be(5601);

            var http = elastic.Bindings.Single(b => b.Name != "kibana");
            http.Port.Should().Be(9200);
            http.ContainerPort.Should().Be(9200);
        }

        [Fact]
        public async Task LocalRun_SetsLoggingProvider()
        {
            var app = CreateApp();
            var options = new HostOptions();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun, options);

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            options.LoggingProvider.Should().Be("elastic=http://localhost:9200");
        }

        [Fact]
        public async Task LocalRun_ExistingLoggingProvider_DoesNotOverwrite()
        {
            var app = CreateApp();
            var options = new HostOptions { LoggingProvider = "custom=http://other:1234" };
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun, options);

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

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
            var config = CreateConfig(new Dictionary<string, object> { ["logPath"] = "/var/logs/elk" });

            await new ElasticStackExtension().ProcessAsync(context, config);

            var elastic = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "elastic");
            elastic.Volumes.Should().ContainSingle();
            elastic.Volumes[0].Target.Should().Be("/var/lib/elasticsearch");
        }

        [Fact]
        public async Task LocalRun_WithoutLogPath_NoVolume()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            var elastic = (ContainerServiceBuilder)app.Services.Single(s => s.Name == "elastic");
            elastic.Volumes.Should().BeEmpty();
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

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            web.Dependencies.Should().Contain("elastic");
        }

        [Fact]
        public async Task LocalRun_ElasticDoesNotDependOnItself()
        {
            var app = CreateApp();
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            var elastic = app.Services.Single(s => s.Name == "elastic");
            elastic.Dependencies.Should().NotContain("elastic");
        }

        // =====================================================================
        // Duplicate detection
        // =====================================================================

        [Fact]
        public async Task LocalRun_ElasticAlreadyExists_SkipsInjection()
        {
            var existing = new ContainerServiceBuilder("elastic", "custom/elk", ServiceSource.Configuration);
            var app = CreateApp(existing);
            var context = CreateContext(app, ExtensionContext.OperationKind.LocalRun);

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            app.Services.Where(s => s.Name == "elastic").Should().HaveCount(1);
        }

        // =====================================================================
        // Deploy — sidecar + remove kibana binding
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

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            project.Sidecars.Should().ContainSingle(s => s.Name == "tye-diag-agent");
            var sidecar = project.Sidecars[0];
            sidecar.Args.Should().Contain("--provider:elastic=service:elastic");
            sidecar.Dependencies.Should().Contain("elastic");
        }

        [Fact]
        public async Task Deploy_RemovesKibanaBinding()
        {
            var project = new DotnetProjectServiceBuilder("web", new FileInfo("C:\\test\\web.csproj"), ServiceSource.Configuration)
            {
                AssemblyName = "web.dll",
            };
            var app = CreateApp(project);
            var context = CreateContext(app, ExtensionContext.OperationKind.Deploy);

            await new ElasticStackExtension().ProcessAsync(context, CreateConfig());

            var elastic = app.Services.Single(s => s.Name == "elastic");
            elastic.Bindings.Should().NotContain(b => b.Name == "kibana");
        }
    }
}
