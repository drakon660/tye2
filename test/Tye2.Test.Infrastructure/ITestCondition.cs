// See the LICENSE file in the project root for more information.

namespace Tye2.Test.Infrastructure
{
    public interface ITestCondition
    {
        bool IsMet { get; }

        string SkipReason { get; }
    }
}
