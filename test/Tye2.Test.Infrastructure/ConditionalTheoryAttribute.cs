// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using Xunit.v3;

namespace Tye2.Test.Infrastructure
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer(typeof(ConditionalTheoryDiscoverer))]
    public class ConditionalTheoryAttribute : TheoryAttribute
    {
    }
}
