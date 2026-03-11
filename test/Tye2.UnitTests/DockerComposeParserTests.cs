using AwesomeAssertions;
using System;
using System.IO;
using System.Linq;
using Tye2.Core;
using Tye2.Core.DockerCompose;
using Tye2.Core.Serialization;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace Tye2.UnitTests
{
    public class DockerComposeParserTests : IDisposable
    {
        private readonly string _tempDir;

        public DockerComposeParserTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"tye2_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private string CreateTempDir(string name)
        {
            var dir = Path.Combine(_tempDir, name);
            Directory.CreateDirectory(dir);
            return dir;
        }

        // =====================================================================
        // ParseConfigApplication — Basic Parsing
        // =====================================================================

        [Fact]
        public void Parse_MinimalCompose_ReturnsEmptyApp()
        {
            using var parser = new DockerComposeParser("version: '3'");
            var app = parser.ParseConfigApplication();
            app.Should().NotBeNull();
            app.Services.Should().BeEmpty();
        }

        [Fact]
        public void Parse_SingleService_WithImage()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
services:
  redis:
    image: redis:latest
");
            var app = parser.ParseConfigApplication();
            var svc = app.Services.Should().ContainSingle().Subject;
            svc.Name.Should().Be("redis");
            svc.Image.Should().Be("redis:latest");
        }

        [Fact]
        public void Parse_MultipleServices()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
services:
  web:
    image: nginx
  db:
    image: postgres
  cache:
    image: redis
");
            var app = parser.ParseConfigApplication();
            app.Services.Count.Should().Be(3);
            var names = app.Services.Select(s => s.Name).OrderBy(n => n).ToList();
            names.Should().Contain("cache");
            names.Should().Contain("db");
            names.Should().Contain("web");
        }

        // =====================================================================
        // Scalar Helpers
        // =====================================================================

        [Fact]
        public void GetScalarValue_ScalarNode_ReturnsValue()
        {
            var value = DockerComposeParser.GetScalarValue(new YamlScalarNode("hello"));

            value.Should().Be("hello");
        }

        [Fact]
        public void GetScalarValue_NonScalarNode_Throws()
        {
            var ex = ((Action)(() => DockerComposeParser.GetScalarValue(new YamlSequenceNode())))
                .Should().Throw<TyeYamlException>().Which;

            ex.Message.Should().Contain(CoreStrings.FormatUnexpectedType(
                YamlNodeType.Scalar.ToString(),
                YamlNodeType.Sequence.ToString()));
        }

        [Fact]
        public void GetScalarValue_WithKey_ScalarNode_ReturnsValue()
        {
            var value = DockerComposeParser.GetScalarValue("name", new YamlScalarNode("service-a"));

            value.Should().Be("service-a");
        }

        [Fact]
        public void GetScalarValue_WithKey_NonScalarNode_Throws()
        {
            var ex = ((Action)(() => DockerComposeParser.GetScalarValue("name", new YamlMappingNode())))
                .Should().Throw<TyeYamlException>().Which;

            ex.Message.Should().Contain(CoreStrings.FormatExpectedYamlScalar("name"));
        }

        [Fact]
        public void ThrowIfNotYamlSequence_SequenceNode_DoesNotThrow()
        {
            var act = () => DockerComposeParser.ThrowIfNotYamlSequence("ports", new YamlSequenceNode());

            act.Should().NotThrow();
        }

        [Fact]
        public void ThrowIfNotYamlSequence_NonSequenceNode_Throws()
        {
            var ex = ((Action)(() => DockerComposeParser.ThrowIfNotYamlSequence("ports", new YamlScalarNode("8080"))))
                .Should().Throw<TyeYamlException>().Which;

            ex.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("ports"));
        }

        // =====================================================================
        // Ports Parsing
        // =====================================================================

        [Fact]
        public void Parse_Ports_HostAndContainer()
        {
            using var parser = new DockerComposeParser(@"
services:
  web:
    image: nginx
    ports:
      - 8080:80
");
            var app = parser.ParseConfigApplication();
            var binding = app.Services.Single().Bindings.Should().ContainSingle().Subject;
            binding.Port.Should().Be(8080);
            binding.ContainerPort.Should().Be(80);
            binding.Protocol.Should().Be("http");
        }

        [Fact]
        public void Parse_Ports_SinglePort_SetsPortAndContainerPort()
        {
            using var parser = new DockerComposeParser(@"
services:
  web:
    image: nginx
    ports:
      - 3000
");
            var app = parser.ParseConfigApplication();
            var binding = app.Services.Single().Bindings.Should().ContainSingle().Subject;
            binding.Port.Should().Be(3000);
            binding.ContainerPort.Should().Be(3000);
        }

        [Fact]
        public void Parse_Ports_MultiplePorts()
        {
            using var parser = new DockerComposeParser(@"
services:
  web:
    image: nginx
    ports:
      - 80:80
      - 443:443
      - 8080:8080
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Bindings.Count.Should().Be(3);
        }

        [Fact]
        public void Parse_Ports_MultiplePorts_AssignsBindingNames()
        {
            using var parser = new DockerComposeParser(@"
services:
  rabbitmq:
    image: rabbitmq
    ports:
      - 5672:5672
      - 15672:15672
      - 15692:15692
");
            var app = parser.ParseConfigApplication();
            var bindings = app.Services.Single().Bindings;

            bindings.Should().HaveCount(3);
            bindings.Select(b => b.Name).Should().OnlyContain(name => !string.IsNullOrWhiteSpace(name));
            bindings.Select(b => b.Name).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void Parse_Ports_AllSetToHttpProtocol()
        {
            using var parser = new DockerComposeParser(@"
services:
  web:
    image: nginx
    ports:
      - 8080:80
      - 9090:90
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Bindings.Should().OnlyContain(b => b.Protocol == "http");
        }

        [Fact]
        public void Parse_Ports_RangeSyntax_ParsesExpandedBindings()
        {
            using var parser = new DockerComposeParser(@"
services:
  mongo:
    image: mongo
    ports:
      - 27017-27019:27017-27019
");
            var app = parser.ParseConfigApplication();
            var bindings = app.Services.Single().Bindings;
            bindings.Should().HaveCount(3);
            bindings.Select(b => b.Port).Should().Equal(27017, 27018, 27019);
            bindings.Select(b => b.ContainerPort).Should().Equal(27017, 27018, 27019);
            bindings.Should().OnlyContain(b => b.Protocol == "http");
        }

        [Fact]
        public void Parse_Ports_RangeSyntax_AssignsBindingNames()
        {
            using var parser = new DockerComposeParser(@"
services:
  mongo:
    image: mongo
    ports:
      - 27017-27019:27017-27019
");
            var app = parser.ParseConfigApplication();
            var bindings = app.Services.Single().Bindings;

            bindings.Should().HaveCount(3);
            bindings.Select(b => b.Name).Should().OnlyContain(name => !string.IsNullOrWhiteSpace(name));
            bindings.Select(b => b.Name).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void Parse_Ports_HostIpMapping_ParsesHostPortAndContainerPort()
        {
            using var parser = new DockerComposeParser(@"
services:
  mongo:
    image: mongo
    ports:
      - 127.0.0.1:27017:27017
");
            var app = parser.ParseConfigApplication();
            var binding = app.Services.Single().Bindings.Should().ContainSingle().Subject;
            binding.Host.Should().Be("127.0.0.1");
            binding.Port.Should().Be(27017);
            binding.ContainerPort.Should().Be(27017);
            binding.Protocol.Should().Be("http");
        }

        // =====================================================================
        // Environment Parsing — Sequence Format
        // =====================================================================
        [Fact]
        public void Parse_Environment_SequenceFormat_KeyValue()
        {
            using var parser = new DockerComposeParser(@"
services:
  db:
    image: postgres
    environment:
      - POSTGRES_PASSWORD=secret
      - POSTGRES_DB=mydb
");
            var app = parser.ParseConfigApplication();
            var config = app.Services.Single().Configuration;
            config.Count.Should().Be(2);
            config.First(c => c.Name == "POSTGRES_PASSWORD").Value.Should().Be("secret");
            config.First(c => c.Name == "POSTGRES_DB").Value.Should().Be("mydb");
        }

        [Fact]
        public void Parse_Environment_SequenceFormat_KeyOnly()
        {
            using var parser = new DockerComposeParser(@"
services:
  app:
    image: myapp
    environment:
      - SOME_VAR
");
            var app = parser.ParseConfigApplication();
            var cfg = app.Services.Single().Configuration.Should().ContainSingle().Subject;
            cfg.Name.Should().Be("SOME_VAR");
            cfg.Value.Should().BeNull();
        }

        // =====================================================================
        // Environment Parsing — Mapping Format
        // =====================================================================

        [Fact]
        public void Parse_Environment_MappingFormat()
        {
            using var parser = new DockerComposeParser(@"
services:
  db:
    image: postgres
    environment:
      POSTGRES_PASSWORD: secret
      POSTGRES_DB: mydb
");
            var app = parser.ParseConfigApplication();
            var config = app.Services.Single().Configuration;
            config.Count.Should().Be(2);
            config.First(c => c.Name == "POSTGRES_PASSWORD").Value.Should().Be("secret");
            config.First(c => c.Name == "POSTGRES_DB").Value.Should().Be("mydb");
        }

        // =====================================================================
        // Build Section Parsing
        // =====================================================================

        [Fact]
        public void Parse_Build_Dockerfile()
        {
            using var parser = new DockerComposeParser(@"
services:
  app:
    build:
      dockerfile: Dockerfile.prod
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().DockerFile.Should().Be("Dockerfile.prod");
        }

        [Fact]
        public void Parse_Build_Context_FindsCsproj()
        {
            var contextDir = CreateTempDir("myapp");
            File.WriteAllText(Path.Combine(contextDir, "MyApp.csproj"), "<Project />");

            using var parser = new DockerComposeParser($@"
services:
  app:
    build:
      context: {contextDir.Replace('\\', '/')}
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Project.Should().Contain("MyApp.csproj");
        }

        [Fact]
        public void Parse_Build_Context_FindsFsproj()
        {
            var contextDir = CreateTempDir("fsharpapp");
            File.WriteAllText(Path.Combine(contextDir, "MyApp.fsproj"), "<Project />");

            using var parser = new DockerComposeParser($@"
services:
  app:
    build:
      context: {contextDir.Replace('\\', '/')}
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Project.Should().Contain("MyApp.fsproj");
        }

        [Fact]
        public void Parse_Build_Context_MultipleCsprojs_Throws()
        {
            var contextDir = CreateTempDir("multiproj");
            File.WriteAllText(Path.Combine(contextDir, "App1.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(contextDir, "App2.csproj"), "<Project />");

            using var parser = new DockerComposeParser($@"
services:
  app:
    build:
      context: {contextDir.Replace('\\', '/')}
");
            var ex = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            ex.Message.Should().Contain("Multiple proj files");
        }

        [Fact]
        public void Parse_Build_Context_NoProj_ServiceProjectIsNull()
        {
            var contextDir = CreateTempDir("noproj");

            using var parser = new DockerComposeParser($@"
services:
  app:
    build:
      context: {contextDir.Replace('\\', '/')}
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Project.Should().BeNull();
        }

        [Fact]
        public void Parse_Build_UnrecognizedKey_Throws()
        {
            using var parser = new DockerComposeParser(@"
services:
  app:
    build:
      unknown_key: value
");
            var ex = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            ex.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("unknown_key"));
        }

        // =====================================================================
        // Top-Level Keys
        // =====================================================================

        [Fact]
        public void Parse_VersionKey_Ignored()
        {
            using var parser = new DockerComposeParser("version: '3.8'");
            var app = parser.ParseConfigApplication();
            app.Should().NotBeNull();
        }

        [Fact]
        public void Parse_VolumesKey_Ignored()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
volumes:
  data:
");
            var app = parser.ParseConfigApplication();
            app.Should().NotBeNull();
        }

        [Fact]
        public void Parse_VolumesKey_WithSupportedOptions_DoesNotThrow()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
volumes:
  data:
    driver: local
    labels:
      purpose: test
    name: shared-data
  cache:
    external: true
");
            var app = parser.ParseConfigApplication();
            app.Should().NotBeNull();
        }

        [Fact]
        public void Parse_VolumesKey_WithUnknownOption_Throws()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
volumes:
  data:
    unsupported_option: true
");
            var ex = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            ex.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("unsupported_option"));
        }

        [Fact]
        public void Parse_NetworksKey_Ignored()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
networks:
  frontend:
");
            var app = parser.ParseConfigApplication();
            app.Should().NotBeNull();
        }

        [Fact]
        public void Parse_NetworksKey_WithSupportedOptions_DoesNotThrow()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
networks:
  frontend:
    driver: bridge
    attachable: true
    labels:
      env: test
  backend:
    internal: true
");
            var app = parser.ParseConfigApplication();
            app.Should().NotBeNull();
        }

        [Fact]
        public void Parse_NetworksKey_WithUnknownOption_Throws()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
networks:
  frontend:
    unsupported_network_option: true
");
            var ex = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            ex.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("unsupported_network_option"));
        }

        [Fact]
        public void Parse_ConfigsKey_Ignored()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
configs:
  my_config:
");
            var app = parser.ParseConfigApplication();
            app.Should().NotBeNull();
        }

        [Fact]
        public void Parse_SecretsKey_Ignored()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
secrets:
  db_password:
");
            var app = parser.ParseConfigApplication();
            app.Should().NotBeNull();
        }

        [Fact]
        public void Parse_UnrecognizedTopLevelKey_Throws()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
unknown_top_level: value
");
            var ex = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            ex.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("unknown_top_level"));
        }

        // =====================================================================
        // Service-Level Ignored Keys (should not throw)
        // =====================================================================

        [Theory]
        [InlineData("cap_add:\n      - NET_ADMIN")]
        [InlineData("cap_drop:\n      - ALL")]
        [InlineData("command: --verbose")]
        [InlineData("container_name: my-container")]
        [InlineData("depends_on:\n      - db")]
        [InlineData("dns: 8.8.8.8")]
        [InlineData("hostname: myhost")]
        [InlineData("restart: always")]
        [InlineData("tty: true")]
        [InlineData("stdin_open: true")]
        [InlineData("working_dir: /app")]
        [InlineData("user: root")]
        [InlineData("privileged: true")]
        [InlineData("read_only: true")]
        public void Parse_Service_IgnoredKeys_DoNotThrow(string serviceProperty)
        {
            using var parser = new DockerComposeParser($@"
services:
  app:
    image: myapp
    {serviceProperty}
");
            var app = parser.ParseConfigApplication();
            app.Services.Should().ContainSingle();
        }

        [Theory]
        [InlineData("cgroup_parent: parent")]
        [InlineData("configs: {}")]
        [InlineData("credential_spec: spec")]
        [InlineData("deploy: {}")]
        [InlineData("devices: []")]
        [InlineData("dns_search: example.local")]
        [InlineData("endpoint: endpoint")]
        [InlineData("env_file: .env")]
        [InlineData("expose: []")]
        [InlineData("external_links: []")]
        [InlineData("extra_hosts: []")]
        [InlineData("healthcheck: {}")]
        [InlineData("init: true")]
        [InlineData("isolation: process")]
        [InlineData("labels: {}")]
        [InlineData("links: []")]
        [InlineData("logging: {}")]
        [InlineData("network_mode: bridge")]
        [InlineData("networks: {}")]
        [InlineData("pid: host")]
        [InlineData("secrets: []")]
        [InlineData("security_opt: []")]
        [InlineData("stop_grace_period: 1s")]
        [InlineData("stop_signal: SIGTERM")]
        [InlineData("sysctls: {}")]
        [InlineData("tmpfs: []")]
        [InlineData("ulimits: {}")]
        [InlineData("userns_mode: host")]
        [InlineData("volumes: []")]
        [InlineData("domainname: example.com")]
        [InlineData("ipc: host")]
        [InlineData("mac_address: '00:11:22:33:44:55'")]
        [InlineData("shm_size: 64m")]
        public void Parse_Service_AdditionalIgnoredKeys_DoNotThrow(string serviceProperty)
        {
            using var parser = new DockerComposeParser($@"
services:
  app:
    image: myapp
    {serviceProperty}
");
            var app = parser.ParseConfigApplication();
            app.Services.Should().ContainSingle();
        }

        [Fact]
        public void Parse_Service_UnrecognizedKey_Throws()
        {
            using var parser = new DockerComposeParser(@"
services:
  app:
    image: myapp
    totally_unknown: value
");
            var ex = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            ex.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("totally_unknown"));
        }

        // =====================================================================
        // Collection Initialization
        // =====================================================================

        [Fact]
        public void Parse_Service_CollectionsInitialized()
        {
            using var parser = new DockerComposeParser(@"
services:
  app:
    image: myapp
");
            var app = parser.ParseConfigApplication();
            var svc = app.Services.Single();
            svc.Bindings.Should().NotBeNull();
            svc.Bindings.Should().BeEmpty();
            svc.Configuration.Should().NotBeNull();
            svc.Configuration.Should().BeEmpty();
            svc.Volumes.Should().NotBeNull();
            svc.Volumes.Should().BeEmpty();
            svc.Tags.Should().NotBeNull();
            svc.Tags.Should().BeEmpty();
        }

        // =====================================================================
        // Error Handling
        // =====================================================================

        [Fact]
        public void Parse_InvalidYaml_ThrowsTyeYamlException()
        {
            using var parser = new DockerComposeParser("{ broken yaml [}");
            var ex = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            ex.Message.Should().Contain("Unable to parse");
        }

        [Fact]
        public void Parse_EmptyDocument_Throws()
        {
            using var parser = new DockerComposeParser("");
            ((Action)(() => parser.ParseConfigApplication())).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Parse_RootIsSequence_Throws()
        {
            using var parser = new DockerComposeParser("- item1\n- item2");
            var ex = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            ex.Message.Should().Contain(CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Sequence.ToString()));
        }

        [Fact]
        public void Parse_RootIsScalar_Throws()
        {
            using var parser = new DockerComposeParser("justascalar");
            var ex = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            ex.Message.Should().Contain(CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString()));
        }

        // =====================================================================
        // Name Inference
        // =====================================================================

        [Fact]
        public void Parse_WithFileInfo_InfersNameFromFile()
        {
            var file = Path.Combine(_tempDir, "docker-compose.yaml");
            File.WriteAllText(file, @"
version: '3'
services:
  web:
    image: nginx
");
            using var parser = new DockerComposeParser(new FileInfo(file));
            var app = parser.ParseConfigApplication();
            app.Name.Should().NotBeNull();
            app.Name.Should().NotBeEmpty();
        }

        [Fact]
        public void Parse_SetsSourceFromFileInfo()
        {
            var file = Path.Combine(_tempDir, "docker-compose.yaml");
            File.WriteAllText(file, "version: '3'");
            var fileInfo = new FileInfo(file);

            using var parser = new DockerComposeParser(fileInfo);
            var app = parser.ParseConfigApplication();
            app.Source.FullName.Should().Be(fileInfo.FullName);
        }

        // =====================================================================
        // Full Compose File
        // =====================================================================

        [Fact]
        public void Parse_RealisticComposeFile()
        {
            using var parser = new DockerComposeParser(@"
version: '3.8'
services:
  web:
    image: nginx:alpine
    ports:
      - 80:80
      - 443:443
    depends_on:
      - api
    restart: always

  api:
    image: myapi:latest
    ports:
      - 5000:5000
    environment:
      - DB_HOST=db
      - DB_PORT=5432
    depends_on:
      - db

  db:
    image: postgres:15
    ports:
      - 5432:5432
    environment:
      POSTGRES_PASSWORD: secret
      POSTGRES_DB: myapp
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
");
            var app = parser.ParseConfigApplication();
            app.Services.Count.Should().Be(3);

            var web = app.Services.First(s => s.Name == "web");
            web.Image.Should().Be("nginx:alpine");
            web.Bindings.Count.Should().Be(2);

            var api = app.Services.First(s => s.Name == "api");
            api.Image.Should().Be("myapi:latest");
            api.Bindings.Should().ContainSingle();
            api.Bindings.Single().Port.Should().Be(5000);
            api.Configuration.Count.Should().Be(2);

            var db = app.Services.First(s => s.Name == "db");
            db.Image.Should().Be("postgres:15");
            db.Configuration.First(c => c.Name == "POSTGRES_PASSWORD").Value.Should().Be("secret");
        }
    }
}









