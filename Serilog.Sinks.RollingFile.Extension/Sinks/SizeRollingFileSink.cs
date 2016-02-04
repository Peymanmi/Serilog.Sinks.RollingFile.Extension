namespace Serilog.Sinks.RollingFile.Extension.Sinks
{
    using System;
    using System.Linq;
    using Core;
    using Events;
    using Formatting;
    using System.Text;
    using System.IO;
    using Debugging;

    public class SizeRollingFileSink : ILogEventSink, IDisposable
    {
        private static readonly string ThisObjectName = (typeof(SizeLimitedFileSink).Name);
        private readonly ITextFormatter formatter;
        private readonly long fileSizeLimitBytes;
        private readonly TimeSpan? retainedFileDurationLimit;
        private readonly Encoding encoding;
        private SizeLimitedFileSink currentSink;
        private readonly object syncRoot = new object();
        private bool disposed;
        private readonly TemplatedPathRoller roller;

        public SizeRollingFileSink(string pathFormat, ITextFormatter formatter, long fileSizeLimitBytes,
            TimeSpan? retainedFileDurationLimit, Encoding encoding = null)
        {
            roller = new TemplatedPathRoller(pathFormat);

            this.formatter = formatter;
            this.fileSizeLimitBytes = fileSizeLimitBytes;
            this.encoding = encoding;
            this.retainedFileDurationLimit = retainedFileDurationLimit;
            this.currentSink = GetLatestSink();
        }

        /// <summary>
        /// Emits a log event to this sink
        /// </summary>
        /// <param name="logEvent">The <see cref="LogEvent"/> to emit</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Emit(LogEvent logEvent)
        {

            if (logEvent == null)
            {
                throw new ArgumentNullException("logEvent");
            }

            lock (this.syncRoot)
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(ThisObjectName, "The rolling file sink has been disposed");
                }

                if (currentSink.SizeLimitReached || currentSink.LogFile.Date.Date != DateTime.UtcNow.Date)
                {
                    this.currentSink = NextSizeLimitedFileSink();
                    ApplyRetentionPolicy(roller.LogFileDirectory);
                }

                if (this.currentSink != null)
                {
                    this.currentSink.Emit(logEvent);
                }
            }
        }

        private SizeLimitedFileSink GetLatestSink()
        {
            EnsureDirectoryCreated(roller.LogFileDirectory);

            var logFile = roller.GetLatestOrNew();

            return new SizeLimitedFileSink(
                this.formatter,
                roller,
                fileSizeLimitBytes,
                logFile,
                this.encoding);
        }

        private SizeLimitedFileSink NextSizeLimitedFileSink()
        {
            var next = this.currentSink.LogFile.Next(roller);
            this.currentSink.Dispose();

            return new SizeLimitedFileSink(this.formatter, roller, fileSizeLimitBytes, next, this.encoding);
        }

        private static void EnsureDirectoryCreated(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (this.syncRoot)
            {
                if (!this.disposed && this.currentSink != null)
                {
                    this.currentSink.Dispose();
                    this.currentSink = null;
                    this.disposed = true;
                }
            }
        }

        void ApplyRetentionPolicy(string currentFilePath)
        {
            if (retainedFileDurationLimit == null) return;

            var currentFileName = Path.GetFileName(currentFilePath);

            var potentialMatches = Directory.GetFiles(roller.LogFileDirectory, roller.DirectorySearchPattern)
                .Select(Path.GetFileName)
                .Union(new[] { currentFileName });

            var toRemove = roller.GetAllFiles()
                .Where(f => DateTime.UtcNow.Subtract(f.Date).TotalSeconds > retainedFileDurationLimit.Value.TotalSeconds)
                .Select(f => f.Filename)
                .ToList();

            foreach (var obsolete in toRemove)
            {
                var fullPath = Path.Combine(roller.LogFileDirectory, obsolete);
                try
                {
                    System.IO.File.Delete(fullPath);
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Error {0} while removing obsolete file {1}", ex, fullPath);
                }
            }
        }
    }
}
