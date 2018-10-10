namespace Serilog.Sinks.RollingFile.Test
{
    using System;

    using NUnit.Framework;

    using Serilog.Formatting.Compact;
    using Serilog.Sinks.RollingFile.Extension;

    [Category("Unit")]
    public class TestFormatterTest
    {
        [Test]
        public void ShouldCreateInstanceOfLogger()
        {
            var loggerConfiguration = new LoggerConfiguration();

            loggerConfiguration.WriteTo.SizeRollingFile(
                new CompactJsonFormatter(),
                "c:\\logs\\Log-{{Date}}.json",
                TimeSpan.FromDays(3));

            var logger = loggerConfiguration.CreateLogger();

            Assert.IsNotNull(logger);
        }
    }
}