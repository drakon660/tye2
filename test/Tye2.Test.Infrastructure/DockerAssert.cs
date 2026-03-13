// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tye2.Core;
using Xunit;
using Xunit.Sdk;

namespace Tye2.Test.Infrastructure
{
    public static class DockerAssert
    {
        private const string ManagedLabelKey = "tye2.managed";
        private const string ManagedLabelValue = "true";
        private const string ContextLabelKey = "tye2.context";

        // Repository is the "registry/image" format. Yeah Docker uses that term for it, and it's
        // weird and confusing.
        public static async Task AssertImageExistsAsync(ITestOutputHelper output, string repository)
        {
            var builder = new StringBuilder();

            output.WriteLine($"> docker images \"{repository}\" --format \"{{{{.Repository}}}}\"");
            var exitCode = await ContainerEngine.Default.ExecuteAsync(
                $"images \"{repository}\" --format \"{{{{.Repository}}}}\"",
                stdOut: OnOutput,
                stdErr: OnOutput);
            if (exitCode != 0)
            {
                throw new XunitException($"Running `docker images \"{repository}\"` failed." + Environment.NewLine + builder.ToString());
            }

            var lines = builder.ToString().Split(new[] { '\r', '\n', }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Any(line => line == repository ||
                          line == $"localhost/{repository}")) // podman format.
            {
                return;
            }

            throw new XunitException($"Image '{repository}' was not found in {builder}." );

            void OnOutput(string text)
            {
                builder.AppendLine(text);
                output.WriteLine(text);
            }
        }

        public static async Task DeleteDockerImagesAsync(ITestOutputHelper output, string repository)
        {
            var ids = await ListDockerImagesIdsAsync(output, repository);

            var builder = new StringBuilder();

            foreach (var id in ids)
            {
                output.WriteLine($"> docker rmi \"{id}\" --force");
                var exitCode = await ContainerEngine.Default.ExecuteAsync(
                    $"rmi \"{id}\" --force",
                    stdOut: OnOutput,
                    stdErr: OnOutput);
                if (exitCode != 0)
                {
                    throw new XunitException($"Running `docker rmi \"{id}\" --force` failed." + Environment.NewLine + builder.ToString());
                }

                builder.Clear();
            }

            void OnOutput(string text)
            {
                builder.AppendLine(text);
                output.WriteLine(text);
            }
        }

        public static async Task<string[]> GetRunningContainersIdsAsync(ITestOutputHelper output)
        {
            var builder = new StringBuilder();

            output.WriteLine("> docker ps --format \"{{.ID}}\"");
            var exitCode = await ContainerEngine.Default.ExecuteAsync(
                "ps --format \"{{.ID}}\"",
                stdOut: OnOutput,
                stdErr: OnOutput);
            if (exitCode != 0)
            {
                throw new XunitException($"Running `docker ps` failed." + Environment.NewLine + builder.ToString());
            }

            var lines = builder.ToString().Split(new[] { '\r', '\n', }, StringSplitOptions.RemoveEmptyEntries);
            return lines;

            void OnOutput(string text)
            {
                builder.AppendLine(text);
                output.WriteLine(text);
            }
        }

        // Best-effort cleanup used in test finally blocks; does not throw on cleanup failures.
        public static async Task CleanupManagedResourcesAsync(ITestOutputHelper output, string contextDirectory = null)
        {
            if (!ContainerEngine.Default.IsUsable(out _))
            {
                return;
            }

            var contextFilter = string.Empty;
            if (!string.IsNullOrWhiteSpace(contextDirectory))
            {
                var contextLabel = CreateContextLabel(contextDirectory);
                contextFilter = $" --filter \"label={ContextLabelKey}={contextLabel}\"";
            }

            await RemoveByQueryAsync(output,
                $"ps -aq --filter \"label={ManagedLabelKey}={ManagedLabelValue}\"{contextFilter}",
                id => $"rm -f {id}");

            await RemoveByQueryAsync(output,
                $"network ls -q --filter \"label={ManagedLabelKey}={ManagedLabelValue}\"{contextFilter}",
                id => $"network rm {id}");
        }

        private static async Task RemoveByQueryAsync(ITestOutputHelper output, string listCommand, Func<string, string> removeCommandFactory)
        {
            var listingOutput = new StringBuilder();
            output.WriteLine($"> docker {listCommand}");
            var listExitCode = await ContainerEngine.Default.ExecuteAsync(
                listCommand,
                stdOut: data =>
                {
                    listingOutput.AppendLine(data);
                    output.WriteLine(data);
                },
                stdErr: data => output.WriteLine(data));

            if (listExitCode != 0)
            {
                output.WriteLine($"docker {listCommand} failed with exit code {listExitCode}; skipping cleanup for this resource type.");
                return;
            }

            var ids = listingOutput
                .ToString()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var id in ids)
            {
                var removeCommand = removeCommandFactory(id);
                output.WriteLine($"> docker {removeCommand}");
                var removeExitCode = await ContainerEngine.Default.ExecuteAsync(
                    removeCommand,
                    stdOut: output.WriteLine,
                    stdErr: output.WriteLine);

                if (removeExitCode != 0)
                {
                    output.WriteLine($"docker {removeCommand} failed with exit code {removeExitCode}; continuing cleanup.");
                }
            }
        }

        private static async Task<string[]> ListDockerImagesIdsAsync(ITestOutputHelper output, string repository)
        {
            // docker images -q '{repository}' returns just the ID of the image (one per line)
            // It does not fail if there are no matches, just returns empty output.

            var builder = new StringBuilder();

            output.WriteLine($"> docker images -q \"{repository}\"");
            var exitCode = await ContainerEngine.Default.ExecuteAsync(
                $"images -q \"{repository}\"",
                stdOut: OnOutput,
                stdErr: OnOutput);
            if (exitCode != 0)
            {
                throw new XunitException($"Running `docker images -q \"{repository}\"` failed." + Environment.NewLine + builder.ToString());
            }

            var lines = builder.ToString().Split(new[] { '\r', '\n', }, StringSplitOptions.RemoveEmptyEntries);
            return lines;

            void OnOutput(string text)
            {
                builder.AppendLine(text);
                output.WriteLine(text);
            }
        }

        private static string CreateContextLabel(string contextDirectory)
        {
            var normalized = Path.GetFullPath(contextDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToLowerInvariant();

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash).Substring(0, 16).ToLowerInvariant();
        }
    }
}
