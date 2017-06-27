using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests
{
    public class PerformanceTests
    {
        private ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }
    }
}