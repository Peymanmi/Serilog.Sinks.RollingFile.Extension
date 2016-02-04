namespace Serilog.Sinks.RollingFile.AcceptanceTest
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    using System.IO;

    [Binding]
    public class Setup
    {

        static Setup()
        {
        }

        [AfterTestRun]
        public static void After()
        {
            var logger = Log.Logger as IDisposable;
            if (logger != null)
            {
                logger.Dispose();
                var logPath = @"C:\temp\logger\";
                var files = Directory.GetFiles(logPath).ToList();
                files.ForEach(f => File.Delete(f));
            }
        }

        [BeforeTestRun]
        public static void Before()
        {
            var logger = new LoggerConfiguration()
                    .ReadFrom.AppSettings()
                    .Enrich.WithMachineName()
                    .Enrich.FromLogContext()
                    .CreateLogger();

            Log.Logger = logger;
            logger.Information("Logger has been initialized!");
        }
    }
}
