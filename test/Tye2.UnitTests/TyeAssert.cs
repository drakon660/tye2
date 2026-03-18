using AwesomeAssertions;
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Tye2.Core.ConfigModel;
using Xunit;

namespace Tye2.Test.Infrastructure
{
    public static class TyeAssert
    {
        public static void Equal(ConfigApplication expected, ConfigApplication actual)
        {
            actual.Name.Should().Be(expected.Name);
            actual.Registry.Should().Be(expected.Registry);
            actual.Network.Should().Be(expected.Network);

            foreach (var ingress in actual.Ingress)
            {
                var otherIngress = expected
                    .Ingress
                    .Where(o => o.Name == ingress.Name)
                    .Single();
                otherIngress.Should().NotBeNull();
                ingress.Replicas.Should().Be(otherIngress.Replicas);

                foreach (var rule in ingress.Rules)
                {
                    var otherRule = otherIngress
                        .Rules
                        .Where(o => o.Path == rule.Path && o.Host == rule.Host && o.Service?.Equals(rule.Service, StringComparison.OrdinalIgnoreCase) == true)
                        .Single();
                    otherRule.Should().NotBeNull();
                }

                foreach (var binding in ingress.Bindings)
                {
                    var otherBinding = otherIngress
                        .Bindings
                        .Where(o => o.Name == binding.Name && o.Port == binding.Port && o.Protocol == binding.Protocol)
                        .Single();

                    otherBinding.Should().NotBeNull();
                }
                ingress.Tags.Should().BeEquivalentTo(otherIngress.Tags);
            }

            foreach (var service in actual.Services)
            {
                var otherService = expected
                    .Services
                    .Where(o => o.Name.Equals(service.Name, StringComparison.OrdinalIgnoreCase))
                    .Single();
                otherService.Should().NotBeNull();
                service.Args.Should().Be(otherService.Args);
                service.Build.Should().Be(otherService.Build);
                service.Executable.Should().Be(otherService.Executable);
                service.External.Should().Be(otherService.External);
                service.Image.Should().Be(otherService.Image);
                service.Project.Should().Be(otherService.Project);
                service.Replicas.Should().Be(otherService.Replicas);
                service.WorkingDirectory.Should().Be(otherService.WorkingDirectory);
                service.Tags.Should().BeEquivalentTo(otherService.Tags);

                foreach (var binding in service.Bindings)
                {
                    var otherBinding = otherService.Bindings
                                    .Where(o => o.Name == binding.Name
                                        && o.Port == binding.Port
                                        && o.Protocol == binding.Protocol
                                        && o.ConnectionString == binding.ConnectionString
                                        && o.ContainerPort == binding.ContainerPort
                                        && o.Host == binding.Host)
                                    .Single();

                    otherBinding.Should().NotBeNull();
                }

                foreach (var config in service.Configuration)
                {
                    var otherConfig = otherService.Configuration
                                    .Where(o => o.Name == config.Name
                                        && o.Value == config.Value)
                                    .Single();

                    otherConfig.Should().NotBeNull();
                }

                foreach (var volume in service.Volumes)
                {
                    var otherVolume = otherService.Volumes
                                   .Where(o => o.Name == volume.Name
                                       && o.Target == volume.Target
                                       && o.Source == volume.Source)
                                   .Single();
                    otherVolume.Should().NotBeNull();
                }
            }
        }
    }
}




