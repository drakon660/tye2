// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit.v3;

namespace Tye2.Test.Infrastructure
{
    public static class TestMethodExtensions
    {
        public static string EvaluateSkipConditions(this IXunitTestMethod testMethod)
        {
            var conditionAttributes = testMethod.Method.GetCustomAttributes(typeof(ITestCondition), inherit: true).Cast<ITestCondition>()
                .Concat(testMethod.TestClass.Class.GetCustomAttributes(typeof(ITestCondition), inherit: true).Cast<ITestCondition>())
                .Concat(testMethod.TestClass.TestCollection.TestAssembly.Assembly.GetCustomAttributes(typeof(ITestCondition), inherit: true).Cast<ITestCondition>());

            foreach (ITestCondition condition in conditionAttributes)
            {
                if (!condition.IsMet)
                {
                    return condition.SkipReason;
                }
            }

            return null!;
        }
    }
}
