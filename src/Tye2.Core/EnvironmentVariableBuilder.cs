// See the LICENSE file in the project root for more information.

namespace Tye2.Core
{
    public sealed class EnvironmentVariableBuilder
    {
        public EnvironmentVariableBuilder(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string? Value { get; set; }

        public EnvironmentVariableSourceBuilder? Source { get; set; }
    }
}
