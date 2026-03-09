using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Core.ConfigModel;
using Tye2.Core.Serialization;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;


namespace Tye2.UnitTests
{
    public partial class YamlConfigParserTests
    {
        // =====================================================================
        // ConfigServiceParser Tests — Scalar Fields
        // =====================================================================

        [Fact]
        public void Service_Name_ConvertedToLowercase()
        {
            using var parser = new YamlParser(@"
services:
  - name: MyService
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Name.Should().Be("myservice");
        }

        [Fact]
        public void Service_AllScalarFields_ParsedCorrectly()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    image: myimage:latest
    dockerFile: ./Dockerfile
    dockerFileContext: ./context
    project: MyApp/MyApp.csproj
    include: other.yaml
    repository: https://github.com/repo
    build: false
    executable: myapp.exe
    workingDirectory: ./app
    args: --port 8080
    replicas: 3
    external: true
    azureFunction: ./func
    pathToFunc: /usr/bin/func
    cloneDirectory: ./clone
");
            var app = parser.ParseConfigApplication();
            var svc = app.Services.Single();
            svc.Name.Should().Be("svc");
            svc.Image.Should().Be("myimage:latest");
            svc.DockerFile.Should().Be("./Dockerfile");
            svc.DockerFileContext.Should().Be("./context");
            svc.Project.Should().Be("MyApp/MyApp.csproj");
            svc.Include.Should().Be("other.yaml");
            svc.Repository.Should().Be("https://github.com/repo");
            svc.Build.Should().BeFalse();
            svc.Executable.Should().Be("myapp.exe");
            svc.WorkingDirectory.Should().Be("./app");
            svc.Args.Should().Be("--port 8080");
            svc.Replicas.Should().Be(3);
            svc.External.Should().BeTrue();
            svc.AzureFunction.Should().Be("./func");
            svc.FuncExecutable.Should().Be("/usr/bin/func");
            svc.CloneDirectory.Should().Be("./clone");
        }

        [Fact]
        public void Service_Replicas_ZeroIsValid()
        {
            using var parser = new YamlParser(@"
services:
  - name: svc
    replicas: 0
");
            var app = parser.ParseConfigApplication();
            app.Services.Single().Replicas.Should().Be(0);
        }

    }
}
