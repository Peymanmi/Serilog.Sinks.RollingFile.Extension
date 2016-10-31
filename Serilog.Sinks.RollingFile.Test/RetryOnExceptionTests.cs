namespace Serilog.Sinks.RollingFile.Test
{
    using Extension.Aspects;
    using NUnit.Framework;
    using NUnit.Framework.Compatibility;
    using System;
    using System.Threading.Tasks;


    [TestFixture(Category = "Unit")]
    public class RetryOnExceptionTests
    {
        [Test]
        [Description("Call dummy method with throw exception should retry 5 times")]
        //[ExpectedException]
        public void RetryDummyMethodWithReturn()
        {
            var fakeObj = new FakeRetryClass();

            var result = fakeObj.DummyMethod(5, 10);

            Assert.AreEqual(fakeObj.NumberOfCall, 6);
            Assert.AreEqual(result, 10);
        }       

        [Test]
        [Description("Call dummy method with throw exception should retry 10 times and delay 100ms between each try")]
        public void RetryDummyMethodForeverShouldHasDelay()
        {
            var numberOfRetry = 10;
            var fakeObj = new FakeRetryClass();

            var sw = Stopwatch.StartNew();

            fakeObj.DummyForeverMethod(numberOfRetry);

            Assert.AreEqual(fakeObj.NumberOfCall, numberOfRetry);

            sw.Stop();

            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 1000);
        }
    }

    [Serializable]
    public class FakeRetryClass
    {
        public int NumberOfCall { get; set; }

        [RetryOnException(MaxRetries = 5)]
        public void DummyMethod()
        {
            this.NumberOfCall++;
            throw new Exception("It's a dummy method for testing retry feature");
        }

        [RetryOnException(MaxRetries = 5)]
        public int DummyMethod(int numberOfTry = 5, int expected = -1)
        {
            this.NumberOfCall++;

            if (NumberOfCall > numberOfTry)
                return expected;

            throw new Exception("It's a dummy method for testing retry feature");
        }        

        [RetryOnException(100, Forever = true)]
        public void DummyForeverMethod(int numberOfTry)
        {
            if (numberOfTry == NumberOfCall)
                return;

            this.NumberOfCall++;
            throw new Exception("It's a dummy method for testing retry feature");
        }
    }
}
