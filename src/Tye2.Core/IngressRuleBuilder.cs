// See the LICENSE file in the project root for more information.

namespace Tye2.Core
{
    public sealed class IngressRuleBuilder
    {
        public string? Path { get; set; }
        public string? Host { get; set; }
        public bool PreservePath { get; set; }

        public string Service { get; set; } = default!;
    }
}
