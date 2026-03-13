// See the LICENSE file in the project root for more information.

using Xunit.Sdk;
using Xunit.v3;

// Do not change this namespace without changing the usage in ConditionalFactAttribute
namespace Tye2.Test.Infrastructure
{
    internal class ConditionalFactDiscoverer : FactDiscoverer
    {
        protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, IFactAttribute factAttribute)
        {
            var skipReason = testMethod.EvaluateSkipConditions();
            return skipReason != null
                ? new SkippedTestCase(skipReason, testMethod, factAttribute)
                : base.CreateTestCase(discoveryOptions, testMethod, factAttribute);
        }
    }
}
