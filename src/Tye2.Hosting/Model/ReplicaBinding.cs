// See the LICENSE file in the project root for more information.

namespace Tye2.Hosting.Model
{
    public class ReplicaBinding
    {
        public int Port { get; set; }
        public int ExternalPort { get; set; }
        public string? Protocol { get; set; }
    }
}
