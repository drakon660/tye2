// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Tye2.Test.Infrastructure
{
    public class SkippedTestCase : XunitTestCase
    {
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public SkippedTestCase() : base()
        {
        }

        public SkippedTestCase(
            string skipReason,
            IXunitTestMethod testMethod,
            IFactAttribute factAttribute)
            : base(
                testMethod,
                testMethod.GetDisplayName(factAttribute.DisplayName ?? testMethod.Method.Name, null, testMethod.TestMethodArguments, null),
                UniqueIDGenerator.ForTestCase(((ITestMethodMetadata)testMethod).UniqueID, null, testMethod.TestMethodArguments),
                factAttribute.Explicit,
                factAttribute.SkipExceptions,
                skipReason,
                factAttribute.SkipType,
                factAttribute.SkipUnless,
                factAttribute.SkipWhen,
                GetTraits(testMethod),
                testMethod.TestMethodArguments,
                factAttribute.SourceFilePath,
                factAttribute.SourceLineNumber,
                factAttribute.Timeout > 0 ? factAttribute.Timeout : null)
        {
        }

        private static Dictionary<string, HashSet<string>> GetTraits(IXunitTestMethod testMethod)
        {
            var traits = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var attributes = testMethod.Method.GetCustomAttributes(typeof(TraitAttribute), inherit: true).Cast<TraitAttribute>()
                .Concat(testMethod.TestClass.Class.GetCustomAttributes(typeof(TraitAttribute), inherit: true).Cast<TraitAttribute>())
                .Concat(testMethod.TestClass.TestCollection.TestAssembly.Assembly.GetCustomAttributes(typeof(TraitAttribute), inherit: true).Cast<TraitAttribute>());

            foreach (var attribute in attributes)
            {
                if (!traits.TryGetValue(attribute.Name, out var values))
                {
                    values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    traits[attribute.Name] = values;
                }

                values.Add(attribute.Value);
            }

            return traits;
        }
    }
}
