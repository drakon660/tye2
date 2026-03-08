// See the LICENSE file in the project root for more information.

using Xunit;

// We have numerous tests that manipulate machine-wide state (docker, ports, etc)
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
