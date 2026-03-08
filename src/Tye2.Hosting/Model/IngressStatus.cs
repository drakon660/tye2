// See the LICENSE file in the project root for more information.

namespace Tye2.Hosting.Model
{
    public class IngressStatus : ReplicaStatus
    {
        public IngressStatus(Service service, string name) : base(service, name)
        {
        }

    }
}
