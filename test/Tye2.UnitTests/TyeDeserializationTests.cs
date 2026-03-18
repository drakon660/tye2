using AwesomeAssertions;
using System;
using System.IO;
using System.Linq;
using Tye2.Core;
using Tye2.Core.ConfigModel;
using Tye2.Core.Serialization;
using Tye2.Test.Infrastructure;
using Xunit;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Tye2.UnitTests
{
    public class TyeDeserializationTests
    {
        private IDeserializer _deserializer;

        public TyeDeserializationTests()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        [Fact]
        public void ComprehensionalTest()
        {
            var input = @"
name: apps-with-ingress
registry: myregistry
extensions:
  - name: dapr
ingress:
  - name: ingress
    bindings:
      - port: 8080
        protocol: http
        name: foo
    rules:
      - path: /A
        service: appA
      - path: /B
        service: appB
      - host: a.example.com
        service: appA
      - host: b.example.com
        service: appB
    replicas: 2
    tags:
      - tagA
      - tagC
services:
  - name: appA
    project: ApplicationA/ApplicationA.csproj
    buildProperties:
    - name: Configuration
    - value: Debug
    replicas: 2
    tags:
      - tagA
      - tagC
    external: false
    image: abc
    build: false
    executable: test.exe
    workingDirectory: ApplicationA/
    args: a b c
    env:
    - name: POSTGRES_PASSWORD
      value: ""test""
    - name: POSTGRES_PASSWORD2
      value: ""test2""
    volumes:
    - name: volume
      source: /data
      target: /data
    bindings:
    - name: test
      port: 4444
      connectionString: asdf
      containerPort: 80
      host: localhost
      protocol: http
      routes:
      - /swagger
      - /graphql
  - name: appB
    project: ApplicationB/ApplicationB.csproj
    replicas: 2
    tags:
      - tagB
      - tagD";

            using var parser = new YamlParser(input);
            var app = parser.ParseConfigApplication();
        }

        [Fact]
        public void IngressIsSetCorrectly()
        {
            var input = @"
ingress:
  - name: ingress
    bindings:
      - port: 8080
        protocol: http
        name: foo
    rules:
      - path: /A
        service: appA
      - path: /B
        service: appB
      - host: a.example.com
        service: appA
      - host: b.example.com
        service: appB
    replicas: 2";

            using var parser = new YamlParser(input);
            var actual = parser.ParseConfigApplication();

            var expected = _deserializer.Deserialize<ConfigApplication>(new StringReader(input));

            TyeAssert.Equal(expected, actual);
        }

        [Fact]
        public void ServicesSetCorrectly()
        {
            var input = @"
services:
  - name: appA
    project: ApplicationA/ApplicationA.csproj
    replicas: 2
    tags:
      - A
      - B
    external: false
    image: abc
    build: false
    executable: test.exe
    workingDirectory: ApplicationA/
    args: a b c
    env:
    - name: POSTGRES_PASSWORD
      value: ""test""
    - name: POSTGRES_PASSWORD2
      value: ""test2""
    volumes:
    - name: volume
      source: /data
      target: /data
    bindings:
    - name: test
      port: 4444
      connectionString: asdf
      containerPort: 80
      host: localhost
      protocol: http
      routes:
      - /swagger
      - /graphql
  - name: appB
    project: ApplicationB/ApplicationB.csproj
    replicas: 2
    tags:
      - tC
      - tD";
            using var parser = new YamlParser(input);
            var actual = parser.ParseConfigApplication();

            var expected = _deserializer.Deserialize<ConfigApplication>(new StringReader(input));

            TyeAssert.Equal(expected, actual);
        }

        [Fact]
        public void ExtensionsTest()
        {
            var input = @"
extensions:
  - name: dapr";
            using var parser = new YamlParser(input);

            var app = parser.ParseConfigApplication();

            app.Extensions.Single()["name"].Should().Be("dapr");

            var expected = _deserializer.Deserialize<ConfigApplication>(new StringReader(input));

            app.Extensions.Count.Should().Be(expected.Extensions.Count);
        }

        [Fact]
        public void NetworkTest()
        {
            var input = @"
network: test-network";
            using var parser = new YamlParser(input);

            var app = parser.ParseConfigApplication();

            app.Network.Should().Be("test-network");

            var expected = _deserializer.Deserialize<ConfigApplication>(new StringReader(input));

            app.Network.Should().Be(expected.Network);
        }

        [Fact]
        public void VotingTest()
        {
            using var parser = new YamlParser(
@"name: VotingSample
registry: myregistry
services:
- name: vote
  project: vote/vote.csproj
- name: redis
  image: redis
  bindings:
    - port: 6379
- name: worker
  project: worker/worker.csproj
- name: postgres
  image:  postgres
  env:
    - name: POSTGRES_PASSWORD
      value: ""test""
  bindings:
    - port: 5432
- name: results
  project: results/results.csproj");
            var app = parser.ParseConfigApplication();
        }

        [Theory]
        [InlineData("env")]
        [InlineData("configuration")]
        public void EnvSimpleSyntaxTest(string rootKeyName)
        {
            using var parser = new YamlParser(
@$"
services:
    - {rootKeyName}:
        - name: env1
          value: value1
        - name: env2
          value: value2
        - name: env3
          value: ""long string""
        - name: env4
          value:
");

            var app = parser.ParseConfigApplication();
            var serviceConfig = app.Services.First().Configuration;

            serviceConfig.Count.Should().Be(4);
            serviceConfig.Where(env => env.Name == "env1").First().Value.Should().Be("value1");
            serviceConfig.Where(env => env.Name == "env2").First().Value.Should().Be("value2");
            serviceConfig.Where(env => env.Name == "env3").First().Value.Should().Be("long string");
            serviceConfig.Where(env => env.Name == "env4").First().Value.Should().Be(string.Empty);
        }

        [Fact]
        public void EnvCompactSyntaxTest()
        {
            using var parser = new YamlParser(
@"
services:
    - env:
        - env1=value1
        - env2=value2
        - env3 = value3
        - env4 = ""long string""
        - name: env5
          value: value5
        - env6 =
");

            var app = parser.ParseConfigApplication();
            var serviceConfig = app.Services.First().Configuration;

            serviceConfig.Count.Should().Be(6);
            serviceConfig.Where(env => env.Name == "env1").First().Value.Should().Be("value1");
            serviceConfig.Where(env => env.Name == "env2").First().Value.Should().Be("value2");
            serviceConfig.Where(env => env.Name == "env3").First().Value.Should().Be("value3");
            serviceConfig.Where(env => env.Name == "env4").First().Value.Should().Be("long string");
            serviceConfig.Where(env => env.Name == "env5").First().Value.Should().Be("value5");
            serviceConfig.Where(env => env.Name == "env6").First().Value.Should().Be(string.Empty);
        }

        [Fact]
        public void EnvTakeValueFromEnvironmentTest()
        {
            using var parser = new YamlParser(
@"
services:
    - env:
        - env1
        - name: env2
        - env3
");

            Environment.SetEnvironmentVariable("env1", "value1");
            Environment.SetEnvironmentVariable("env2", "value2");

            var app = parser.ParseConfigApplication();
            var serviceConfig = app.Services.First().Configuration;

            serviceConfig.Count.Should().Be(3);
            serviceConfig.Where(env => env.Name == "env1").First().Value.Should().Be("value1");
            serviceConfig.Where(env => env.Name == "env2").First().Value.Should().Be("value2");
            serviceConfig.Where(env => env.Name == "env3").First().Value.Should().Be(string.Empty);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EnvFromFile(bool setWorkingDirectory)
        {
            var subDirectory = setWorkingDirectory ? string.Empty : "testassets/";
            using var parser = new YamlParser(
@$"
services:
    - env_file:
        - ./{subDirectory}envfile_a.env
        - ./{subDirectory}envfile_b.env
", setWorkingDirectory ? new FileInfo(Path.Join(Directory.GetCurrentDirectory(), "testassets", "tye.yaml")) : null);


            var app = parser.ParseConfigApplication();
            var serviceConfig = app.Services.First().Configuration;

            serviceConfig.Count.Should().Be(3);
        }

        [Fact]
        public void PathNotFound_ThrowException()
        {
            using var parser = new YamlParser(
@"
services:
    - env_file:
        - ./envfile_c.env
", new FileInfo(Path.Join(Directory.GetCurrentDirectory(), "testassets", "tye.yaml")));

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatPathNotFound(Path.Join(Directory.GetCurrentDirectory(), "testassets", "envfile_c.env")));
        }

        [Fact]
        public void UnrecognizedConfigApplicationField_ThrowException()
        {
            using var parser = new YamlParser("asdf: 123");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("asdf"));
        }

        [Fact]
        public void Replicas_MustBeInteger()
        {
            using var parser = new YamlParser(
@"services:
- name: app
  replicas: asdf");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBeAnInteger("replicas"));
        }

        [Fact]
        public void Replicas_MustBePositive()
        {
            using var parser = new YamlParser(
@"services:
- name: app
  replicas: -1");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBePositive("replicas"));
        }

        [Fact]
        public void Name_MustBeScalar()
        {
            using var parser = new YamlParser(
@"name:
- a: b");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlScalar("name"));
        }


        [Fact]
        public void YamlIsCaseSensitive()
        {
            using var parser = new YamlParser(
@"Name: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("Name"));
        }

        [Fact]
        public void Registry_MustBeScalar()
        {
            using var parser = new YamlParser(
@"registry:
- a: b");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlScalar("registry"));
        }

        [Fact]
        public void Ingress_MustBeSequence()
        {
            using var parser = new YamlParser(
@"ingress: a");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("ingress"));
        }

        [Fact]
        public void Services_MustBeSequence()
        {
            using var parser = new YamlParser(
@"services: a");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("services"));
        }

        [Fact]
        public void ConfigApplication_MustBeMappings()
        {
            using var parser = new YamlParser(
@"- name: app
  replicas: -1");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Sequence.ToString()));
        }

        [Fact]
        public void Services_MustBeMappings()
        {
            using var parser = new YamlParser(
@"services:
  - name");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString()));
        }

        [Fact]
        public void Ingress_MustBeMappings()
        {
            using var parser = new YamlParser(
@"ingress:
  - name");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString()));
        }

        [Fact]
        public void Ingress_Replicas_MustBeInteger()
        {
            using var parser = new YamlParser(
@"ingress:
  - replicas: asdf");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBeAnInteger("replicas"));
        }

        [Fact]
        public void Ingress_Replicas_MustBePositive()
        {
            using var parser = new YamlParser(
@"ingress:
  - replicas: -1");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBePositive("replicas"));
        }

        [Fact]
        public void Ingress_UnrecognizedKey()
        {
            using var parser = new YamlParser(
@"ingress:
  - abc: abc");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("abc"));
        }

        [Fact]
        public void Ingress_Rules_MustSequence()
        {
            using var parser = new YamlParser(
@"ingress:
  - rules: abc");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("rules"));
        }

        [Fact]
        public void Ingress_Rules_MustBeMappings()
        {
            using var parser = new YamlParser(
@"ingress:
  - rules:
    - abc");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString()));
        }

        [Fact]
        public void Ingress_Bindings_MustBeMappings()
        {
            using var parser = new YamlParser(
@"ingress:
  - bindings:
    - abc");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnexpectedType(YamlNodeType.Mapping.ToString(), YamlNodeType.Scalar.ToString()));
        }

        [Fact]
        public void Ingress_RulesMapping_UnrecognizedKey()
        {
            using var parser = new YamlParser(
@"ingress:
  - rules:
    - abc: 123");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("abc"));
        }

        [Fact]
        public void Ingress_Bindings_MustSequence()
        {
            using var parser = new YamlParser(
@"ingress:
  - bindings: abc");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("bindings"));
        }

        [Fact]
        public void Ingress_Bindings_Port_MustBeInteger()
        {
            using var parser = new YamlParser(
@"ingress:
  - name: ingress
    bindings:
      - port: abc
        protocol: http
        name: foo");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBeAnInteger("port"));
        }

        [Fact]
        public void Ingress_Bindings_UnrecognizedKey()
        {
            using var parser = new YamlParser(
@"ingress:
  - name: ingress
    bindings:
      - abc: abc");
            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("abc"));
        }


        [Fact]
        public void Ingress_Tags_MustBeSequence()
        {
            using var parser = new YamlParser(
@"ingress:
  - name: ingress
    tags: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("tags"));
        }

        [Fact]
        public void Ingress_Tags_SetCorrectly()
        {
            var input = @"
ingress:
  - name: ingress
    tags:
      - tagA
      - with space
      - ""C.X""
";
            using var parser = new YamlParser(input);
            var actual = parser.ParseConfigApplication();

            var expected = _deserializer.Deserialize<ConfigApplication>(new StringReader(input));

            TyeAssert.Equal(expected, actual);
        }

        [Fact]
        public void Services_External_MustBeBool()
        {
            using var parser = new YamlParser(
@"services:
  - name: ingress
    external: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBeABoolean("external"));
        }

        [Fact]
        public void Services_Build_MustBeBool()
        {
            using var parser = new YamlParser(
@"services:
  - name: ingress
    build: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBeABoolean("build"));
        }

        [Fact]
        public void Services_Bindings_MustBeSequence()
        {
            using var parser = new YamlParser(
@"services:
  - name: ingress
    bindings: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("bindings"));
        }


        [Fact]
        public void Services_Volumes_MustBeSequence()
        {
            using var parser = new YamlParser(
@"services:
  - name: ingress
    volumes: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("volumes"));
        }

        [Fact]
        public void Services_Env_MustBeSequence()
        {
            using var parser = new YamlParser(
@"services:
  - name: ingress
    env: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("env"));
        }

        [Fact]
        public void Services_Tags_MustBeSequence()
        {
            using var parser = new YamlParser(
@"services:
  - name: ingress
    tags: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("tags"));
        }

        [Fact]
        public void Services_Tags_SetCorrectly()
        {
            var input = @"
services:
  - name: ingress
    tags:
      - tagA
      - with space
      - ""C.X""
";
            using var parser = new YamlParser(input);
            var actual = parser.ParseConfigApplication();

            var expected = _deserializer.Deserialize<ConfigApplication>(new StringReader(input));

            TyeAssert.Equal(expected, actual);
        }

        [Fact]
        public void Services_UnrecognizedKey()
        {
            using var parser = new YamlParser(
@"services:
  - name: ingress
    xyz: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("xyz"));
        }

        [Theory]
        [InlineData("liveness")]
        [InlineData("readiness")]
        public void Probe_UnrecognizedKey(string probe)
        {
            using var parser = new YamlParser($@"
services:
    - name: sample
      {probe}:
        something: something");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("something"));
        }

        [Theory]
        [InlineData("initialDelay")]
        [InlineData("period")]
        [InlineData("timeout")]
        [InlineData("successThreshold")]
        [InlineData("failureThreshold")]
        public void Probe_ScalarFields_MustBeInteger(string field)
        {
            using var parser = new YamlParser($@"
services:
    - name: sample
      liveness:
        {field}: 3.5");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBeAnInteger(field));
        }

        [Theory]
        [InlineData("initialDelay")]
        public void Probe_ScalarFields_MustBePositive(string field)
        {
            using var parser = new YamlParser($@"
services:
    - name: sample
      liveness:
        {field}: -1");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBePositive(field));
        }

        [Theory]
        [InlineData("period")]
        [InlineData("timeout")]
        [InlineData("successThreshold")]
        [InlineData("failureThreshold")]
        public void Probe_ScalarFields_MustBeGreaterThanZero(string field)
        {
            using var parser = new YamlParser($@"
services:
    - name: sample
      liveness:
        {field}: 0");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBeGreaterThanZero(field));
        }

        [Fact]
        public void Probe_HttpProber_UnrecognizedKey()
        {
            using var parser = new YamlParser(@"
services:
    - name: sample
      liveness:
        http:
            something: something");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("something"));
        }

        [Fact]
        public void Probe_HttpProber_PortMustBeScalar()
        {
            using var parser = new YamlParser($@"
services:
    - name: sample
      liveness:
        http:
            port: 3.5");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatMustBeAnInteger("port"));
        }

        [Fact]
        public void Probe_HttpProber_HeadersMustBeSequence()
        {
            using var parser = new YamlParser(@"
services:
    - name: sample
      liveness:
        http:
            headers: abc");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatExpectedYamlSequence("headers"));
        }

        [Fact]
        public void Probe_HttpProber_Headers_UnrecognizedKey()
        {
            using var parser = new YamlParser(@"
services:
    - name: sample
      liveness:
        http:
            headers:
                - name: header1
                  something: something");

            var exception = ((Action)(() => parser.ParseConfigApplication())).Should().Throw<TyeYamlException>().Which;
            exception.Message.Should().Contain(CoreStrings.FormatUnrecognizedKey("something"));
        }
    }
}




