namespace Serilog.Sinks.RollingFile.Test
{
    using NUnit.Framework;
    using Events;
    using Support;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Extension;

    [Category("Unit")]
    public class RollingFileSinkTests
    {
        [SetUp]
        public void RunBeforeAnyTest()
        {

        }

        [Test]
        public void LogEventsAreEmittedToTheFileNamedAccordingToTheEventTimestamp()
        {
            TestRollingEventSequence(Some.InformationEvent());
        }

        [Test]
        public void WhenTheDateChangesTheCorrectFileIsWritten()
        {
            var e1 = Some.InformationEvent();
            var e2 = Some.InformationEvent(e1.Timestamp.AddDays(1));
            TestRollingEventSequence(e1, e2);
        }        

        [Test]
        public void IfTheLogFolderDoesNotExistItWillBeCreated()
        {
            var fileName = Some.String() + "-{Date}.txt";
            var temp = Some.TempFolderPath();
            var folder = Path.Combine(temp, Guid.NewGuid().ToString());
            var pathFormat = Path.Combine(folder, fileName);

            ILogger log = null;

            try
            {
                log = new LoggerConfiguration()
                    .WriteTo.SizeRollingFile(pathFormat, retainedFileDurationLimit: TimeSpan.FromSeconds(180))
                    .CreateLogger();

                log.Write(Some.InformationEvent());

                Assert.True(Directory.Exists(folder));
            }
            finally
            {
                var disposable = (IDisposable)log;
                if (disposable != null) disposable.Dispose();
                Directory.Delete(temp, true);
            }
        }

        static void TestRollingEventSequence(params LogEvent[] events)
        {
            TestRollingEventSequence(events, null, f => { });
        }

        static void TestRollingEventSequence(IEnumerable<LogEvent> events, int? retainedFiles, Action<IList<string>> verifyWritten)
        {
            var fileName = Some.String() + "-{Date}.txt";
            var folder = Some.TempFolderPath();
            var pathFormat = Path.Combine(folder, fileName);

            var log = new LoggerConfiguration()
                .WriteTo.SizeRollingFile(pathFormat, retainedFileDurationLimit: TimeSpan.FromSeconds(180))
                .CreateLogger();

            var verified = new List<string>();

            try
            {
                foreach (var @event in events)
                {                    
                    log.Write(@event);

                    var expected = pathFormat.Replace("{Date}", DateTime.UtcNow.ToString("yyyyMMdd") + "_0001");                    
                    Assert.True(File.Exists(expected));

                    verified.Add(expected);
                }
            }
            finally
            {
                ((IDisposable)log).Dispose();
                verifyWritten(verified);
                Directory.Delete(folder, true);
            }
        }
    }
}
