namespace Serilog.Sinks.RollingFile.Extension
{
    internal static class AsyncOptions
    {
        static AsyncOptions()
        {
            MaxRetries = 3;
            RetryWaitInMillisecond = 100;
            BufferSize = 1000;
            SupportAsync = false;
        }

        public static int BufferSize { get; set; }

        public static int MaxRetries { get; set; }

        public static int RetryWaitInMillisecond { get; set; }

        public static bool SupportAsync { get; set; }
    }
}