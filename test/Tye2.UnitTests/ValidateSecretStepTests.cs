using System;
using System.Collections.Generic;
using System.CommandLine.IO;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AwesomeAssertions;
using k8s;
using k8s.Models;
using Microsoft.Rest;
using Tye2.Core;
using Xunit;

namespace Tye2.UnitTests
{
    public class ValidateSecretStepTests
    {
        private static readonly FileInfo DummySource = new("C:\\test\\tye.yaml");

        private static (ApplicationBuilder app, ContainerServiceBuilder service) CreateAppAndService(string serviceName = "web")
        {
            var app = new ApplicationBuilder(DummySource, "test-app", new ContainerEngine(null), null)
            {
                Namespace = "test-ns",
            };

            var service = new ContainerServiceBuilder(serviceName, "nginx", ServiceSource.Configuration);
            service.Bindings.Add(new BindingBuilder { Name = "http", Port = 80, Protocol = "http" });
            app.Services.Add(service);

            return (app, service);
        }

        private static OutputContext CreateOutput()
        {
            return new OutputContext(new TestConsole(), Verbosity.Debug);
        }

        private static void AddSecretOutput(ServiceBuilder service, params SecretInputBinding[] bindings)
        {
            var computed = new ComputedBindings();
            foreach (var binding in bindings)
            {
                computed.Bindings.Add(binding);
            }

            service.Outputs.Add(computed);
        }

        private static SecretConnectionStringInputBinding CreateConnectionStringSecret(ServiceBuilder service, string name)
        {
            return new SecretConnectionStringInputBinding(name, service, service.Bindings[0], "CONNECTIONSTRINGS__DB");
        }

        private static SecretUrlInputBinding CreateUrlSecret(ServiceBuilder service, string name)
        {
            return new SecretUrlInputBinding(name, service, service.Bindings[0], "SERVICE__DB");
        }

        private static HttpOperationException CreateNotFoundException()
        {
            var response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.NotFound), string.Empty);
            return new HttpOperationException("not found")
            {
                Response = response,
            };
        }

        [Fact]
        public async Task ExecuteAsync_NoComputedBindings_DoesNothing()
        {
            var (app, service) = CreateAppAndService();
            var step = new TestableValidateSecretStep();

            await step.ExecuteAsync(CreateOutput(), app, service);

            step.ReadCalls.Should().Be(0);
            step.CreateCalls.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_SecretExists_DoesNotCreateSecret()
        {
            var (app, service) = CreateAppAndService();
            AddSecretOutput(service, CreateConnectionStringSecret(service, "my-secret"));

            var step = new TestableValidateSecretStep
            {
                Interactive = false,
            };

            await step.ExecuteAsync(CreateOutput(), app, service);

            step.ReadCalls.Should().Be(1);
            step.CreateCalls.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_MissingConnectionStringSecret_NonInteractive_Throws()
        {
            var (app, service) = CreateAppAndService();
            AddSecretOutput(service, CreateConnectionStringSecret(service, "missing-secret"));

            var step = new TestableValidateSecretStep
            {
                Interactive = false,
                Force = false,
                OnReadSecretAsync = (_, _) => Task.FromException(CreateNotFoundException()),
            };

            Func<Task> act = () => step.ExecuteAsync(CreateOutput(), app, service);

            var ex = (await act.Should().ThrowAsync<CommandException>()).Which;
            ex.Message.Should().Contain("missing-secret");
            ex.Message.Should().Contain("--interactive");
            step.CreateCalls.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_MissingSecret_WithForce_SkipsCreation()
        {
            var (app, service) = CreateAppAndService();
            AddSecretOutput(service, CreateUrlSecret(service, "missing-url-secret"));

            var step = new TestableValidateSecretStep
            {
                Interactive = false,
                Force = true,
                OnReadSecretAsync = (_, _) => Task.FromException(CreateNotFoundException()),
            };

            await step.ExecuteAsync(CreateOutput(), app, service);

            step.ReadCalls.Should().Be(1);
            step.CreateCalls.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_DuplicateSecretNames_ValidatesOnlyOnce()
        {
            var (app, service) = CreateAppAndService();
            var secretName = "shared-secret";
            AddSecretOutput(
                service,
                CreateConnectionStringSecret(service, secretName),
                CreateUrlSecret(service, secretName));

            var step = new TestableValidateSecretStep();

            await step.ExecuteAsync(CreateOutput(), app, service);

            step.ReadCalls.Should().Be(1);
            step.ValidatedSecretNames.Should().ContainSingle().Which.Should().Be(secretName);
        }

        private sealed class TestableValidateSecretStep : ValidateSecretStep
        {
            public int ReadCalls { get; private set; }
            public int CreateCalls { get; private set; }
            public List<string> ValidatedSecretNames { get; } = new();
            public List<V1Secret> CreatedSecrets { get; } = new();

            public Func<string, string, Task> OnReadSecretAsync { get; set; } = (_, _) => Task.CompletedTask;
            public Func<V1Secret, string, Task> OnCreateSecretAsync { get; set; } = (_, _) => Task.CompletedTask;

            protected override KubernetesClientConfiguration BuildKubernetesClientConfiguration()
            {
                return new KubernetesClientConfiguration
                {
                    Host = "http://localhost",
                    Namespace = "default",
                };
            }

            protected override Kubernetes CreateKubernetesClient(KubernetesClientConfiguration config)
            {
                return new Kubernetes(config);
            }

            protected override async Task ReadSecretAsync(Kubernetes kubernetes, string secretName, string @namespace)
            {
                ReadCalls++;
                ValidatedSecretNames.Add(secretName);
                await OnReadSecretAsync(secretName, @namespace);
            }

            protected override async Task CreateSecretAsync(Kubernetes kubernetes, V1Secret secret, string @namespace)
            {
                CreateCalls++;
                CreatedSecrets.Add(secret);
                await OnCreateSecretAsync(secret, @namespace);
            }
        }
    }
}
