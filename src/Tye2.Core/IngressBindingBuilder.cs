// See the LICENSE file in the project root for more information.

namespace Tye2.Core
{
    public sealed class IngressBindingBuilder
    {
        public string? Name { get; set; }
        public int? Port { get; set; }
        public string? Protocol { get; set; } // HTTP or HTTPS
        public string? IPAddress { get; set; }
    }
}
