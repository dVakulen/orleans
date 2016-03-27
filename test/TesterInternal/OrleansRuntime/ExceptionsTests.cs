using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.Serialization;
using Orleans.TestingHost;
using Xunit;

namespace UnitTests.OrleansRuntime
{
    public class ExceptionsTests
    {
        public ExceptionsTests()
        {
            BufferPool.InitGlobalBufferPool(new MessagingConfiguration(false));
            SerializationManager.Initialize(false, null, false);
        }

        [Fact, TestCategory("Functional"), TestCategory("Serialization")]
        public void SerializationTests_Exception_DotNet()
        {
            var original = new InvalidSchedulingContextException("InvalidSchedulingContext");
            var output = TestingUtils.RoundTripDotNetSerializer(original);
            Assert.AreEqual(original.Message, output.Message);
        }

        [Fact, TestCategory("Functional"), TestCategory("Serialization")]
        public void SerializationTests_Exception_Orleans()
        {
            var original = new InvalidSchedulingContextException("InvalidSchedulingContext");
            var output = SerializationManager.RoundTripSerializationForTesting(original);
            Assert.AreEqual(original.Message, output.Message);
        }
    }
}
