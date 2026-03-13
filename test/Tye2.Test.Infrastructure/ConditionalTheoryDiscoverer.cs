// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace Tye2.Test.Infrastructure
{
    internal class ConditionalTheoryDiscoverer : TheoryDiscoverer
    {
        protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            IXunitTestMethod testMethod,
            ITheoryAttribute theoryAttribute)
        {
            var skipReason = testMethod.EvaluateSkipConditions();
            return skipReason != null
                       ? new ValueTask<IReadOnlyCollection<IXunitTestCase>>(new IXunitTestCase[]
                         {
                             new SkippedTestCase(skipReason, testMethod, theoryAttribute)
                         })
                       : base.CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
        }
    }
}
