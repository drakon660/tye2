using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Tye2.Core;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace Tye2.UnitTests
{
    public class KubernetesManifestGeneratorTests
    {
        private readonly OutputContext _output;
        private readonly FileInfo _dummySource;

        public KubernetesManifestGeneratorTests()
        {
            _output = new OutputContext(new TestConsole(), Verbosity.Debug);
            _dummySource = new FileInfo(Path.Combine(Path.GetTempPath(), "tye.yaml"));
        }

        private ApplicationBuilder CreateApp(string name = "test-app", string? ns = null, ContainerRegistry? registry = null)
        {
            var app = new ApplicationBuilder(_dummySource, name, new ContainerEngine(null), null)
            {
                Namespace = ns,
                Registry = registry,
            };
            return app;
        }

        private ProjectServiceBuilder CreateProjectService(string name, int replicas = 1)
        {
            return new ProjectServiceBuilder(name, ServiceSource.Configuration)
            {
                Replicas = replicas,
            };
        }

        private DeploymentManifestInfo CreateDeployment(string appName = "my-svc",
            Dictionary<string, string>? labels = null,
            Dictionary<string, string>? annotations = null)
        {
            var deployment = new DeploymentManifestInfo();
            deployment.Labels["app.kubernetes.io/name"] = appName;
            deployment.Labels["app.kubernetes.io/part-of"] = "test-app";
            if (labels != null)
            {
                foreach (var kvp in labels)
                    deployment.Labels[kvp.Key] = kvp.Value;
            }
            if (annotations != null)
            {
                foreach (var kvp in annotations)
                    deployment.Annotations[kvp.Key] = kvp.Value;
            }
            return deployment;
        }

        private ServiceManifestInfo CreateServiceManifest(
            Dictionary<string, string>? labels = null,
            Dictionary<string, string>? annotations = null)
        {
            var svc = new ServiceManifestInfo();
            svc.Labels["app.kubernetes.io/name"] = "my-svc";
            if (labels != null)
            {
                foreach (var kvp in labels)
                    svc.Labels[kvp.Key] = kvp.Value;
            }
            if (annotations != null)
            {
                foreach (var kvp in annotations)
                    svc.Annotations[kvp.Key] = kvp.Value;
            }
            return svc;
        }

        private static YamlMappingNode GetRootMapping(YamlDocument doc) => (YamlMappingNode)doc.RootNode;

        private static string GetScalar(YamlMappingNode node, string key) =>
            ((YamlScalarNode)node.Children[new YamlScalarNode(key)]).Value!;

        private static YamlMappingNode GetMapping(YamlMappingNode node, string key) =>
            (YamlMappingNode)node.Children[new YamlScalarNode(key)];

        private static YamlSequenceNode GetSequence(YamlMappingNode node, string key) =>
            (YamlSequenceNode)node.Children[new YamlScalarNode(key)];

        private static bool HasKey(YamlMappingNode node, string key) =>
            node.Children.ContainsKey(new YamlScalarNode(key));

        // =====================================================================
        // CreateService
        // =====================================================================

        [Fact]
        public void CreateService_SetsKindAndApiVersion()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var root = GetRootMapping(result.Yaml);
            GetScalar(root, "kind").Should().Be("Service");
            GetScalar(root, "apiVersion").Should().Be("v1");
        }

        [Fact]
        public void CreateService_SetsMetadataName()
        {
            var app = CreateApp();
            var project = CreateProjectService("my-api");
            var deployment = CreateDeployment("my-api");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            GetScalar(metadata, "name").Should().Be("my-api");
        }

        [Fact]
        public void CreateService_OutputNameMatchesProjectName()
        {
            var app = CreateApp();
            var project = CreateProjectService("backend");
            var deployment = CreateDeployment("backend");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            result.Name.Should().Be("backend");
        }

        [Fact]
        public void CreateService_WithNamespace_SetsNamespace()
        {
            var app = CreateApp(ns: "production");
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            GetScalar(metadata, "namespace").Should().Be("production");
        }

        [Fact]
        public void CreateService_WithoutNamespace_OmitsNamespace()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            HasKey(metadata, "namespace").Should().BeFalse();
        }

        [Fact]
        public void CreateService_SetsLabels()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest(labels: new Dictionary<string, string>
            {
                ["custom-label"] = "custom-value",
            });

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            var labels = GetMapping(metadata, "labels");
            GetScalar(labels, "custom-label").Should().Be("custom-value");
        }

        [Fact]
        public void CreateService_WithAnnotations_SetsAnnotations()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest(annotations: new Dictionary<string, string>
            {
                ["prometheus.io/scrape"] = "true",
            });

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            var annotations = GetMapping(metadata, "annotations");
            GetScalar(annotations, "prometheus.io/scrape").Should().Be("true");
        }

        [Fact]
        public void CreateService_WithoutAnnotations_OmitsAnnotations()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            HasKey(metadata, "annotations").Should().BeFalse();
        }

        [Fact]
        public void CreateService_SetsSelectorFromDeploymentLabel()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            var selector = GetMapping(spec, "selector");
            GetScalar(selector, "app.kubernetes.io/name").Should().Be("web");
        }

        [Fact]
        public void CreateService_SetsTypeClusterIP()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            GetScalar(spec, "type").Should().Be("ClusterIP");
        }

        [Fact]
        public void CreateService_MissingNameLabel_ThrowsInvalidOperationException()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = new DeploymentManifestInfo(); // no labels
            var svc = CreateServiceManifest();

            var act = () => KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*app.kubernetes.io/name*");
        }

        [Fact]
        public void CreateService_WithHttpBinding_CreatesPort()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            var ports = GetSequence(spec, "ports");
            ports.Children.Should().ContainSingle();
            var port = (YamlMappingNode)ports[0];
            GetScalar(port, "name").Should().Be("http");
            GetScalar(port, "protocol").Should().Be("TCP");
            GetScalar(port, "port").Should().Be("80");
            GetScalar(port, "targetPort").Should().Be("80");
        }

        [Fact]
        public void CreateService_WithContainerPort_UsesContainerPortAsTarget()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Bindings.Add(new BindingBuilder { Port = 8080, ContainerPort = 80, Protocol = "http" });
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            var port = (YamlMappingNode)GetSequence(spec, "ports")[0];
            GetScalar(port, "port").Should().Be("8080");
            GetScalar(port, "targetPort").Should().Be("80");
        }

        [Fact]
        public void CreateService_WithNamedBinding_UsesNameAsPortName()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Bindings.Add(new BindingBuilder { Name = "grpc", Port = 5000, Protocol = "http" });
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var port = (YamlMappingNode)GetSequence(GetMapping(GetRootMapping(result.Yaml), "spec"), "ports")[0];
            GetScalar(port, "name").Should().Be("grpc");
        }

        [Fact]
        public void CreateService_HttpsBinding_IsSkipped()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Bindings.Add(new BindingBuilder { Port = 443, Protocol = "https" });
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            var ports = GetSequence(spec, "ports");
            ports.Children.Should().BeEmpty();
        }

        [Fact]
        public void CreateService_NullPortBinding_IsSkipped()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Bindings.Add(new BindingBuilder { Protocol = "http" }); // no port
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            var ports = GetSequence(spec, "ports");
            ports.Children.Should().BeEmpty();
        }

        [Fact]
        public void CreateService_MultipleBindings_CreatesMultiplePorts()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Bindings.Add(new BindingBuilder { Name = "http", Port = 80, Protocol = "http" });
            project.Bindings.Add(new BindingBuilder { Name = "grpc", Port = 5000, Protocol = "http" });
            var deployment = CreateDeployment("web");
            var svc = CreateServiceManifest();

            var result = KubernetesManifestGenerator.CreateService(_output, app, project, deployment, svc);

            var ports = GetSequence(GetMapping(GetRootMapping(result.Yaml), "spec"), "ports");
            ports.Children.Should().HaveCount(2);
        }

        // =====================================================================
        // CreateDeployment
        // =====================================================================

        [Fact]
        public void CreateDeployment_SetsKindAndApiVersion()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var root = GetRootMapping(result.Yaml);
            GetScalar(root, "kind").Should().Be("Deployment");
            GetScalar(root, "apiVersion").Should().Be("apps/v1");
        }

        [Fact]
        public void CreateDeployment_SetsMetadataName()
        {
            var app = CreateApp();
            var project = CreateProjectService("my-api");
            var deployment = CreateDeployment("my-api");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            GetScalar(metadata, "name").Should().Be("my-api");
        }

        [Fact]
        public void CreateDeployment_OutputNameMatchesProjectName()
        {
            var app = CreateApp();
            var project = CreateProjectService("backend");
            var deployment = CreateDeployment("backend");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            result.Name.Should().Be("backend");
        }

        [Fact]
        public void CreateDeployment_WithNamespace_SetsNamespace()
        {
            var app = CreateApp(ns: "staging");
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            GetScalar(metadata, "namespace").Should().Be("staging");
        }

        [Fact]
        public void CreateDeployment_WithoutNamespace_OmitsNamespace()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            HasKey(metadata, "namespace").Should().BeFalse();
        }

        [Fact]
        public void CreateDeployment_SetsReplicas()
        {
            var app = CreateApp();
            var project = CreateProjectService("web", replicas: 3);
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            GetScalar(spec, "replicas").Should().Be("3");
        }

        [Fact]
        public void CreateDeployment_SetsMatchLabels()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            var matchLabels = GetMapping(GetMapping(spec, "selector"), "matchLabels");
            GetScalar(matchLabels, "app.kubernetes.io/name").Should().Be("web");
        }

        [Fact]
        public void CreateDeployment_MissingNameLabel_ThrowsInvalidOperationException()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = new DeploymentManifestInfo(); // no labels

            var act = () => KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*app.kubernetes.io/name*");
        }

        [Fact]
        public void CreateDeployment_WithAnnotations_SetsAnnotations()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web", annotations: new Dictionary<string, string>
            {
                ["sidecar.istio.io/inject"] = "true",
            });

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            var annotations = GetMapping(metadata, "annotations");
            GetScalar(annotations, "sidecar.istio.io/inject").Should().Be("true");
        }

        [Fact]
        public void CreateDeployment_WithAnnotations_AlsoSetsOnTemplateMetadata()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web", annotations: new Dictionary<string, string>
            {
                ["my-annotation"] = "value",
            });

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var template = GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template");
            var templateMetadata = GetMapping(template, "metadata");
            var templateAnnotations = GetMapping(templateMetadata, "annotations");
            GetScalar(templateAnnotations, "my-annotation").Should().Be("value");
        }

        [Fact]
        public void CreateDeployment_Labels_AppliedToTemplateMetadata()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web", labels: new Dictionary<string, string>
            {
                ["version"] = "v1",
            });

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var template = GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template");
            var templateLabels = GetMapping(GetMapping(template, "metadata"), "labels");
            GetScalar(templateLabels, "version").Should().Be("v1");
        }

        [Fact]
        public void CreateDeployment_WithDockerImage_CreatesContainer()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("myregistry/web", "latest"));
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var template = GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template");
            var containers = GetSequence(GetMapping(template, "spec"), "containers");
            containers.Children.Should().ContainSingle();
            var container = (YamlMappingNode)containers[0];
            GetScalar(container, "name").Should().Be("web");
            GetScalar(container, "image").Should().Be("myregistry/web:latest");
            GetScalar(container, "imagePullPolicy").Should().Be("Always");
        }

        [Fact]
        public void CreateDeployment_WithoutDockerImage_ContainersEmpty()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var template = GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template");
            var containers = GetSequence(GetMapping(template, "spec"), "containers");
            containers.Children.Should().BeEmpty();
        }

        [Fact]
        public void CreateDeployment_WithBindings_CreatesContainerPorts()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            var ports = GetSequence(container, "ports");
            ports.Children.Should().ContainSingle();
            GetScalar((YamlMappingNode)ports[0], "containerPort").Should().Be("80");
        }

        [Fact]
        public void CreateDeployment_HttpsBinding_IsSkipped()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Bindings.Add(new BindingBuilder { Port = 443, Protocol = "https" });
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            // ports sequence is created but https entries are skipped, so it's empty
            var ports = GetSequence(container, "ports");
            ports.Children.Should().BeEmpty();
        }

        [Fact]
        public void CreateDeployment_WithContainerPort_UsesContainerPortValue()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Bindings.Add(new BindingBuilder { Port = 8080, ContainerPort = 80, Protocol = "http" });
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            GetScalar((YamlMappingNode)GetSequence(container, "ports")[0], "containerPort").Should().Be("80");
        }

        [Fact]
        public void CreateDeployment_WithEnvironmentVariables_SetsEnv()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.EnvironmentVariables.Add(new EnvironmentVariableBuilder("MY_VAR") { Value = "my-value" });
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            var env = GetSequence(container, "env");
            env.Children.Should().ContainSingle();
            var envVar = (YamlMappingNode)env[0];
            GetScalar(envVar, "name").Should().Be("MY_VAR");
            GetScalar(envVar, "value").Should().Be("my-value");
        }

        [Fact]
        public void CreateDeployment_WithComputedBindings_EnvironmentVariableInputBinding()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var computed = new ComputedBindings();
            computed.Bindings.Add(new EnvironmentVariableInputBinding("SERVICE__URL", "http://backend:80"));
            project.Outputs.Add(computed);
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            var env = GetSequence(container, "env");
            var binding = env.Children.Cast<YamlMappingNode>()
                .First(e => GetScalar(e, "name") == "SERVICE__URL");
            GetScalar(binding, "value").Should().Be("http://backend:80");
        }

        [Fact]
        public void CreateDeployment_WithComputedBindings_SecretConnectionStringInputBinding()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var computed = new ComputedBindings();
            var dummyService = new ExternalServiceBuilder("db", ServiceSource.Configuration);
            var dummyBinding = new BindingBuilder { ConnectionString = "conn" };
            computed.Bindings.Add(new SecretConnectionStringInputBinding("DB_CONN_KEY", dummyService, dummyBinding, "db-secret"));
            project.Outputs.Add(computed);
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            var env = GetSequence(container, "env");
            var secretEntry = env.Children.Cast<YamlMappingNode>()
                .First(e => GetScalar(e, "name") == "db-secret");
            var valueFrom = GetMapping(secretEntry, "valueFrom");
            var secretKeyRef = GetMapping(valueFrom, "secretKeyRef");
            GetScalar(secretKeyRef, "name").Should().Be("DB_CONN_KEY");
            GetScalar(secretKeyRef, "key").Should().Be("connectionstring");
        }

        [Fact]
        public void CreateDeployment_WithComputedBindings_SecretUrlInputBinding()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var computed = new ComputedBindings();
            var dummyService = new ExternalServiceBuilder("api", ServiceSource.Configuration);
            var dummyBinding = new BindingBuilder();
            computed.Bindings.Add(new SecretUrlInputBinding("api-secret", dummyService, dummyBinding, "SERVICE__API"));
            project.Outputs.Add(computed);
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            var env = GetSequence(container, "env");
            var entries = env.Children.Cast<YamlMappingNode>().ToList();

            // SecretUrlInputBinding generates 3 env vars: PROTOCOL, HOST, PORT
            var protocolEntry = entries.First(e => GetScalar(e, "name") == "SERVICE__API__PROTOCOL");
            GetScalar(GetMapping(GetMapping(protocolEntry, "valueFrom"), "secretKeyRef"), "key").Should().Be("protocol");

            var hostEntry = entries.First(e => GetScalar(e, "name") == "SERVICE__API__HOST");
            GetScalar(GetMapping(GetMapping(hostEntry, "valueFrom"), "secretKeyRef"), "key").Should().Be("host");

            var portEntry = entries.First(e => GetScalar(e, "name") == "SERVICE__API__PORT");
            GetScalar(GetMapping(GetMapping(portEntry, "valueFrom"), "secretKeyRef"), "key").Should().Be("port");
        }

        [Fact]
        public void CreateDeployment_WithLivenessProbe()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            project.Liveness = new ProbeBuilder
            {
                Http = new HttpProberBuilder { Path = "/healthz", Port = 80, Protocol = "http" },
                InitialDelay = 10,
                Period = 30,
                SuccessThreshold = 1,
                FailureThreshold = 3,
            };
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            HasKey(container, "livenessProbe").Should().BeTrue();
            var probe = GetMapping(container, "livenessProbe");
            var httpGet = GetMapping(probe, "httpGet");
            GetScalar(httpGet, "path").Should().Be("/healthz");
            GetScalar(httpGet, "port").Should().Be("80");
            GetScalar(httpGet, "scheme").Should().Be("HTTP");
            GetScalar(probe, "initialDelaySeconds").Should().Be("10");
            GetScalar(probe, "periodSeconds").Should().Be("30");
            GetScalar(probe, "successThreshold").Should().Be("1");
            GetScalar(probe, "failureThreshold").Should().Be("3");
        }

        [Fact]
        public void CreateDeployment_WithReadinessProbe()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            project.Readiness = new ProbeBuilder
            {
                Http = new HttpProberBuilder { Path = "/ready", Port = 8080 },
                InitialDelay = 5,
                Period = 10,
                SuccessThreshold = 2,
                FailureThreshold = 5,
            };
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            HasKey(container, "readinessProbe").Should().BeTrue();
            var probe = GetMapping(container, "readinessProbe");
            GetScalar(GetMapping(probe, "httpGet"), "path").Should().Be("/ready");
        }

        [Fact]
        public void CreateDeployment_ProbeWithoutPort_FallsBackToBindingPort()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Bindings.Add(new BindingBuilder { Port = 8080, Protocol = "http" });
            project.Liveness = new ProbeBuilder
            {
                Http = new HttpProberBuilder { Path = "/healthz", Protocol = "http" },
            };
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            var httpGet = GetMapping(GetMapping(container, "livenessProbe"), "httpGet");
            GetScalar(httpGet, "port").Should().Be("8080");
        }

        [Fact]
        public void CreateDeployment_ProbeWithHeaders()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            project.Liveness = new ProbeBuilder
            {
                Http = new HttpProberBuilder
                {
                    Path = "/healthz",
                    Port = 80,
                    Headers = new List<KeyValuePair<string, object>>
                    {
                        new("Authorization", "Bearer token123"),
                    },
                },
            };
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            var httpGet = GetMapping(GetMapping(container, "livenessProbe"), "httpGet");
            var headers = GetSequence(httpGet, "httpHeaders");
            headers.Children.Should().ContainSingle();
            GetScalar((YamlMappingNode)headers[0], "name").Should().Be("Authorization");
            GetScalar((YamlMappingNode)headers[0], "value").Should().Be("Bearer token123");
        }

        [Fact]
        public void CreateDeployment_WithoutProbes_OmitsProbes()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            HasKey(container, "livenessProbe").Should().BeFalse();
            HasKey(container, "readinessProbe").Should().BeFalse();
        }

        // =====================================================================
        // CreateDeployment - Sidecars
        // =====================================================================

        [Fact]
        public void CreateDeployment_WithSidecars_SetsShareProcessNamespace()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Sidecars.Add(new SidecarBuilder("sidecar", "envoy", "v1.20"));
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var templateSpec = GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec");
            GetScalar(templateSpec, "shareProcessNamespace").Should().Be("true");
        }

        [Fact]
        public void CreateDeployment_WithSidecars_AddsSidecarContainer()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Sidecars.Add(new SidecarBuilder("envoy-proxy", "envoy", "latest"));
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var containers = GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers");
            containers.Children.Should().HaveCount(2);
            var sidecar = (YamlMappingNode)containers[1];
            GetScalar(sidecar, "name").Should().Be("envoy-proxy");
            GetScalar(sidecar, "image").Should().Be("envoy:latest");
        }

        [Fact]
        public void CreateDeployment_SidecarWithArgs()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var sidecar = new SidecarBuilder("proxy", "envoy", "v1");
            sidecar.Args.Add("--config-path");
            sidecar.Args.Add("/etc/envoy/envoy.yaml");
            project.Sidecars.Add(sidecar);
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var sidecarContainer = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[1];
            var args = GetSequence(sidecarContainer, "args");
            args.Children.Should().HaveCount(2);
        }

        [Fact]
        public void CreateDeployment_SidecarWithEnvironmentVariables()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var sidecar = new SidecarBuilder("proxy", "envoy", "v1");
            sidecar.EnvironmentVariables.Add(new EnvironmentVariableBuilder("LOG_LEVEL") { Value = "debug" });
            project.Sidecars.Add(sidecar);
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var sidecarContainer = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[1];
            var env = GetSequence(sidecarContainer, "env");
            env.Children.Should().ContainSingle();
            GetScalar((YamlMappingNode)env[0], "name").Should().Be("LOG_LEVEL");
        }

        [Fact]
        public void CreateDeployment_WithoutSidecars_NoShareProcessNamespace()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var templateSpec = GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec");
            HasKey(templateSpec, "shareProcessNamespace").Should().BeFalse();
        }

        // =====================================================================
        // CreateDeployment - RelocateDiagnosticsDomainSockets
        // =====================================================================

        [Fact]
        public void CreateDeployment_RelocateDiagnostics_AddsVolume()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.RelocateDiagnosticsDomainSockets = true;
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            // Need env or bindings to trigger the env section which contains volumeMounts
            project.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var templateSpec = GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec");
            var volumes = GetSequence(templateSpec, "volumes");
            volumes.Children.Should().ContainSingle();
            GetScalar((YamlMappingNode)volumes[0], "name").Should().Be("tye-diagnostics");
        }
        [Fact]
        public void CreateDeployment_RelocateDiagnostics_AddsTMPDIREnvVar()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.RelocateDiagnosticsDomainSockets = true;
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            var env = GetSequence(container, "env");
            env.Children
                .Cast<YamlMappingNode>()
                .Should()
                .Contain(e => GetScalar(e, "name") == "TMPDIR" && GetScalar(e, "value") == "/var/tye/diagnostics");
        }
        [Fact]
        public void CreateDeployment_RelocateDiagnostics_AddsVolumeMount()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.RelocateDiagnosticsDomainSockets = true;
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            project.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var container = (YamlMappingNode)GetSequence(
                GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec"), "containers")[0];
            var volumeMounts = GetSequence(container, "volumeMounts");
            volumeMounts.Children.Should().ContainSingle();
            GetScalar((YamlMappingNode)volumeMounts[0], "name").Should().Be("tye-diagnostics");
            GetScalar((YamlMappingNode)volumeMounts[0], "mountPath").Should().Be("/var/tye/diagnostics");
        }

        [Fact]
        public void CreateDeployment_WithoutRelocateDiagnostics_NoVolumes()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var templateSpec = GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec");
            HasKey(templateSpec, "volumes").Should().BeFalse();
        }

        // =====================================================================
        // CreateDeployment - Image Pull Secrets
        // =====================================================================

        [Fact]
        public void CreateDeployment_WithRegistryPullSecret_SetsImagePullSecrets()
        {
            var registry = new ContainerRegistry("myregistry.azurecr.io", "my-pull-secret");
            var app = CreateApp(registry: registry);
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var templateSpec = GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec");
            var imagePullSecrets = GetSequence(templateSpec, "imagePullSecrets");
            imagePullSecrets.Children.Should().ContainSingle();
            GetScalar((YamlMappingNode)imagePullSecrets[0], "name").Should().Be("my-pull-secret");
        }

        [Fact]
        public void CreateDeployment_WithoutPullSecret_OmitsImagePullSecrets()
        {
            var app = CreateApp();
            var project = CreateProjectService("web");
            project.Outputs.Add(new DockerImageOutput("web", "v1"));
            var deployment = CreateDeployment("web");

            var result = KubernetesManifestGenerator.CreateDeployment(_output, app, project, deployment);

            var templateSpec = GetMapping(GetMapping(GetMapping(GetRootMapping(result.Yaml), "spec"), "template"), "spec");
            HasKey(templateSpec, "imagePullSecrets").Should().BeFalse();
        }

        // =====================================================================
        // CreateIngress
        // =====================================================================

        [Fact]
        public async Task CreateIngress_SetsKindIngress()
        {
            var app = CreateApp();
            var ingress = new IngressBuilder("my-ingress");

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var root = GetRootMapping(result.Yaml);
            GetScalar(root, "kind").Should().Be("Ingress");
        }

        [Fact]
        public async Task CreateIngress_SetsMetadataName()
        {
            var app = CreateApp();
            var ingress = new IngressBuilder("api-gateway");

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            GetScalar(metadata, "name").Should().Be("api-gateway");
        }

        [Fact]
        public async Task CreateIngress_OutputNameMatchesIngressName()
        {
            var app = CreateApp();
            var ingress = new IngressBuilder("my-gw");

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            result.Name.Should().Be("my-gw");
        }

        [Fact]
        public async Task CreateIngress_WithNamespace_SetsNamespace()
        {
            var app = CreateApp(ns: "production");
            var ingress = new IngressBuilder("gw");

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            GetScalar(metadata, "namespace").Should().Be("production");
        }

        [Fact]
        public async Task CreateIngress_WithoutNamespace_OmitsNamespace()
        {
            var app = CreateApp();
            var ingress = new IngressBuilder("gw");

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var metadata = GetMapping(GetRootMapping(result.Yaml), "metadata");
            HasKey(metadata, "namespace").Should().BeFalse();
        }

        [Fact]
        public async Task CreateIngress_SetsNginxAnnotations()
        {
            var app = CreateApp();
            var ingress = new IngressBuilder("gw");

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var annotations = GetMapping(GetMapping(GetRootMapping(result.Yaml), "metadata"), "annotations");
            GetScalar(annotations, "kubernetes.io/ingress.class").Should().Be("nginx");
            GetScalar(annotations, "nginx.ingress.kubernetes.io/rewrite-target").Should().Be("/$2");
        }

        [Fact]
        public async Task CreateIngress_SetsPartOfLabel()
        {
            var app = CreateApp(name: "my-app");
            var ingress = new IngressBuilder("gw");

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var labels = GetMapping(GetMapping(GetRootMapping(result.Yaml), "metadata"), "labels");
            GetScalar(labels, "app.kubernetes.io/part-of").Should().Be("my-app");
        }

        [Fact]
        public async Task CreateIngress_NoRules_ReturnsWithoutSpecRules()
        {
            var app = CreateApp();
            var ingress = new IngressBuilder("gw");

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            HasKey(spec, "rules").Should().BeFalse();
        }

        [Fact]
        public async Task CreateIngress_WithRule_ServiceNotFound_Throws()
        {
            var app = CreateApp();
            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Path = "/", Service = "nonexistent" });

            var act = () => KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Could not resolve service*nonexistent*");
        }

        [Fact]
        public async Task CreateIngress_WithRule_NoHttpBinding_Throws()
        {
            var app = CreateApp();
            var svc = CreateProjectService("web");
            svc.Bindings.Add(new BindingBuilder { Name = "grpc", Port = 5000, Protocol = "grpc" });
            app.Services.Add(svc);
            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Path = "/", Service = "web" });

            var act = () => KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Could not resolve an http binding*web*");
        }

        [Fact]
        public async Task CreateIngress_RootPath_GeneratesCorrectPathPattern()
        {
            var app = CreateApp();
            var svc = CreateProjectService("web");
            svc.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            app.Services.Add(svc);
            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Path = "/", Service = "web" });

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var spec = GetMapping(GetRootMapping(result.Yaml), "spec");
            var rules = GetSequence(spec, "rules");
            var pathNode = (YamlMappingNode)GetSequence(GetMapping((YamlMappingNode)rules[0], "http"), "paths")[0];
            GetScalar(pathNode, "path").Should().Be("/()(.*)");
        }

        [Fact]
        public async Task CreateIngress_SubPath_GeneratesPathRegex()
        {
            var app = CreateApp();
            var svc = CreateProjectService("api");
            svc.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            app.Services.Add(svc);
            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Path = "/api", Service = "api" });

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var pathNode = (YamlMappingNode)GetSequence(
                GetMapping((YamlMappingNode)GetSequence(GetMapping(GetRootMapping(result.Yaml), "spec"), "rules")[0], "http"), "paths")[0];
            GetScalar(pathNode, "path").Should().Be("/api(/|$)(.*)");
        }

        [Fact]
        public async Task CreateIngress_PreservePath_GeneratesPreservePathRegex()
        {
            var app = CreateApp();
            var svc = CreateProjectService("api");
            svc.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            app.Services.Add(svc);
            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Path = "/api", Service = "api", PreservePath = true });

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var pathNode = (YamlMappingNode)GetSequence(
                GetMapping((YamlMappingNode)GetSequence(GetMapping(GetRootMapping(result.Yaml), "spec"), "rules")[0], "http"), "paths")[0];
            GetScalar(pathNode, "path").Should().Be("/()(api.*)");
        }

        [Fact]
        public async Task CreateIngress_WithHost_SetsHost()
        {
            var app = CreateApp();
            var svc = CreateProjectService("web");
            svc.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            app.Services.Add(svc);
            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Host = "example.com", Path = "/", Service = "web" });

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var rule = (YamlMappingNode)GetSequence(GetMapping(GetRootMapping(result.Yaml), "spec"), "rules")[0];
            GetScalar(rule, "host").Should().Be("example.com");
        }

        [Fact]
        public async Task CreateIngress_WithoutHost_OmitsHost()
        {
            var app = CreateApp();
            var svc = CreateProjectService("web");
            svc.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            app.Services.Add(svc);
            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Path = "/", Service = "web" });

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var rule = (YamlMappingNode)GetSequence(GetMapping(GetRootMapping(result.Yaml), "spec"), "rules")[0];
            HasKey(rule, "host").Should().BeFalse();
        }

        [Fact]
        public async Task CreateIngress_DefaultPort80_WhenNoPortSpecified()
        {
            var app = CreateApp();
            var svc = CreateProjectService("web");
            svc.Bindings.Add(new BindingBuilder { Protocol = "http" }); // no port
            app.Services.Add(svc);
            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Path = "/", Service = "web" });

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            // The backend should use port 80 as default
            var pathNode = (YamlMappingNode)GetSequence(
                GetMapping((YamlMappingNode)GetSequence(GetMapping(GetRootMapping(result.Yaml), "spec"), "rules")[0], "http"), "paths")[0];
            var backend = GetMapping(pathNode, "backend");

            // Depending on k8s version, the port is in different locations
            // For v1: backend.service.port.number
            // For v1beta1: backend.servicePort
            if (HasKey(backend, "service"))
            {
                var port = GetMapping(GetMapping(backend, "service"), "port");
                GetScalar(port, "number").Should().Be("80");
            }
            else
            {
                GetScalar(backend, "servicePort").Should().Be("80");
            }
        }
        [Fact]
        public async Task CreateIngress_RulesGroupedByHost()
        {
            var app = CreateApp();
            var svc1 = CreateProjectService("web");
            svc1.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            app.Services.Add(svc1);
            var svc2 = CreateProjectService("api");
            svc2.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            app.Services.Add(svc2);

            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Host = "example.com", Path = "/", Service = "web" });
            ingress.Rules.Add(new IngressRuleBuilder { Host = "example.com", Path = "/api", Service = "api" });

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var rules = GetSequence(GetMapping(GetRootMapping(result.Yaml), "spec"), "rules");
            // Both rules share the same host, so they should be grouped into one rule
            rules.Children.Should().ContainSingle();
            var rule = (YamlMappingNode)rules[0];
            GetScalar(rule, "host").Should().Be("example.com");

            var paths = GetSequence(GetMapping(rule, "http"), "paths");
            paths.Children.Should().HaveCount(2);

            var actualPaths = paths.Children
                .Cast<YamlMappingNode>()
                .Select(path =>
                {
                    var backend = GetMapping(path, "backend");
                    var service = HasKey(backend, "service")
                        ? GetScalar(GetMapping(backend, "service"), "name")
                        : GetScalar(backend, "serviceName");

                    return new
                    {
                        Path = GetScalar(path, "path"),
                        Service = service,
                    };
                });

            actualPaths.Should().BeEquivalentTo(
                new[]
                {
                    new { Path = "/()(.*)", Service = "web" },
                    new { Path = "/api(/|$)(.*)", Service = "api" },
                });
        }
        [Fact]
        public async Task CreateIngress_DifferentHosts_CreatesSeparateRules()
        {
            var app = CreateApp();
            var svc1 = CreateProjectService("web");
            svc1.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            app.Services.Add(svc1);
            var svc2 = CreateProjectService("api");
            svc2.Bindings.Add(new BindingBuilder { Port = 80, Protocol = "http" });
            app.Services.Add(svc2);

            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Host = "web.example.com", Path = "/", Service = "web" });
            ingress.Rules.Add(new IngressRuleBuilder { Host = "api.example.com", Path = "/", Service = "api" });

            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            var rules = GetSequence(GetMapping(GetRootMapping(result.Yaml), "spec"), "rules");
            rules.Children.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreateIngress_UnnamedBinding_ResolvedAsHttp()
        {
            var app = CreateApp();
            var svc = CreateProjectService("web");
            svc.Bindings.Add(new BindingBuilder { Port = 3000 }); // no name, no protocol
            app.Services.Add(svc);
            var ingress = new IngressBuilder("gw");
            ingress.Rules.Add(new IngressRuleBuilder { Path = "/", Service = "web" });

            // Should not throw - unnamed bindings are treated as http
            var result = await KubernetesManifestGenerator.CreateIngress(_output, app, ingress);

            result.Should().NotBeNull();
        }
    }
}
