// See the LICENSE file in the project root for more information.

namespace Tye2.Hosting.Model.V1
{
    public class V1Metric
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public V1MetricMetadata? Metadata { get; set; }
    }
}
