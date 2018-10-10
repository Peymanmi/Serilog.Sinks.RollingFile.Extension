using System;

namespace Serilog.Sinks.RollingFile.IntegrationTest
{
    using System.IO;

    using Microsoft.Extensions.Configuration;

    using Serilog.Debugging;

    public class Program
    {
        static void Main(string[] args)
        {
            SelfLog.Enable(Console.Out);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var dir = new DirectoryInfo("C:\\temp");
            foreach (var fileInfo in dir.GetFiles())
            {
                fileInfo.Delete();
            }

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            for (var i = 0; i < 1000000; ++i)
            {
                Log.Information("Hello, file logger!");
            }

            Log.CloseAndFlush();

            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");

            foreach (var fileInfo in dir.GetFiles())
            {
                Console.WriteLine($"Size - {fileInfo.Name}: {fileInfo.Length}");
            }

            Console.WriteLine("Press any key to delete the temporary log file...");
            Console.ReadKey(true);

            foreach (var fileInfo in dir.GetFiles())
            {
                fileInfo.Delete();
            }
        }
    }
}
