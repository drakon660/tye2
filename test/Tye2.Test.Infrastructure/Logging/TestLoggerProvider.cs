// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Tye2.Test.Infrastructure.Logging
{
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly ITestSink _sink;

        public TestLoggerProvider(ITestSink sink)
        {
            _sink = sink;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _sink, enabled: true);
        }

        public void Dispose()
        {
        }
    }
}
