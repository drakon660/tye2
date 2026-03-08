// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Tye2.Hosting.Infrastructure
{
    internal class IngressHostMetadata
    {
        public IngressHostMetadata(params string[] hosts)
        {
            Hosts = new List<string>(hosts).AsReadOnly();
        }

        public IReadOnlyList<string> Hosts { get; }
    }
}
