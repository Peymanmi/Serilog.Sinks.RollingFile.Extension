namespace Serilog.Sinks.RollingFile.AcceptanceTest
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    using System.IO;

    [Binding]
    public class Setup
    {
        private static ILogger logger;

        static Setup()
        {
        }

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
            logger = new LoggerConfiguration()
                    .ReadFrom.AppSettings()
                    .Enrich.WithMachineName()
                    .Enrich.FromLogContext()
                    .CreateLogger();

            Log.Logger = logger;
            logger.Information("Logger has been initialized!");
        }
    }
}
