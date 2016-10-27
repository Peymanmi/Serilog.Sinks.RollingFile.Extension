namespace Serilog.Sinks.RollingFile.Test
{
    using NUnit.Framework;
    using Events;
    using Support;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Extension;
    using Formatting.Compact;

    [Category("Unit")]
    public class TestFormatterTest
    {
        [Test]
        public void ShouldCreateInstanceOfLogger()
        {
            var loggerConfiguration = new LoggerConfiguration();

            loggerConfiguration.WriteTo.SizeRollingFile(new CompactJsonFormatter(), "c:\\logs\\Log-{{Date}}.json", retainedFileDurationLimit: TimeSpan.FromDays(3));

            var logger = loggerConfiguration.CreateLogger();

            Assert.IsNotNull(logger);
        }
    }
}
