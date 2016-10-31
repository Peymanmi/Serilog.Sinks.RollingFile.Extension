namespace Serilog.Sinks.RollingFile.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


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

        public static bool SupportAsync { get; set; }

        public static int RetryWaitInMillisecond { get; set; }
    }
}
