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
            Assert.NotNull(app);
            Assert.Empty(app.Services);
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
            var svc = Assert.Single(app.Services);
            Assert.Equal("redis", svc.Name);
            Assert.Equal("redis:latest", svc.Image);
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
            Assert.Equal(3, app.Services.Count);
            var names = app.Services.Select(s => s.Name).OrderBy(n => n).ToList();
            Assert.Contains("cache", names);
            Assert.Contains("db", names);
            Assert.Contains("web", names);
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
            var binding = Assert.Single(app.Services.Single().Bindings);
            Assert.Equal(8080, binding.Port);
            Assert.Equal(80, binding.ContainerPort);
            Assert.Equal("http", binding.Protocol);
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
            var binding = Assert.Single(app.Services.Single().Bindings);
            Assert.Equal(3000, binding.Port);
            Assert.Equal(3000, binding.ContainerPort);
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
            Assert.Equal(3, app.Services.Single().Bindings.Count);
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
            Assert.All(app.Services.Single().Bindings, b => Assert.Equal("http", b.Protocol));
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
            Assert.Equal(2, config.Count);
            Assert.Equal("secret", config.First(c => c.Name == "POSTGRES_PASSWORD").Value);
            Assert.Equal("mydb", config.First(c => c.Name == "POSTGRES_DB").Value);
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
            var cfg = Assert.Single(app.Services.Single().Configuration);
            Assert.Equal("SOME_VAR", cfg.Name);
            Assert.Null(cfg.Value);
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
            Assert.Equal(2, config.Count);
            Assert.Equal("secret", config.First(c => c.Name == "POSTGRES_PASSWORD").Value);
            Assert.Equal("mydb", config.First(c => c.Name == "POSTGRES_DB").Value);
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
            Assert.Equal("Dockerfile.prod", app.Services.Single().DockerFile);
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
            Assert.Contains("MyApp.csproj", app.Services.Single().Project);
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
            Assert.Contains("MyApp.fsproj", app.Services.Single().Project);
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
            var ex = Assert.Throws<TyeYamlException>(() => parser.ParseConfigApplication());
            Assert.Contains("Multiple proj files", ex.Message);
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
            Assert.Null(app.Services.Single().Project);
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
            var ex = Assert.Throws<TyeYamlException>(() => parser.ParseConfigApplication());
            Assert.Contains(CoreStrings.FormatUnrecognizedKey("unknown_key"), ex.Message);
        }

        // =====================================================================
        // Top-Level Keys
        // =====================================================================

        [Fact]
        public void Parse_VersionKey_Ignored()
        {
            using var parser = new DockerComposeParser("version: '3.8'");
            var app = parser.ParseConfigApplication();
            Assert.NotNull(app);
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
            Assert.NotNull(app);
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
            Assert.NotNull(app);
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
            Assert.NotNull(app);
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
            Assert.NotNull(app);
        }

        [Fact]
        public void Parse_UnrecognizedTopLevelKey_Throws()
        {
            using var parser = new DockerComposeParser(@"
version: '3'
unknown_top_level: value
");
            var ex = Assert.Throws<TyeYamlException>(() => parser.ParseConfigApplication());
            Assert.Contains(CoreStrings.FormatUnrecognizedKey("unknown_top_level"), ex.Message);
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
            Assert.Single(app.Services);
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
            var ex = Assert.Throws<TyeYamlException>(() => parser.ParseConfigApplication());
            Assert.Contains(CoreStrings.FormatUnrecognizedKey("totally_unknown"), ex.Message);
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
            Assert.NotNull(svc.Bindings);
            Assert.Empty(svc.Bindings);
            Assert.NotNull(svc.Configuration);
            Assert.Empty(svc.Configuration);
            Assert.NotNull(svc.Volumes);
            Assert.Empty(svc.Volumes);
            Assert.NotNull(svc.Tags);
            Assert.Empty(svc.Tags);
        }

        // =====================================================================
        // Error Handling
        // =====================================================================

        [Fact]
        public void Parse_InvalidYaml_ThrowsTyeYamlException()
        {
            using var parser = new DockerComposeParser("{ broken yaml [}");
            var ex = Assert.ThrowsAny<TyeYamlException>(() => parser.ParseConfigApplication());
            Assert.Contains("Unable to parse", ex.Message);
        }

        [Fact]
        public void Parse_EmptyDocument_Throws()
        {
            using var parser = new DockerComposeParser("");
            Assert.Throws<ArgumentOutOfRangeException>(() => parser.ParseConfigApplication());
        }

        [Fact]
        public void Parse_RootIsSequence_Throws()
        {
            using var parser = new DockerComposeParser("- item1\n- item2");
            var ex = Assert.ThrowsAny<TyeYamlException>(() => parser.ParseConfigApplication());
            Assert.Contains(CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Sequence.ToString()), ex.Message);
        }

        [Fact]
        public void Parse_RootIsScalar_Throws()
        {
            using var parser = new DockerComposeParser("justascalar");
            var ex = Assert.ThrowsAny<TyeYamlException>(() => parser.ParseConfigApplication());
            Assert.Contains(CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString()), ex.Message);
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
            Assert.NotNull(app.Name);
            Assert.NotEmpty(app.Name);
        }

        [Fact]
        public void Parse_SetsSourceFromFileInfo()
        {
            var file = Path.Combine(_tempDir, "docker-compose.yaml");
            File.WriteAllText(file, "version: '3'");
            var fileInfo = new FileInfo(file);

            using var parser = new DockerComposeParser(fileInfo);
            var app = parser.ParseConfigApplication();
            Assert.Equal(fileInfo.FullName, app.Source.FullName);
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
            Assert.Equal(3, app.Services.Count);

            var web = app.Services.First(s => s.Name == "web");
            Assert.Equal("nginx:alpine", web.Image);
            Assert.Equal(2, web.Bindings.Count);

            var api = app.Services.First(s => s.Name == "api");
            Assert.Equal("myapi:latest", api.Image);
            Assert.Single(api.Bindings);
            Assert.Equal(5000, api.Bindings.Single().Port);
            Assert.Equal(2, api.Configuration.Count);

            var db = app.Services.First(s => s.Name == "db");
            Assert.Equal("postgres:15", db.Image);
            Assert.Equal("secret", db.Configuration.First(c => c.Name == "POSTGRES_PASSWORD").Value);
        }
    }
}
