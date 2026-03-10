using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tye2.Core;
using Tye2.Hosting;
using Tye2.Hosting.Model;
using Xunit;

namespace Tye2.UnitTests
{
    public class PortAssignerTests
    {
        private readonly ILogger _logger = NullLogger.Instance;
        private readonly FileInfo _dummySource = new(Path.Combine(Path.GetTempPath(), "tye.yaml"));

        private Application CreateApp(Dictionary<string, Service> services)
        {
            return new Application("test-app", _dummySource, null, services, new ContainerEngine(null));
        }

        private Service CreateService(string name, RunInfo? runInfo, int replicas = 1,
            List<ServiceBinding>? bindings = null, Probe? readiness = null)
        {
            var desc = new ServiceDescription(name, runInfo)
            {
                Replicas = replicas,
                Readiness = readiness,
            };
            if (bindings != null)
            {
                foreach (var b in bindings)
                    desc.Bindings.Add(b);
            }
            return new Service(desc, ServiceSource.Configuration);
        }

        // =====================================================================
        // Skips services without RunInfo
        // =====================================================================

        [Fact]
        public async Task StartAsync_ServiceWithoutRunInfo_SkipsService()
        {
            var svc = CreateService("external", runInfo: null, bindings: new List<ServiceBinding>
            {
                new() { Port = 5000, Protocol = "http" },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["external"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            // Port should remain unchanged, no replica ports assigned
            svc.Description.Bindings[0].Port.Should().Be(5000);
            svc.Description.Bindings[0].ReplicaPorts.Should().BeEmpty();
        }

        // =====================================================================
        // Auto-assigns port when null
        // =====================================================================

        [Fact]
        public async Task StartAsync_NullPort_AssignsPort()
        {
            var svc = CreateService("web", new DockerRunInfo("nginx", null), bindings: new List<ServiceBinding>
            {
                new() { Port = null, Protocol = "http" },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            svc.Description.Bindings[0].Port.Should().NotBeNull();
            svc.Description.Bindings[0].Port.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task StartAsync_ExistingPort_PreservesPort()
        {
            var svc = CreateService("web", new DockerRunInfo("nginx", null), bindings: new List<ServiceBinding>
            {
                new() { Port = 9999, Protocol = "http" },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            svc.Description.Bindings[0].Port.Should().Be(9999);
        }

        // =====================================================================
        // Single replica, no readiness — port maps to itself
        // =====================================================================

        [Fact]
        public async Task StartAsync_SingleReplica_NoReadiness_PortMapsToItself()
        {
            var svc = CreateService("web", new DockerRunInfo("nginx", null), replicas: 1, bindings: new List<ServiceBinding>
            {
                new() { Port = 5000, Protocol = "http" },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            var binding = svc.Description.Bindings[0];
            binding.ReplicaPorts.Should().ContainSingle().Which.Should().Be(5000);
        }

        // =====================================================================
        // Multiple replicas — reserves separate ports
        // =====================================================================

        [Fact]
        public async Task StartAsync_MultipleReplicas_ReservesSeparatePorts()
        {
            var svc = CreateService("web", new DockerRunInfo("nginx", null), replicas: 3, bindings: new List<ServiceBinding>
            {
                new() { Port = 5000, Protocol = "http" },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            var binding = svc.Description.Bindings[0];
            binding.ReplicaPorts.Should().HaveCount(3);
            binding.ReplicaPorts.Should().OnlyHaveUniqueItems();
            // Replica ports should differ from main port
            binding.ReplicaPorts.Should().NotContain(5000);
        }

        // =====================================================================
        // Single replica with readiness — still proxies (reserves separate port)
        // =====================================================================

        [Fact]
        public async Task StartAsync_SingleReplica_WithReadiness_ReservesReplicaPort()
        {
            var readiness = new Probe { Http = new HttpProber { Path = "/ready" } };
            var svc = CreateService("web", new DockerRunInfo("nginx", null), replicas: 1,
                readiness: readiness,
                bindings: new List<ServiceBinding>
                {
                    new() { Port = 5000, Protocol = "http" },
                });
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            var binding = svc.Description.Bindings[0];
            binding.ReplicaPorts.Should().ContainSingle();
            // With readiness, the replica port should be a different reserved port
            binding.ReplicaPorts[0].Should().NotBe(5000);
        }

        // =====================================================================
        // HTTP/HTTPS container port defaults
        // =====================================================================

        [Fact]
        public async Task StartAsync_HttpBinding_DefaultContainerPort80()
        {
            var svc = CreateService("web", new DockerRunInfo("nginx", null), bindings: new List<ServiceBinding>
            {
                new() { Port = 5000, Protocol = "http" },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            svc.Description.Bindings[0].ContainerPort.Should().Be(80);
        }

        [Fact]
        public async Task StartAsync_HttpsBinding_DefaultContainerPort443()
        {
            var svc = CreateService("web", new DockerRunInfo("nginx", null), bindings: new List<ServiceBinding>
            {
                new() { Port = 5000, Protocol = "https" },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            svc.Description.Bindings[0].ContainerPort.Should().Be(443);
        }

        [Fact]
        public async Task StartAsync_HttpBinding_PresetContainerPort_DoesNotOverwrite()
        {
            var svc = CreateService("web", new DockerRunInfo("nginx", null), bindings: new List<ServiceBinding>
            {
                new() { Port = 5000, Protocol = "http", ContainerPort = 8080 },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            svc.Description.Bindings[0].ContainerPort.Should().Be(8080);
        }

        [Fact]
        public async Task StartAsync_TcpBinding_NoContainerPortDefault()
        {
            var svc = CreateService("redis", new DockerRunInfo("redis", null), bindings: new List<ServiceBinding>
            {
                new() { Port = 6379, Protocol = "tcp" },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["redis"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            svc.Description.Bindings[0].ContainerPort.Should().BeNull();
        }

        // =====================================================================
        // Multiple bindings on same service
        // =====================================================================

        [Fact]
        public async Task StartAsync_MultipleBindings_ProcessesAll()
        {
            var svc = CreateService("web", new DockerRunInfo("nginx", null), bindings: new List<ServiceBinding>
            {
                new() { Port = 5000, Protocol = "http" },
                new() { Port = 5001, Protocol = "https" },
                new() { Port = 6379, Protocol = "tcp" },
            });
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            svc.Description.Bindings[0].ContainerPort.Should().Be(80);
            svc.Description.Bindings[1].ContainerPort.Should().Be(443);
            svc.Description.Bindings[2].ContainerPort.Should().BeNull();
            // All bindings should have replica ports assigned
            svc.Description.Bindings.Should().OnlyContain(b => b.ReplicaPorts.Count > 0);
        }

        // =====================================================================
        // Multiple services
        // =====================================================================

        [Fact]
        public async Task StartAsync_MultipleServices_ProcessesAll()
        {
            var web = CreateService("web", new DockerRunInfo("nginx", null), bindings: new List<ServiceBinding>
            {
                new() { Port = 5000, Protocol = "http" },
            });
            var redis = CreateService("redis", new DockerRunInfo("redis", null), bindings: new List<ServiceBinding>
            {
                new() { Port = 6379, Protocol = "tcp" },
            });
            var app = CreateApp(new Dictionary<string, Service>
            {
                ["web"] = web,
                ["redis"] = redis,
            });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            web.Description.Bindings[0].ReplicaPorts.Should().NotBeEmpty();
            redis.Description.Bindings[0].ReplicaPorts.Should().NotBeEmpty();
        }

        // =====================================================================
        // No bindings — no-op
        // =====================================================================

        [Fact]
        public async Task StartAsync_NoBindings_NoError()
        {
            var svc = CreateService("web", new DockerRunInfo("nginx", null));
            var app = CreateApp(new Dictionary<string, Service> { ["web"] = svc });

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);

            svc.Description.Bindings.Should().BeEmpty();
        }

        // =====================================================================
        // Empty application
        // =====================================================================

        [Fact]
        public async Task StartAsync_EmptyApplication_NoError()
        {
            var app = CreateApp(new Dictionary<string, Service>());

            var assigner = new PortAssigner(_logger);
            await assigner.StartAsync(app);
            // No exception expected
        }

        // =====================================================================
        // StopAsync is a no-op
        // =====================================================================

        [Fact]
        public async Task StopAsync_CompletesSuccessfully()
        {
            var app = CreateApp(new Dictionary<string, Service>());

            var assigner = new PortAssigner(_logger);
            await assigner.StopAsync(app);
            // No exception expected
        }
    }
}
