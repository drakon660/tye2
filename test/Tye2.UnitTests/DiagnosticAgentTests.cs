using System.IO;
using System.Linq;
using AwesomeAssertions;
using Tye2.Core;
using Tye2.Extensions;
using Xunit;

namespace Tye2.UnitTests
{
    public class DiagnosticAgentTests
    {
        private static DotnetProjectServiceBuilder CreateProject(string name = "web")
        {
            return new DotnetProjectServiceBuilder(name, new FileInfo("C:\\test\\web.csproj"), ServiceSource.Configuration)
            {
                AssemblyName = $"{name}.dll",
            };
        }

        [Fact]
        public void GetOrAddSidecar_AddsSidecarToProject()
        {
            var project = CreateProject();

            var sidecar = DiagnosticAgent.GetOrAddSidecar(project);

            project.Sidecars.Should().ContainSingle();
            project.Sidecars[0].Should().BeSameAs(sidecar);
        }

        [Fact]
        public void GetOrAddSidecar_SetsCorrectName()
        {
            var project = CreateProject();

            var sidecar = DiagnosticAgent.GetOrAddSidecar(project);

            sidecar.Name.Should().Be("tye-diag-agent");
        }

        [Fact]
        public void GetOrAddSidecar_SetsCorrectImage()
        {
            var project = CreateProject();

            var sidecar = DiagnosticAgent.GetOrAddSidecar(project);

            sidecar.ImageName.Should().Be("rynowak/tye-diag-agent");
            sidecar.ImageTag.Should().Be("0.1");
        }

        [Fact]
        public void GetOrAddSidecar_SetsKubernetesArg()
        {
            var project = CreateProject();

            var sidecar = DiagnosticAgent.GetOrAddSidecar(project);

            sidecar.Args.Should().Contain("--kubernetes=true");
        }

        [Fact]
        public void GetOrAddSidecar_SetsServiceArg()
        {
            var project = CreateProject("my-api");

            var sidecar = DiagnosticAgent.GetOrAddSidecar(project);

            sidecar.Args.Should().Contain("--service=my-api");
        }

        [Fact]
        public void GetOrAddSidecar_SetsAssemblyNameArg()
        {
            var project = CreateProject("web");
            project.AssemblyName = "MyWeb.dll";

            var sidecar = DiagnosticAgent.GetOrAddSidecar(project);

            sidecar.Args.Should().Contain("--assemblyName=MyWeb.dll");
        }

        [Fact]
        public void GetOrAddSidecar_EnablesRelocateDiagnostics()
        {
            var project = CreateProject();

            DiagnosticAgent.GetOrAddSidecar(project);

            project.RelocateDiagnosticsDomainSockets.Should().BeTrue();
        }

        [Fact]
        public void GetOrAddSidecar_CalledTwice_ReturnsSameSidecar()
        {
            var project = CreateProject();

            var first = DiagnosticAgent.GetOrAddSidecar(project);
            var second = DiagnosticAgent.GetOrAddSidecar(project);

            first.Should().BeSameAs(second);
            project.Sidecars.Should().HaveCount(1);
        }

        [Fact]
        public void GetOrAddSidecar_ExistingSidecarWithSameName_ReturnsExisting()
        {
            var project = CreateProject();
            var existing = new SidecarBuilder("tye-diag-agent", "other-image", "1.0");
            project.Sidecars.Add(existing);

            var result = DiagnosticAgent.GetOrAddSidecar(project);

            result.Should().BeSameAs(existing);
            project.Sidecars.Should().HaveCount(1);
        }

        [Fact]
        public void GetOrAddSidecar_DifferentSidecarExists_AddsNew()
        {
            var project = CreateProject();
            project.Sidecars.Add(new SidecarBuilder("other-sidecar", "some-image", "1.0"));

            var sidecar = DiagnosticAgent.GetOrAddSidecar(project);

            sidecar.Name.Should().Be("tye-diag-agent");
            project.Sidecars.Should().HaveCount(2);
        }
    }
}
