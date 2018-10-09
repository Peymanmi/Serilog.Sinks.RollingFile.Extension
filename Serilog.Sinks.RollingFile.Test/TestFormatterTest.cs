namespace Serilog.Sinks.RollingFile.Test
{
    using System;

    using Extension;

    using Formatting.Compact;

    using NUnit.Framework;

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
