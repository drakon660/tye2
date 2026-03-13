// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using Xunit.Sdk;

namespace Tye2.Test.Infrastructure
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Tye2.Test.Infrastructure." + nameof(ConditionalTheoryDiscoverer), "Tye2.Test.Infrastructure")]
    public class ConditionalTheoryAttribute : TheoryAttribute
    {
    }
}
