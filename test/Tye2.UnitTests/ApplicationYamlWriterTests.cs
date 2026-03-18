using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions;
using Tye2.Core;
using YamlDotNet.RepresentationModel;
using Xunit;

namespace Tye2.UnitTests
{
    public class ApplicationYamlWriterTests
    {
        [Fact]
        public async Task WriteAsync_NoYamlManifestOutputs_WritesDebugAndSkipsYaml()
        {
            var console = new TestConsole();
            var output = new OutputContext(console, Verbosity.Debug);
            var application = CreateApplication();

            var service = new ContainerServiceBuilder("api", "nginx", ServiceSource.Configuration);
            service.Outputs.Add(new PlainServiceOutput());
            application.Services.Add(service);

            var ingress = new IngressBuilder("gateway");
            ingress.Outputs.Add(new PlainIngressOutput());
            application.Ingress.Add(ingress);

            var text = await WriteAndReadAsync(output, application);

            text.Should().BeEmpty();
            console.Out.ToString().Should().Contain("No yaml manifests found. Skipping.");
        }

        [Fact]
        public async Task WriteAsync_ServiceYamlOutput_WritesSingleDocument()
        {
            var output = new OutputContext(new TestConsole(), Verbosity.Debug);
            var application = CreateApplication();

            var service = new ContainerServiceBuilder("api", "nginx", ServiceSource.Configuration);
            service.Outputs.Add(new KubernetesServiceOutput("api", CreateYaml("Service", "api")));
            application.Services.Add(service);

            var text = await WriteAndReadAsync(output, application);
            var stream = LoadYaml(text);

            stream.Documents.Should().HaveCount(1);
            GetScalar(stream.Documents[0], "kind").Should().Be("Service");
            GetNestedScalar(stream.Documents[0], "metadata", "name").Should().Be("api");
        }

        [Fact]
        public async Task WriteAsync_ServiceAndIngressYamlOutputs_WritesAllDocumentsInOrder()
        {
            var output = new OutputContext(new TestConsole(), Verbosity.Debug);
            var application = CreateApplication();

            var service = new ContainerServiceBuilder("api", "nginx", ServiceSource.Configuration);
            service.Outputs.Add(new KubernetesServiceOutput("api", CreateYaml("Service", "api")));
            service.Outputs.Add(new KubernetesDeploymentOutput("api", CreateYaml("Deployment", "api")));
            application.Services.Add(service);

            var ingress = new IngressBuilder("gateway");
            ingress.Outputs.Add(new KubernetesIngressOutput("gateway", CreateYaml("Ingress", "gateway")));
            application.Ingress.Add(ingress);

            var text = await WriteAndReadAsync(output, application);
            var stream = LoadYaml(text);

            stream.Documents.Should().HaveCount(3);
            stream.Documents.Select(d => GetScalar(d, "kind"))
                .Should().ContainInOrder("Service", "Deployment", "Ingress");
        }

        private static async Task<string> WriteAndReadAsync(OutputContext output, ApplicationBuilder application)
        {
            using var memory = new MemoryStream();
            using (var writer = new StreamWriter(memory, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
            {
                await ApplicationYamlWriter.WriteAsync(output, writer, application);
                await writer.FlushAsync();
            }

            memory.Position = 0;
            using var reader = new StreamReader(memory, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }

        private static ApplicationBuilder CreateApplication()
        {
            var source = new FileInfo(Path.Combine(Path.GetTempPath(), $"tye2-app-yaml-writer-{System.Guid.NewGuid():N}.yaml"));
            return new ApplicationBuilder(source, "yaml-writer-test", new ContainerEngine(default), dashboardPort: null);
        }

        private static YamlDocument CreateYaml(string kind, string name)
        {
            return new YamlDocument(
                new YamlMappingNode
                {
                    { "apiVersion", "v1" },
                    { "kind", kind },
                    {
                        "metadata",
                        new YamlMappingNode
                        {
                            { "name", name },
                        }
                    },
                });
        }

        private static YamlStream LoadYaml(string yaml)
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yaml));
            return stream;
        }

        private static string GetScalar(YamlDocument document, string key)
        {
            var root = (YamlMappingNode)document.RootNode;
            return root.Children[new YamlScalarNode(key)].ToString();
        }

        private static string GetNestedScalar(YamlDocument document, string parentKey, string key)
        {
            var root = (YamlMappingNode)document.RootNode;
            var parent = (YamlMappingNode)root.Children[new YamlScalarNode(parentKey)];
            return parent.Children[new YamlScalarNode(key)].ToString();
        }

        private sealed class PlainServiceOutput : ServiceOutput
        {
        }

        private sealed class PlainIngressOutput : IngressOutput
        {
        }
    }
}
