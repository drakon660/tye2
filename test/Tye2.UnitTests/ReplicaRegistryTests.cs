using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tye2.Hosting;
using Xunit;

namespace Tye2.UnitTests
{
    public class ReplicaRegistryTests : IDisposable
    {
        private readonly ILogger _logger = NullLogger.Instance;
        private readonly string _tempDir;

        public ReplicaRegistryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tye2_registry_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        // =====================================================================
        // WriteReplicaEvent
        // =====================================================================

        [Fact]
        public void WriteReplicaEvent_CreatesFileAndReturnsTrue()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            var record = new Dictionary<string, string> { ["key"] = "value" };
            var result = registry.WriteReplicaEvent("test", record);

            result.Should().BeTrue();
            File.Exists(Path.Combine(_tempDir, ".tye", "test_store")).Should().BeTrue();
        }

        [Fact]
        public void WriteReplicaEvent_CreatesTyeFolder()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            Directory.Exists(Path.Combine(_tempDir, ".tye")).Should().BeFalse();

            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["k"] = "v" });

            Directory.Exists(Path.Combine(_tempDir, ".tye")).Should().BeTrue();
        }

        [Fact]
        public void WriteReplicaEvent_AppendsMultipleEvents()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["event"] = "1" });
            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["event"] = "2" });
            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["event"] = "3" });

            var lines = File.ReadAllLines(Path.Combine(_tempDir, ".tye", "test_store"));
            lines.Where(l => !string.IsNullOrWhiteSpace(l)).Should().HaveCount(3);
        }

        [Fact]
        public void WriteReplicaEvent_DifferentStores_CreateSeparateFiles()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            registry.WriteReplicaEvent("store-a", new Dictionary<string, string> { ["a"] = "1" });
            registry.WriteReplicaEvent("store-b", new Dictionary<string, string> { ["b"] = "2" });

            File.Exists(Path.Combine(_tempDir, ".tye", "store-a_store")).Should().BeTrue();
            File.Exists(Path.Combine(_tempDir, ".tye", "store-b_store")).Should().BeTrue();
        }

        [Fact]
        public void WriteReplicaEvent_SerializesAsJson()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            registry.WriteReplicaEvent("test", new Dictionary<string, string>
            {
                ["name"] = "web-abc123",
                ["state"] = "Started",
            });

            var content = File.ReadAllText(Path.Combine(_tempDir, ".tye", "test_store"));
            content.Should().Contain("\"name\":\"web-abc123\"");
            content.Should().Contain("\"state\":\"Started\"");
        }

        // =====================================================================
        // DeleteStore
        // =====================================================================

        [Fact]
        public void DeleteStore_ExistingStore_DeletesFileAndReturnsTrue()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["k"] = "v" });
            var filePath = Path.Combine(_tempDir, ".tye", "test_store");
            File.Exists(filePath).Should().BeTrue();

            var result = registry.DeleteStore("test");

            result.Should().BeTrue();
            File.Exists(filePath).Should().BeFalse();
        }

        [Fact]
        public void DeleteStore_NonExistentStore_ReturnsFalse()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            var result = registry.DeleteStore("nonexistent");

            result.Should().BeFalse();
        }

        [Fact]
        public void DeleteStore_DoesNotAffectOtherStores()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            registry.WriteReplicaEvent("keep", new Dictionary<string, string> { ["k"] = "v" });
            registry.WriteReplicaEvent("delete", new Dictionary<string, string> { ["k"] = "v" });

            registry.DeleteStore("delete");

            File.Exists(Path.Combine(_tempDir, ".tye", "keep_store")).Should().BeTrue();
            File.Exists(Path.Combine(_tempDir, ".tye", "delete_store")).Should().BeFalse();
        }

        // =====================================================================
        // GetEvents
        // =====================================================================

        [Fact]
        public async Task GetEvents_ExistingStore_ReturnsAllEvents()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["event"] = "1" });
            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["event"] = "2" });

            var events = await registry.GetEvents("test");

            events.Should().HaveCount(2);
            events[0]["event"].Should().Be("1");
            events[1]["event"].Should().Be("2");
        }

        [Fact]
        public async Task GetEvents_NonExistentStore_ReturnsEmpty()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            var events = await registry.GetEvents("nonexistent");

            events.Should().BeEmpty();
        }

        [Fact]
        public async Task GetEvents_PreservesAllFields()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            registry.WriteReplicaEvent("test", new Dictionary<string, string>
            {
                ["name"] = "web-abc",
                ["state"] = "Ready",
                ["pid"] = "12345",
            });

            var events = await registry.GetEvents("test");
            var evt = events.Should().ContainSingle().Subject;
            evt["name"].Should().Be("web-abc");
            evt["state"].Should().Be("Ready");
            evt["pid"].Should().Be("12345");
        }

        [Fact]
        public async Task GetEvents_AfterDeleteStore_ReturnsEmpty()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["k"] = "v" });
            registry.DeleteStore("test");

            var events = await registry.GetEvents("test");
            events.Should().BeEmpty();
        }

        // =====================================================================
        // Dispose
        // =====================================================================

        [Fact]
        public void Dispose_DeletesTyeFolder()
        {
            var registry = new ReplicaRegistry(_tempDir, _logger);
            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["k"] = "v" });

            var tyeFolder = Path.Combine(_tempDir, ".tye");
            Directory.Exists(tyeFolder).Should().BeTrue();

            registry.Dispose();

            Directory.Exists(tyeFolder).Should().BeFalse();
        }

        [Fact]
        public void Dispose_NoTyeFolder_NoError()
        {
            var registry = new ReplicaRegistry(_tempDir, _logger);
            // Never wrote anything, so .tye folder doesn't exist
            registry.Dispose();
            // No exception expected
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_NoError()
        {
            var registry = new ReplicaRegistry(_tempDir, _logger);
            registry.WriteReplicaEvent("test", new Dictionary<string, string> { ["k"] = "v" });

            registry.Dispose();
            registry.Dispose(); // Second call should not throw
        }

        // =====================================================================
        // Roundtrip: Write → Read → Delete → Read
        // =====================================================================

        [Fact]
        public async Task Roundtrip_WriteReadDeleteRead()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            // Write
            registry.WriteReplicaEvent("svc", new Dictionary<string, string> { ["state"] = "Starting" });
            registry.WriteReplicaEvent("svc", new Dictionary<string, string> { ["state"] = "Running" });

            // Read
            var events = await registry.GetEvents("svc");
            events.Should().HaveCount(2);

            // Delete
            registry.DeleteStore("svc").Should().BeTrue();

            // Read again
            var eventsAfterDelete = await registry.GetEvents("svc");
            eventsAfterDelete.Should().BeEmpty();
        }

        // =====================================================================
        // Concurrent writes (thread safety)
        // =====================================================================

        [Fact]
        public void WriteReplicaEvent_ConcurrentWrites_AllSucceed()
        {
            using var registry = new ReplicaRegistry(_tempDir, _logger);

            var tasks = Enumerable.Range(0, 20).Select(i =>
                Task.Run(() => registry.WriteReplicaEvent("concurrent",
                    new Dictionary<string, string> { ["index"] = i.ToString() }))
            ).ToArray();

            Task.WaitAll(tasks);

            var lines = File.ReadAllLines(Path.Combine(_tempDir, ".tye", "concurrent_store"));
            lines.Where(l => !string.IsNullOrWhiteSpace(l)).Should().HaveCount(20);
        }
    }
}
