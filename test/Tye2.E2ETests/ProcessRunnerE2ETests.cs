// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Hosting;
using Tye2.Hosting.Model;
using Tye2.Hosting.Model.V1;
using Tye2.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using static Tye2.Test.Infrastructure.TestHelpers;

namespace Tye2.E2ETests
{
    public class ProcessRunnerE2ETests
    {
        private readonly ITestOutputHelper _output;
        private readonly TestOutputLogEventSink _sink;
        private readonly JsonSerializerOptions _options;

        private static readonly ReplicaState?[] RunningStates = new ReplicaState?[]
        {
            ReplicaState.Started, ReplicaState.Healthy, ReplicaState.Ready
        };

        public ProcessRunnerE2ETests(ITestOutputHelper output)
        {
            _output = output;
            _sink = new TestOutputLogEventSink(output);

            _options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };

            _options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        [Fact]
        public async Task SingleProject_ProcessStartsAndHasValidPid()
        {
            using var projectDirectory = CopyTestProjectDirectory("single-project");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                var service = host.Application.Services["test-project"];
                service.Replicas.Should().HaveCount(1);

                var replica = service.Replicas.Values.First();
                Assert.Contains(replica.State, RunningStates);

                // Process-based replicas should have a valid PID
                var processStatus = replica as ProcessStatus;
                processStatus.Should().NotBeNull();
                processStatus.Pid.Should().BeGreaterThan(0);
            });
        }

        [Fact]
        public async Task SingleProject_ServiceIsReachableViaHttp()
        {
            using var projectDirectory = CopyTestProjectDirectory("single-project");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
                AllowAutoRedirect = false
            };
            var client = new HttpClient(new RetryHandler(handler));

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                // Get service URL from dashboard API
                var serviceResult = await client.GetStringAsync($"{uri}api/v1/services/test-project");
                var service = JsonSerializer.Deserialize<V1Service>(serviceResult, _options);
                var binding = service.Description.Bindings.Where(b => b.Protocol == "http").Single();
                var serviceUrl = $"http://localhost:{binding.Port}";

                var response = await client.GetAsync(serviceUrl);
                response.IsSuccessStatusCode.Should().BeTrue();
            });
        }

        [Fact]
        public async Task MultipleReplicas_AllStartSuccessfully()
        {
            using var projectDirectory = CopyTestProjectDirectory("health-checks");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye-none.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                var service = host.Application.Services["health-none"];
                service.Replicas.Should().HaveCount(3);

                foreach (var replica in service.Replicas.Values)
                {
                    Assert.Contains(replica.State, RunningStates);

                    var processStatus = replica as ProcessStatus;
                    processStatus.Should().NotBeNull();
                    processStatus.Pid.Should().BeGreaterThan(0);
                }

                // All replicas should have unique PIDs
                var pids = service.Replicas.Values
                    .OfType<ProcessStatus>()
                    .Select(p => p.Pid)
                    .ToList();
                pids.Distinct().Should().HaveCount(3, "each replica should have a unique PID");
            });
        }

        [Fact]
        public async Task ReplicaStop_AutomaticallyRestarts()
        {
            using var projectDirectory = CopyTestProjectDirectory("health-checks");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye-none.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                var service = host.Application.Services["health-none"];
                var replicaToStop = service.Replicas.First();

                Assert.Contains(replicaToStop.Value.State, RunningStates);

                var replicasToRestart = new[] { replicaToStop.Key }.ToHashSet();
                var restOfReplicas = host.Application.Services
                    .SelectMany(s => s.Value.Replicas)
                    .Select(r => r.Value.Name)
                    .Where(r => r != replicaToStop.Key)
                    .ToHashSet();

                var restarted = await DoOperationAndWaitForReplicasToRestart(
                    host,
                    replicasToRestart,
                    restOfReplicas,
                    TimeSpan.FromSeconds(30),
                    _ =>
                    {
                        replicaToStop.Value.StoppingTokenSource!.Cancel();
                        return Task.CompletedTask;
                    });

                restarted.Should().BeTrue("the stopped replica should restart automatically");

                // After restart, all replicas should be running again
                Assert.True(
                    host.Application.Services
                        .SelectMany(s => s.Value.Replicas)
                        .All(r => RunningStates.Contains(r.Value.State)),
                    "all replicas should be running after restart");
            });
        }

        [Fact]
        public async Task ServiceWithArgs_PassesArgumentsToProcess()
        {
            using var projectDirectory = CopyTestProjectDirectory("single-project-with-args");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            var client = new HttpClient(new RetryHandler(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
                AllowAutoRedirect = false
            }));

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                var service = host.Application.Services["test-project-with-args"];
                service.Replicas.Should().HaveCount(1);
                Assert.Contains(service.Replicas.Values.First().State, RunningStates);

                // Verify args were passed by checking logs through dashboard API
                var logs = await client.GetStringAsync($"{uri}api/v1/logs/test-project-with-args");
                logs.Should().Contain("Argument1", "first argument should appear in logs");
                logs.Should().Contain("Argument2", "second argument should appear in logs");
            });
        }

        [Fact]
        public async Task ServiceWithEnvVars_SetsEnvironmentVariables()
        {
            using var projectDirectory = CopyTestProjectDirectory("dotnet-env-vars");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                var service = host.Application.Services["test-project"];
                service.Replicas.Should().HaveCount(1);
                Assert.Contains(service.Replicas.Values.First().State, RunningStates);

                // Check that environment bindings were set on the replica
                var replica = service.Replicas.Values.First();
                replica.Environment.Should().NotBeEmpty();
                replica.Environment.Should().ContainKey("DOTNET_ENVIRONMENT");
                replica.Environment["DOTNET_ENVIRONMENT"].Should().Be("dev");
                replica.Environment.Should().ContainKey("ASPNETCORE_ENVIRONMENT");
                replica.Environment["ASPNETCORE_ENVIRONMENT"].Should().Be("dev");
            });
        }

        [Fact]
        public async Task DashboardApi_ReportsProcessReplicaState()
        {
            using var projectDirectory = CopyTestProjectDirectory("single-project");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            var client = new HttpClient(new RetryHandler(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
                AllowAutoRedirect = false
            }));

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                var response = await client.GetStringAsync($"{uri}api/v1/services/test-project");
                var service = JsonSerializer.Deserialize<V1Service>(response, _options);

                service.Should().NotBeNull();
                service.Description.Name.Should().Be("test-project");
                service.Replicas.Should().HaveCount(1);

                var replica = service.Replicas.First().Value;
                replica.Pid.Should().BeGreaterThan(0, "dashboard API should report the process PID");
            });
        }

        [Fact]
        public async Task NoBuild_SkipsBuildStep()
        {
            using var projectDirectory = CopyTestProjectDirectory("single-project");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            // First do a regular build so binaries exist
            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                Assert.Contains(
                    host.Application.Services["test-project"].Replicas.Values.First().State,
                    RunningStates);
            });

            // Now run again with NoBuild - should work since binaries already exist
            application = await ApplicationFactory.CreateAsync(outputContext, projectFile);
            await RunHostingApplication(application, new HostOptions { NoBuild = true }, async (host, uri) =>
            {
                var service = host.Application.Services["test-project"];
                service.Replicas.Should().HaveCount(1);
                Assert.Contains(service.Replicas.Values.First().State, RunningStates);
            });
        }

        [Fact]
        public async Task FrontendBackend_BothServicesStartWithProcessRunner()
        {
            using var projectDirectory = CopyTestProjectDirectory("frontend-backend");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                host.Application.Services.Should().HaveCount(2);

                var frontend = host.Application.Services["frontend"];
                var backend = host.Application.Services["backend"];

                frontend.Replicas.Should().HaveCount(1);
                backend.Replicas.Should().HaveCount(1);

                Assert.Contains(frontend.Replicas.Values.First().State, RunningStates);
                Assert.Contains(backend.Replicas.Values.First().State, RunningStates);

                // Both should be running as processes with valid PIDs
                var frontendPid = (frontend.Replicas.Values.First() as ProcessStatus)?.Pid;
                var backendPid = (backend.Replicas.Values.First() as ProcessStatus)?.Pid;

                frontendPid.Should().BeGreaterThan(0);
                backendPid.Should().BeGreaterThan(0);
                frontendPid.Should().NotBe(backendPid, "each service should have its own process");
            });
        }

        [Fact]
        public async Task ServiceLogs_CapturedByProcessRunner()
        {
            using var projectDirectory = CopyTestProjectDirectory("single-project");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            var client = new HttpClient(new RetryHandler(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
                AllowAutoRedirect = false
            }));

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                // Give the service a moment to produce some log output
                await Task.Delay(TimeSpan.FromSeconds(2));

                var logs = await client.GetStringAsync($"{uri}api/v1/logs/test-project");
                logs.Should().NotBeNullOrEmpty("process runner should capture service stdout/stderr");
            });
        }

        [Fact]
        public async Task ServiceBindings_HaveAssignedPorts()
        {
            using var projectDirectory = CopyTestProjectDirectory("single-project");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                var service = host.Application.Services["test-project"];
                var replica = service.Replicas.Values.First();

                // Replica should have port bindings assigned
                replica.Ports.Should().NotBeEmpty("process runner should assign ports to replicas");

                // Bindings should have valid port numbers
                foreach (var port in replica.Ports)
                {
                    port.Should().BeGreaterThan(0).And.BeLessThan(65536);
                }
            });
        }

        [Fact]
        public async Task KillProcessAsync_StopsServiceProcess()
        {
            using var projectDirectory = CopyTestProjectDirectory("single-project");

            var projectFile = new FileInfo(Path.Combine(projectDirectory.DirectoryPath, "tye.yaml"));
            var outputContext = new OutputContext(_sink, Verbosity.Debug);
            var application = await ApplicationFactory.CreateAsync(outputContext, projectFile);

            await RunHostingApplication(application, new HostOptions(), async (host, uri) =>
            {
                var service = host.Application.Services["test-project"];
                Assert.Contains(service.Replicas.Values.First().State, RunningStates);

                var pidBefore = (service.Replicas.Values.First() as ProcessStatus)?.Pid;
                pidBefore.Should().BeGreaterThan(0);

                // Kill the process via the static method
                await ProcessRunner.KillProcessAsync(service);

                // After kill, replicas should be removed (process stopped and cleaned up)
                service.Replicas.Should().BeEmpty("killed service should have no running replicas");
            });
        }


        private async Task RunHostingApplication(ApplicationBuilder application, HostOptions options, Func<TyeHost, Uri, Task> execute)
        {
            await using var host = new TyeHost(application.ToHostingApplication(), options)
            {
                Sink = _sink,
            };

            try
            {
                await StartHostAndWaitForReplicasToStart(host);

                var uri = new Uri(host.Addresses!.First());

                await execute(host, uri!);
            }
            finally
            {
                if (host.DashboardWebApplication != null)
                {
                    var uri = new Uri(host.Addresses!.First());

                    using var client = new HttpClient();

                    foreach (var s in host.Application.Services.Values)
                    {
                        var logs = await client.GetStringAsync(new Uri(uri, $"/api/v1/logs/{s.Description.Name}"));

                        _output.WriteLine($"Logs for service: {s.Description.Name}");
                        _output.WriteLine(logs);

                        var description = await client.GetStringAsync(new Uri(uri, $"/api/v1/services/{s.Description.Name}"));

                        _output.WriteLine($"Service definition: {s.Description.Name}");
                        _output.WriteLine(description);
                    }
                }

                await DockerAssert.CleanupManagedResourcesAsync(_output, application.Source.DirectoryName);
            }
        }
    }
}
