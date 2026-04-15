using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tye2.Core;
using Tye2.Hosting;
using Tye2.Hosting.Model;
using Xunit;

namespace Tye2.UnitTests;

public class ProcessRunnerTests
{
    // --- ProcessRunnerOptions ---

    [Fact]
    public void Options_DefaultMaxRestarts_IsFive()
    {
        ProcessRunnerOptions.DefaultMaxRestarts.Should().Be(5);
    }

    [Fact]
    public void Options_AllServicesConstant()
    {
        ProcessRunnerOptions.AllServices.Should().Be("*");
    }

    [Fact]
    public void Options_Default_MaxRestartsIsFive()
    {
        var options = new ProcessRunnerOptions();
        options.MaxRestarts.Should().Be(5);
    }

    [Fact]
    public void Options_ShouldDebugService_FalseWhenNotDebugMode()
    {
        var options = new ProcessRunnerOptions { DebugMode = false };

        options.ShouldDebugService("myservice").Should().BeFalse();
    }

    [Fact]
    public void Options_ShouldDebugService_TrueForMatchingService()
    {
        var options = new ProcessRunnerOptions
        {
            DebugMode = true,
            ServicesToDebug = new[] { "backend", "frontend" }
        };

        options.ShouldDebugService("backend").Should().BeTrue();
        options.ShouldDebugService("frontend").Should().BeTrue();
    }

    [Fact]
    public void Options_ShouldDebugService_FalseForNonMatchingService()
    {
        var options = new ProcessRunnerOptions
        {
            DebugMode = true,
            ServicesToDebug = new[] { "backend" }
        };

        options.ShouldDebugService("worker").Should().BeFalse();
    }

    [Fact]
    public void Options_ShouldDebugService_CaseInsensitive()
    {
        var options = new ProcessRunnerOptions
        {
            DebugMode = true,
            ServicesToDebug = new[] { "Backend" }
        };

        options.ShouldDebugService("backend").Should().BeTrue();
        options.ShouldDebugService("BACKEND").Should().BeTrue();
    }

    [Fact]
    public void Options_ShouldDebugService_TrueForAllWhenDebugAll()
    {
        var options = new ProcessRunnerOptions
        {
            DebugMode = true,
            DebugAllServices = true,
            ServicesToDebug = new[] { "*" }
        };

        options.ShouldDebugService("anything").Should().BeTrue();
    }

    [Fact]
    public void Options_ShouldWatchService_FalseWhenNotWatchMode()
    {
        var options = new ProcessRunnerOptions { WatchMode = false };

        options.ShouldWatchService("myservice").Should().BeFalse();
    }

    [Fact]
    public void Options_ShouldWatchService_TrueForMatchingService()
    {
        var options = new ProcessRunnerOptions
        {
            WatchMode = true,
            ServicesToWatch = new[] { "frontend" }
        };

        options.ShouldWatchService("frontend").Should().BeTrue();
    }

    [Fact]
    public void Options_ShouldWatchService_FalseForNonMatchingService()
    {
        var options = new ProcessRunnerOptions
        {
            WatchMode = true,
            ServicesToWatch = new[] { "frontend" }
        };

        options.ShouldWatchService("backend").Should().BeFalse();
    }

    [Fact]
    public void Options_ShouldWatchService_TrueForAllWhenWatchAll()
    {
        var options = new ProcessRunnerOptions
        {
            WatchMode = true,
            WatchAllServices = true,
            ServicesToWatch = new[] { "*" }
        };

        options.ShouldWatchService("anything").Should().BeTrue();
    }

    [Fact]
    public void Options_FromHostOptions_DefaultValues()
    {
        var hostOptions = new HostOptions();

        var options = ProcessRunnerOptions.FromHostOptions(hostOptions);

        options.BuildProjects.Should().BeTrue(); // NoBuild is false by default
        options.DebugMode.Should().BeFalse();
        options.WatchMode.Should().BeFalse();
        options.ManualStartServices.Should().BeFalse();
        options.MaxRestarts.Should().Be(5);
    }

    [Fact]
    public void Options_FromHostOptions_NoBuild()
    {
        var hostOptions = new HostOptions { NoBuild = true };

        var options = ProcessRunnerOptions.FromHostOptions(hostOptions);

        options.BuildProjects.Should().BeFalse();
    }

    [Fact]
    public void Options_FromHostOptions_DebugSpecificServices()
    {
        var hostOptions = new HostOptions();
        hostOptions.Debug.Add("backend");
        hostOptions.Debug.Add("frontend");

        var options = ProcessRunnerOptions.FromHostOptions(hostOptions);

        options.DebugMode.Should().BeTrue();
        options.ServicesToDebug.Should().Contain("backend");
        options.ServicesToDebug.Should().Contain("frontend");
        options.DebugAllServices.Should().BeFalse();
    }

    [Fact]
    public void Options_FromHostOptions_DebugAll()
    {
        var hostOptions = new HostOptions();
        hostOptions.Debug.Add("*");

        var options = ProcessRunnerOptions.FromHostOptions(hostOptions);

        options.DebugMode.Should().BeTrue();
        options.DebugAllServices.Should().BeTrue();
    }

    [Fact]
    public void Options_FromHostOptions_WatchSpecificServices()
    {
        var hostOptions = new HostOptions();
        hostOptions.Watch.Add("frontend");

        var options = ProcessRunnerOptions.FromHostOptions(hostOptions);

        options.WatchMode.Should().BeTrue();
        options.ServicesToWatch.Should().Contain("frontend");
        options.WatchAllServices.Should().BeFalse();
    }

    [Fact]
    public void Options_FromHostOptions_WatchAll()
    {
        var hostOptions = new HostOptions();
        hostOptions.Watch.Add("*");

        var options = ProcessRunnerOptions.FromHostOptions(hostOptions);

        options.WatchMode.Should().BeTrue();
        options.WatchAllServices.Should().BeTrue();
    }

    [Fact]
    public void Options_FromHostOptions_ManualStart()
    {
        var hostOptions = new HostOptions();
        hostOptions.NoStart.Add("*");

        var options = ProcessRunnerOptions.FromHostOptions(hostOptions);

        options.ManualStartServices.Should().BeTrue();
    }

    [Fact]
    public void Options_FromHostOptions_ServicesNotToStart()
    {
        var hostOptions = new HostOptions();
        hostOptions.NoStart.Add("worker");

        var options = ProcessRunnerOptions.FromHostOptions(hostOptions);

        options.ServicesNotToStart.Should().Contain("worker");
        options.ManualStartServices.Should().BeFalse();
    }

    // --- ProcessRunner constructor ---

    [Fact]
    public void Constructor_CreatesInstance()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var logger = NullLogger.Instance;
            var registry = new ReplicaRegistry(tempDir, NullLogger.Instance);
            var options = new ProcessRunnerOptions();

            var runner = new ProcessRunner(logger, registry, options);

            runner.Should().NotBeNull();

            registry.Dispose();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // --- KillProcessAsync ---

    [Fact]
    public async Task KillProcessAsync_NoProcessInfo_CompletesGracefully()
    {
        var description = new ServiceDescription("test-service", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        // Should not throw even when no ProcessInfo exists
        await ProcessRunner.KillProcessAsync(service);
    }

    [Fact]
    public void WriteReplicaToStore_RecordsPidAndStartTime()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tye2-test-" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            using var registry = new ReplicaRegistry(tempDir, NullLogger.Instance);
            var runner = new ProcessRunner(NullLogger.Instance, registry, new ProcessRunnerOptions());
            var writeReplicaToStore = typeof(ProcessRunner).GetMethod("WriteReplicaToStore", BindingFlags.Instance | BindingFlags.NonPublic);

            writeReplicaToStore.Should().NotBeNull();

            writeReplicaToStore!.Invoke(runner, new object[] { Process.GetCurrentProcess().Id });

            var storePath = Path.Combine(tempDir, ".tye", "process_store");
            File.Exists(storePath).Should().BeTrue();

            var content = File.ReadAllText(storePath);
            content.Should().Contain("\"pid\":");
            content.Should().Contain("\"startTimeUtcTicks\":");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // --- Service.State ---

    [Fact]
    public void ServiceState_NoReplicas_Unknown()
    {
        var description = new ServiceDescription("test-service", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        service.State.Should().Be(ServiceState.Unknown);
    }

    [Fact]
    public void ServiceState_AllStarted_Started()
    {
        var description = new ServiceDescription("test-service", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        var replica = new ProcessStatus(service, "test_abc");
        replica.State = ReplicaState.Started;
        service.Replicas["test_abc"] = replica;

        service.State.Should().Be(ServiceState.Started);
    }

    [Fact]
    public void ServiceState_AllReady_Started()
    {
        var description = new ServiceDescription("test-service", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        var replica = new ProcessStatus(service, "test_abc");
        replica.State = ReplicaState.Ready;
        service.Replicas["test_abc"] = replica;

        service.State.Should().Be(ServiceState.Started);
    }

    [Fact]
    public void ServiceState_ReplicaAdded_Starting()
    {
        var description = new ServiceDescription("test-service", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        var replica = new ProcessStatus(service, "test_abc");
        replica.State = ReplicaState.Added;
        service.Replicas["test_abc"] = replica;

        service.State.Should().Be(ServiceState.Starting);
    }

    [Fact]
    public void ServiceState_SingleReplica_Stopped()
    {
        var description = new ServiceDescription("test-service", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        var replica = new ProcessStatus(service, "test_abc");
        replica.State = ReplicaState.Stopped;
        service.Replicas["test_abc"] = replica;

        service.State.Should().Be(ServiceState.Stopped);
    }

    [Fact]
    public void ServiceState_SingleReplica_Removed_Failed()
    {
        var description = new ServiceDescription("test-service", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        var replica = new ProcessStatus(service, "test_abc");
        replica.State = ReplicaState.Removed;
        service.Replicas["test_abc"] = replica;

        service.State.Should().Be(ServiceState.Failed);
    }

    [Fact]
    public void ServiceState_MultipleReplicas_OneStopped_Degraded()
    {
        var description = new ServiceDescription("test-service", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        var replica1 = new ProcessStatus(service, "test_001");
        replica1.State = ReplicaState.Started;
        service.Replicas["test_001"] = replica1;

        var replica2 = new ProcessStatus(service, "test_002");
        replica2.State = ReplicaState.Stopped;
        service.Replicas["test_002"] = replica2;

        service.State.Should().Be(ServiceState.Degraded);
    }

    [Fact]
    public void ServiceState_MultipleReplicas_AllStopped_Stopped()
    {
        var description = new ServiceDescription("test-service", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        var replica1 = new ProcessStatus(service, "test_001");
        replica1.State = ReplicaState.Stopped;
        service.Replicas["test_001"] = replica1;

        var replica2 = new ProcessStatus(service, "test_002");
        replica2.State = ReplicaState.Stopped;
        service.Replicas["test_002"] = replica2;

        service.State.Should().Be(ServiceState.Stopped);
    }

    // --- Service.ServiceType ---

    [Fact]
    public void ServiceType_ExecutableRunInfo_IsExecutable()
    {
        var description = new ServiceDescription("test", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        service.ServiceType.Should().Be(ServiceType.Executable);
    }

    [Fact]
    public void ServiceType_ProjectRunInfo_IsProject()
    {
        var project = new DotnetProjectServiceBuilder("test", new FileInfo("test.csproj"), ServiceSource.Configuration);
        var runInfo = new ProjectRunInfo(project);
        var description = new ServiceDescription("test", runInfo);
        var service = new Service(description, ServiceSource.Configuration);

        service.ServiceType.Should().Be(ServiceType.Project);
    }

    [Fact]
    public void ServiceType_DockerRunInfo_IsContainer()
    {
        var description = new ServiceDescription("test", new DockerRunInfo("nginx:latest", null));
        var service = new Service(description, ServiceSource.Configuration);

        service.ServiceType.Should().Be(ServiceType.Container);
    }

    [Fact]
    public void ServiceType_NoRunInfo_IsExternal()
    {
        var description = new ServiceDescription("test", null!);
        var service = new Service(description, ServiceSource.Configuration);

        service.ServiceType.Should().Be(ServiceType.External);
    }

    // --- Service.CachedLogs ---

    [Fact]
    public void Service_Logs_CachedInQueue()
    {
        var description = new ServiceDescription("test", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        service.Logs.OnNext("hello world");
        service.Logs.OnNext("second line");

        service.CachedLogs.Should().HaveCount(2);
        service.CachedLogs.Should().Contain("hello world");
    }

    [Fact]
    public void Service_Restarts_DefaultIsZero()
    {
        var description = new ServiceDescription("test", new ExecutableRunInfo("echo", null, null));
        var service = new Service(description, ServiceSource.Configuration);

        service.Restarts.Should().Be(0);
    }
}
