using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Core.ConfigModel;
using Xunit;

namespace Tye2.UnitTests
{
    public class ApplicationFactoryTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly OutputContext _output;

        public ApplicationFactoryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tye2_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _output = new OutputContext(new TestConsole(), Verbosity.Debug);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private FileInfo CreateTyeYaml(string content, string fileName = "tye.yaml")
        {
            var path = Path.Combine(_tempDir, fileName);
            File.WriteAllText(path, content);
            return new FileInfo(path);
        }

        // =====================================================================
        // Null / Invalid Arguments
        // =====================================================================

        [Fact]
        public async Task CreateAsync_NullSource_ThrowsArgumentNullException()
        {
            var act = () => ApplicationFactory.CreateAsync(_output, null!);
            await act.Should().ThrowExactlyAsync<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Fact]
        public async Task CreateAsync_UnsupportedFileExtension_ThrowsCommandException()
        {
            var file = Path.Combine(_tempDir, "config.txt");
            File.WriteAllText(file, "content");
            var act = () => ApplicationFactory.CreateAsync(_output, new FileInfo(file));
            await act.Should().ThrowAsync<CommandException>();
        }

        // =====================================================================
        // Basic Application Properties
        // =====================================================================

        [Fact]
        public async Task CreateAsync_SetsApplicationName()
        {
            var file = CreateTyeYaml(@"
name: my-test-app
services:
  - name: redis
    image: redis:latest
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Name.Should().Be("my-test-app");
        }

        [Fact]
        public async Task CreateAsync_SetsSourceFile()
        {
            var file = CreateTyeYaml(@"
name: src-test
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Source.FullName.Should().Be(file.FullName);
        }

        [Fact]
        public async Task CreateAsync_SetsNamespace()
        {
            var file = CreateTyeYaml(@"
name: ns-test
namespace: my-namespace
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Namespace.Should().Be("my-namespace");
        }

        [Fact]
        public async Task CreateAsync_SetsNetwork()
        {
            var file = CreateTyeYaml(@"
name: net-test
network: my-network
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Network.Should().Be("my-network");
        }

        [Fact]
        public async Task CreateAsync_SetsDashboardPort()
        {
            var file = CreateTyeYaml(@"
name: dash-test
dashboardPort: 9999
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.DashboardPort.Should().Be(9999);
        }

        // =====================================================================
        // Registry
        // =====================================================================

        [Fact]
        public async Task CreateAsync_WithRegistry_SetsRegistryHostname()
        {
            var file = CreateTyeYaml(@"
name: reg-test
registry:
  name: myregistry.azurecr.io
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Registry.Should().NotBeNull();
            app.Registry!.Hostname.Should().Be("myregistry.azurecr.io");
        }

        // =====================================================================
        // Image-Based Services (ContainerServiceBuilder)
        // =====================================================================

        [Fact]
        public async Task CreateAsync_ImageService_CreatesContainerServiceBuilder()
        {
            var file = CreateTyeYaml(@"
name: container-test
services:
  - name: redis
    image: redis:7
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var svc = app.Services.Should().ContainSingle().Subject;
            svc.Should().BeOfType<ContainerServiceBuilder>();
            svc.Name.Should().Be("redis");
            ((ContainerServiceBuilder)svc).Image.Should().Be("redis:7");
        }

        [Fact]
        public async Task CreateAsync_ImageService_SetsReplicas()
        {
            var file = CreateTyeYaml(@"
name: replica-test
services:
  - name: redis
    image: redis
    replicas: 3
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var container = app.Services.Single().Should().BeOfType<ContainerServiceBuilder>().Subject;
            container.Replicas.Should().Be(3);
        }

        [Fact]
        public async Task CreateAsync_ImageService_DefaultReplicasIsOne()
        {
            var file = CreateTyeYaml(@"
name: default-replica
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var container = app.Services.Single().Should().BeOfType<ContainerServiceBuilder>().Subject;
            container.Replicas.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_ImageService_SetsArgs()
        {
            var file = CreateTyeYaml(@"
name: args-test
services:
  - name: redis
    image: redis
    args: --appendonly yes
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var container = app.Services.Single().Should().BeOfType<ContainerServiceBuilder>().Subject;
            container.Args.Should().Be("--appendonly yes");
        }

        [Fact]
        public async Task CreateAsync_MultipleImageServices()
        {
            var file = CreateTyeYaml(@"
name: multi-svc
services:
  - name: redis
    image: redis
  - name: postgres
    image: postgres:15
  - name: rabbitmq
    image: rabbitmq:management
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Services.Should().HaveCount(3);
            app.Services.Select(s => s.Name).Should().BeEquivalentTo("redis", "postgres", "rabbitmq");
        }

        // =====================================================================
        // External Services
        // =====================================================================

        [Fact]
        public async Task CreateAsync_ExternalService_CreatesExternalServiceBuilder()
        {
            var file = CreateTyeYaml(@"
name: ext-test
services:
  - name: my-external
    external: true
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var svc = app.Services.Should().ContainSingle().Subject;
            svc.Should().BeOfType<ExternalServiceBuilder>();
            svc.Name.Should().Be("my-external");
        }

        [Fact]
        public async Task CreateAsync_ExternalService_WithEnvironmentVariable_ThrowsCommandException()
        {
            var file = CreateTyeYaml(@"
name: ext-env-test
services:
  - name: my-external
    external: true
    env:
      - name: SOME_VAR
        value: some-value
");
            var act = () => ApplicationFactory.CreateAsync(_output, file);
            await act.Should().ThrowAsync<CommandException>()
                .WithMessage("*External services do not support environment variables*");
        }

        [Fact]
        public async Task CreateAsync_ExternalService_WithVolume_ThrowsCommandException()
        {
            var file = CreateTyeYaml(@"
name: ext-vol-test
services:
  - name: my-external
    external: true
    volumes:
      - source: mydata
        target: /data
");
            var act = () => ApplicationFactory.CreateAsync(_output, file);
            await act.Should().ThrowAsync<CommandException>()
                .WithMessage("*External services do not support volumes*");
        }

        // =====================================================================
        // Bindings
        // =====================================================================

        [Fact]
        public async Task CreateAsync_ImageService_WithBindings()
        {
            var file = CreateTyeYaml(@"
name: binding-test
services:
  - name: redis
    image: redis
    bindings:
      - port: 6379
        containerPort: 6379
        protocol: tcp
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var svc = app.Services.Single();
            var binding = svc.Bindings.Should().ContainSingle().Subject;
            binding.Port.Should().Be(6379);
            binding.ContainerPort.Should().Be(6379);
            binding.Protocol.Should().Be("tcp");
        }

        [Fact]
        public async Task CreateAsync_ImageService_WithMultipleBindings()
        {
            var file = CreateTyeYaml(@"
name: multi-binding
services:
  - name: web
    image: nginx
    bindings:
      - name: http
        port: 80
        protocol: http
      - name: https
        port: 443
        protocol: https
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Services.Single().Bindings.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreateAsync_ImageService_WithConnectionString()
        {
            var file = CreateTyeYaml(@"
name: connstr-test
services:
  - name: sql
    image: mcr.microsoft.com/mssql/server
    bindings:
      - connectionString: Server=localhost;Database=mydb
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var binding = app.Services.Single().Bindings.Should().ContainSingle().Subject;
            binding.ConnectionString.Should().Be("Server=localhost;Database=mydb");
        }

        [Fact]
        public async Task CreateAsync_ImageService_NoBindings_BindingsListEmpty()
        {
            var file = CreateTyeYaml(@"
name: no-binding
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Services.Single().Bindings.Should().BeEmpty();
        }

        // =====================================================================
        // Environment Variables on Container Services
        // =====================================================================

        [Fact]
        public async Task CreateAsync_ContainerService_WithEnvironmentVariables()
        {
            var file = CreateTyeYaml(@"
name: env-test
services:
  - name: postgres
    image: postgres
    env:
      - name: POSTGRES_PASSWORD
        value: secret
      - name: POSTGRES_DB
        value: mydb
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var container = app.Services.Single().Should().BeOfType<ContainerServiceBuilder>().Subject;
            container.EnvironmentVariables.Should().HaveCount(2);
            container.EnvironmentVariables.First(e => e.Name == "POSTGRES_PASSWORD").Value.Should().Be("secret");
            container.EnvironmentVariables.First(e => e.Name == "POSTGRES_DB").Value.Should().Be("mydb");
        }

        // =====================================================================
        // Volumes on Container Services
        // =====================================================================

        [Fact]
        public async Task CreateAsync_ContainerService_WithVolumes()
        {
            var file = CreateTyeYaml(@"
name: vol-test
services:
  - name: postgres
    image: postgres
    volumes:
      - name: pgdata
        target: /var/lib/postgresql/data
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var container = app.Services.Single().Should().BeOfType<ContainerServiceBuilder>().Subject;
            var volume = container.Volumes.Should().ContainSingle().Subject;
            volume.Name.Should().Be("pgdata");
            volume.Target.Should().Be("/var/lib/postgresql/data");
        }

        // =====================================================================
        // Ingress
        // =====================================================================

        [Fact]
        public async Task CreateAsync_WithIngress_CreatesIngressBuilder()
        {
            var file = CreateTyeYaml(@"
name: ingress-test
services:
  - name: web
    image: nginx
ingress:
  - name: my-ingress
    bindings:
      - port: 8080
        protocol: http
    rules:
      - path: /
        service: web
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var ingress = app.Ingress.Should().ContainSingle().Subject;
            ingress.Name.Should().Be("my-ingress");
        }

        [Fact]
        public async Task CreateAsync_Ingress_BindingsAreMapped()
        {
            var file = CreateTyeYaml(@"
name: ingress-binding
services:
  - name: web
    image: nginx
ingress:
  - name: gw
    bindings:
      - name: http
        port: 80
        protocol: http
      - name: https
        port: 443
        protocol: https
    rules:
      - path: /
        service: web
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var ingress = app.Ingress.Single();
            ingress.Bindings.Should().HaveCount(2);
            ingress.Bindings
                .Select(b => new { b.Name, b.Port, b.Protocol })
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        new { Name = "http", Port = (int?)80, Protocol = "http" },
                        new { Name = "https", Port = (int?)443, Protocol = "https" },
                    });
        }

        [Fact]
        public async Task CreateAsync_Ingress_DefaultProtocolIsHttp()
        {
            var file = CreateTyeYaml(@"
name: ingress-default-proto
services:
  - name: web
    image: nginx
ingress:
  - name: gw
    bindings:
      - port: 8080
    rules:
      - path: /
        service: web
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Ingress.Single().Bindings.Single().Protocol.Should().Be("http");
        }

        [Fact]
        public async Task CreateAsync_Ingress_RulesAreMapped()
        {
            var file = CreateTyeYaml(@"
name: ingress-rules
services:
  - name: web
    image: nginx
  - name: api
    image: myapi
ingress:
  - name: gw
    bindings:
      - port: 80
    rules:
      - path: /
        service: web
      - path: /api
        service: api
        preservePath: true
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var rules = app.Ingress.Single().Rules;
            rules.Should().HaveCount(2);
            rules
                .Select(r => new { r.Path, r.Service, r.PreservePath })
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                        new { Path = "/", Service = "web", PreservePath = false },
                        new { Path = "/api", Service = "api", PreservePath = true },
                    });
        }

        [Fact]
        public async Task CreateAsync_Ingress_WithHostRule()
        {
            var file = CreateTyeYaml(@"
name: ingress-host
services:
  - name: web
    image: nginx
ingress:
  - name: gw
    bindings:
      - port: 80
    rules:
      - host: example.com
        service: web
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Ingress.Single().Rules.Single().Host.Should().Be("example.com");
        }

        [Fact]
        public async Task CreateAsync_Ingress_DefaultReplicasIsOne()
        {
            var file = CreateTyeYaml(@"
name: ingress-replicas
services:
  - name: web
    image: nginx
ingress:
  - name: gw
    bindings:
      - port: 80
    rules:
      - path: /
        service: web
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Ingress.Single().Replicas.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_Ingress_CustomReplicas()
        {
            var file = CreateTyeYaml(@"
name: ingress-custom-replicas
services:
  - name: web
    image: nginx
ingress:
  - name: gw
    replicas: 3
    bindings:
      - port: 80
    rules:
      - path: /
        service: web
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Ingress.Single().Replicas.Should().Be(3);
        }

        // =====================================================================
        // Extensions
        // =====================================================================

        [Fact]
        public async Task CreateAsync_WithExtension_AddsExtensionConfiguration()
        {
            var file = CreateTyeYaml(@"
name: ext-test
extensions:
  - name: dapr
    log-level: debug
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var ext = app.Extensions.Should().ContainSingle().Subject;
            ext.Name.Should().Be("dapr");
            ext.Data.Should().ContainKey("log-level");
        }

        [Fact]
        public async Task CreateAsync_WithMultipleExtensions()
        {
            var file = CreateTyeYaml(@"
name: multi-ext
extensions:
  - name: dapr
  - name: zipkin
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Extensions.Should().HaveCount(2);
            app.Extensions.Select(e => e.Name).Should().BeEquivalentTo("dapr", "zipkin");
        }

        // =====================================================================
        // Service Filter
        // =====================================================================

        [Fact]
        public async Task CreateAsync_WithServicesFilter_OnlyIncludesMatchingServices()
        {
            var file = CreateTyeYaml(@"
name: filter-test
services:
  - name: redis
    image: redis
    tags:
      - backend
  - name: nginx
    image: nginx
    tags:
      - frontend
  - name: postgres
    image: postgres
    tags:
      - backend
");
            var filter = new ApplicationFactoryFilter
            {
                ServicesFilter = svc => svc.Tags.Contains("backend")
            };
            var app = await ApplicationFactory.CreateAsync(_output, file, filter: filter);
            app.Services.Should().HaveCount(2);
            app.Services.Select(s => s.Name).Should().BeEquivalentTo("redis", "postgres");
        }

        [Fact]
        public async Task CreateAsync_WithServicesFilter_NoMatch_ReturnsEmptyServices()
        {
            var file = CreateTyeYaml(@"
name: empty-filter
services:
  - name: redis
    image: redis
");
            var filter = new ApplicationFactoryFilter
            {
                ServicesFilter = _ => false
            };
            var app = await ApplicationFactory.CreateAsync(_output, file, filter: filter);
            app.Services.Should().BeEmpty();
        }

        // =====================================================================
        // Ingress Filter
        // =====================================================================

        [Fact]
        public async Task CreateAsync_WithIngressFilter_OnlyIncludesMatchingIngress()
        {
            var file = CreateTyeYaml(@"
name: ingress-filter
services:
  - name: web
    image: nginx
ingress:
  - name: public-gw
    bindings:
      - port: 80
    rules:
      - path: /
        service: web
  - name: internal-gw
    bindings:
      - port: 8080
    rules:
      - path: /
        service: web
");
            var filter = new ApplicationFactoryFilter
            {
                IngressFilter = ingress => ingress.Name == "public-gw"
            };
            var app = await ApplicationFactory.CreateAsync(_output, file, filter: filter);
            app.Ingress.Should().ContainSingle().Which.Name.Should().Be("public-gw");
        }

        // =====================================================================
        // Mixed Service Types
        // =====================================================================

        [Fact]
        public async Task CreateAsync_MixedContainerAndExternalServices()
        {
            var file = CreateTyeYaml(@"
name: mixed-test
services:
  - name: redis
    image: redis
  - name: external-api
    external: true
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Services.Should().HaveCount(2);
            app.Services.First(s => s.Name == "redis").Should().BeOfType<ContainerServiceBuilder>();
            app.Services.First(s => s.Name == "external-api").Should().BeOfType<ExternalServiceBuilder>();
        }

        // =====================================================================
        // Liveness and Readiness Probes
        // =====================================================================

        [Fact]
        public async Task CreateAsync_ContainerService_WithLivenessProbe()
        {
            var file = CreateTyeYaml(@"
name: probe-test
services:
  - name: web
    image: nginx
    liveness:
      http:
        path: /health
        port: 80
      period: 10
      timeout: 5
      successThreshold: 1
      failureThreshold: 3
      initialDelay: 15
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var container = app.Services.Single().Should().BeOfType<ContainerServiceBuilder>().Subject;
            container.Liveness.Should().NotBeNull();
            container.Liveness!.Http.Should().NotBeNull();
            container.Liveness.Http!.Path.Should().Be("/health");
            container.Liveness.Http.Port.Should().Be(80);
            container.Liveness.Period.Should().Be(10);
            container.Liveness.Timeout.Should().Be(5);
            container.Liveness.SuccessThreshold.Should().Be(1);
            container.Liveness.FailureThreshold.Should().Be(3);
            container.Liveness.InitialDelay.Should().Be(15);
        }

        [Fact]
        public async Task CreateAsync_ContainerService_WithReadinessProbe()
        {
            var file = CreateTyeYaml(@"
name: readiness-test
services:
  - name: web
    image: nginx
    readiness:
      http:
        path: /ready
        port: 80
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var container = app.Services.Single().Should().BeOfType<ContainerServiceBuilder>().Subject;
            container.Readiness.Should().NotBeNull();
            container.Readiness!.Http!.Path.Should().Be("/ready");
        }

        [Fact]
        public async Task CreateAsync_ContainerService_NoProbes_ProbesAreNull()
        {
            var file = CreateTyeYaml(@"
name: no-probes
services:
  - name: redis
    image: redis
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var container = app.Services.Single().Should().BeOfType<ContainerServiceBuilder>().Subject;
            container.Liveness.Should().BeNull();
            container.Readiness.Should().BeNull();
        }

        // =====================================================================
        // Binding Name and Host
        // =====================================================================

        [Fact]
        public async Task CreateAsync_Binding_WithNameAndHost()
        {
            var file = CreateTyeYaml(@"
name: named-binding
services:
  - name: redis
    image: redis
    bindings:
      - name: main
        host: localhost
        port: 6379
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var binding = app.Services.Single().Bindings.Should().ContainSingle().Subject;
            binding.Name.Should().Be("main");
            binding.Host.Should().Be("localhost");
        }

        // =====================================================================
        // No Services
        // =====================================================================

        [Fact]
        public async Task CreateAsync_NoServices_ReturnsEmptyServicesList()
        {
            var file = CreateTyeYaml(@"
name: empty-app
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Services.Should().BeEmpty();
        }
        
        // =====================================================================
        // Docker Compose via ApplicationFactory
        // =====================================================================

        [Fact]
        public async Task CreateAsync_DockerCompose_ParsesServices()
        {
            var file = CreateTyeYaml(@"
version: '3'
services:
  redis:
    image: redis:latest
    ports:
      - 6379:6379
", "docker-compose.yaml");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Services.Should().Contain(s => s.Name == "redis");
        }

        // =====================================================================
        // External Service Bindings
        // =====================================================================

        [Fact]
        public async Task CreateAsync_ExternalService_WithBindings()
        {
            var file = CreateTyeYaml(@"
name: ext-binding
services:
  - name: external-db
    external: true
    bindings:
      - connectionString: Server=remote;Database=prod
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var svc = app.Services.Single();
            svc.Should().BeOfType<ExternalServiceBuilder>();
            var binding = svc.Bindings.Should().ContainSingle().Subject;
            binding.ConnectionString.Should().Be("Server=remote;Database=prod");
        }

        // =====================================================================
        // Ingress IP Address
        // =====================================================================

        [Fact]
        public async Task CreateAsync_Ingress_WithIPAddress()
        {
            var file = CreateTyeYaml(@"
name: ingress-ip
services:
  - name: web
    image: nginx
ingress:
  - name: gw
    bindings:
      - port: 80
        ip: ""127.0.0.1""
    rules:
      - path: /
        service: web
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            app.Ingress.Single().Bindings.Single().IPAddress.Should().Be("127.0.0.1");
        }

        // =====================================================================
        // Container Service with All Properties
        // =====================================================================

        [Fact]
        public async Task CreateAsync_ContainerService_FullConfiguration()
        {
            var file = CreateTyeYaml(@"
name: full-config
services:
  - name: postgres
    image: postgres:15
    replicas: 2
    args: -c shared_buffers=256MB
    bindings:
      - port: 5432
        containerPort: 5432
        protocol: tcp
    env:
      - name: POSTGRES_PASSWORD
        value: secret
    volumes:
      - name: pgdata
        target: /var/lib/postgresql/data
    liveness:
      http:
        path: /health
        port: 5432
    readiness:
      http:
        path: /ready
        port: 5432
");
            var app = await ApplicationFactory.CreateAsync(_output, file);
            var container = app.Services.Single().Should().BeOfType<ContainerServiceBuilder>().Subject;

            container.Name.Should().Be("postgres");
            container.Image.Should().Be("postgres:15");
            container.Replicas.Should().Be(2);
            container.Args.Should().Be("-c shared_buffers=256MB");
            container.Volumes.Should().ContainSingle();
            container.EnvironmentVariables.Should().ContainSingle();
            container.Liveness.Should().NotBeNull();
            container.Readiness.Should().NotBeNull();
            app.Services.Single().Bindings.Should().ContainSingle();
        }
    }
}
