
// See the LICENSE file in the project root for more information.

using System;

namespace Tye2.Test.Infrastructure
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class TestFrameworkFileLoggerAttribute : TestOutputDirectoryAttribute
    {
        public TestFrameworkFileLoggerAttribute(string preserveExistingLogsInOutput, string tfm, string baseDirectory = null)
            : base(preserveExistingLogsInOutput, tfm, baseDirectory)
        {
        }
    }
}
