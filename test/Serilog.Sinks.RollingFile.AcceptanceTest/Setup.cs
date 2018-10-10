namespace Serilog.Sinks.RollingFile.AcceptanceTest
{
    using System;
    using System.IO;
    using System.Linq;

    using TechTalk.SpecFlow;

    [Binding]
    public class Setup
    {
        private static ILogger logger;

        [AfterTestRun]
        public static void After()
        {
            if (logger != null)
            {
                var logPath = @"C:\temp\logger\";
                var files = Directory.GetFiles(logPath).ToList();

                ((IDisposable)logger).Dispose();

                files.ForEach(f => File.Delete(f));
            }
        }

        [BeforeTestRun]
        public static void Before()
        {
            logger = new LoggerConfiguration().ReadFrom.AppSettings().Enrich.WithMachineName().Enrich.FromLogContext()
                .CreateLogger();

            Log.Logger = logger;
            logger.Information("Logger has been initialized!");
        }
    }
}