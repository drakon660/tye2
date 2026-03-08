// See the LICENSE file in the project root for more information.

namespace Tye2.Core
{
    public class ProbeBuilder
    {
        public HttpProberBuilder? Http { get; set; }
        public int InitialDelay { get; set; }
        public int Period { get; set; }
        public int Timeout { get; set; }
        public int SuccessThreshold { get; set; }
        public int FailureThreshold { get; set; }
    }
}
