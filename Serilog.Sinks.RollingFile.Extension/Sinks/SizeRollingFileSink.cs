namespace Serilog.Sinks.RollingFile.Extension.Sinks
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Polly;

    using Serilog.Core;
    using Serilog.Debugging;
    using Serilog.Events;
    using Serilog.Formatting;

    public class SizeRollingFileSink : ILogEventSink, IDisposable
    {
        private static readonly string ThisObjectName = typeof(SizeLimitedFileSink).Name;

        private readonly CancellationTokenSource cancelToken = new CancellationTokenSource();

        private readonly Encoding encoding;

        private readonly long fileSizeLimitBytes;

        private readonly ITextFormatter formatter;

        private readonly BlockingCollection<LogEvent> queue;

        private readonly TimeSpan? retainedFileDurationLimit;

        private readonly TemplatedPathRoller roller;

        private readonly object syncRoot = new object();

        private readonly ITextFormatter textFormatter;

        private SizeLimitedFileSink currentSink;

        private bool disposed;

        public SizeRollingFileSink(
            string pathFormat,
            ITextFormatter formatter,
            long fileSizeLimitBytes,
            TimeSpan? retainedFileDurationLimit,
            Encoding encoding = null)
        {
            roller = new TemplatedPathRoller(pathFormat);

            this.formatter = formatter;
            this.fileSizeLimitBytes = fileSizeLimitBytes;
            this.encoding = encoding;
            this.retainedFileDurationLimit = retainedFileDurationLimit;
            currentSink = GetLatestSink();

            if (AsyncOptions.SupportAsync)
            {
                queue = new BlockingCollection<LogEvent>(AsyncOptions.BufferSize);
                Task.Run((Action)ProcessQueue, cancelToken.Token);
            }
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (!disposed && currentSink != null)
                {
                    currentSink.Dispose();
                    currentSink = null;
                    disposed = true;
                    cancelToken.Cancel();
                }
            }
        }

        /// <summary>
        ///     Emits a log event to this sink
        /// </summary>
        /// <param name="logEvent">The <see cref="LogEvent" /> to emit</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException("logEvent");
            }

            if (AsyncOptions.SupportAsync)
                queue.Add(logEvent);
            else
                WriteToFile(logEvent);
        }

        private static void EnsureDirectoryCreated(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void ApplyRetentionPolicy(string currentFilePath)
        {
            if (!retainedFileDurationLimit.HasValue)
            {
                return;
            }

            var currentFileName = Path.GetFileName(currentFilePath);

            var potentialMatches = Directory.GetFiles(roller.LogFileDirectory, roller.DirectorySearchPattern)
                .Select(Path.GetFileName).Union(new[] { currentFileName });

            var toRemove = roller.GetAllFiles()
                .Where(
                    f => DateTime.UtcNow.Subtract(f.Date).TotalSeconds > retainedFileDurationLimit.Value.TotalSeconds)
                .Select(f => f.Filename).ToList();

            foreach (var obsolete in toRemove)
            {
                var fullPath = Path.Combine(roller.LogFileDirectory, obsolete);
                try
                {
                    File.Delete(fullPath);
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Error {0} while removing obsolete file {1}", ex, fullPath);
                }
            }
        }

        private SizeLimitedFileSink GetLatestSink()
        {
            EnsureDirectoryCreated(roller.LogFileDirectory);

            var logFile = roller.GetLatestOrNew();

            return new SizeLimitedFileSink(formatter, roller, fileSizeLimitBytes, logFile, encoding);
        }

        private SizeLimitedFileSink NextSizeLimitedFileSink(bool resetSequance = false, LogEventLevel? level = null)
        {
            if (resetSequance)
            {
                currentSink.LogFile.ResetSequance();
            }

            var next = currentSink.LogFile.Next(roller, level);
            currentSink.Dispose();

            return new SizeLimitedFileSink(formatter, roller, fileSizeLimitBytes, next, encoding)
                       {
                           ActiveLogLevel = level
                       };
        }

        private void ProcessQueue()
        {
            try
            {
                Func<int, TimeSpan> sleepFunc = retryNumber =>
                    TimeSpan.FromMilliseconds(AsyncOptions.RetryWaitInMillisecond * retryNumber);

                var polly = Policy.Handle<Exception>();
                polly.WaitAndRetry(
                        AsyncOptions.MaxRetries,
                        sleepFunc,
                        (exception, timeSpan) => Log.Error("Error executing callback {@Exception}", exception))
                    .Execute(ProcessQueueWithRetry);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine(
                    "Error occured in processing queue, {0} thread: {1}",
                    typeof(SizeRollingFileSink),
                    ex);
            }
        }

        private void ProcessQueueWithRetry()
        {
            try
            {
                while (true)
                {
                    var logEvent = queue.Take(cancelToken.Token);
                    WriteToFile(logEvent);
                }
            }
            catch
            {
                SelfLog.WriteLine("Error occured in ProcessQueueWithRetry, {0} ", typeof(SizeRollingFileSink));
                throw;
            }
        }

        private void WriteToFile(LogEvent logEvent)
        {
            lock (syncRoot)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(ThisObjectName, "The rolling file sink has been disposed");
                }

                var resetSequence = currentSink.LogFile.Date.Date != DateTime.UtcNow.Date;

                if (currentSink.EnableLevelLogging && currentSink.ActiveLogLevel != logEvent.Level)
                {
                    currentSink = NextSizeLimitedFileSink(resetSequence, logEvent.Level);
                }

                if (currentSink.SizeLimitReached || resetSequence)
                {
                    currentSink = NextSizeLimitedFileSink(resetSequence, logEvent.Level);
                    ApplyRetentionPolicy(roller.LogFileDirectory);
                }

                if (currentSink != null)
                {
                    currentSink.Emit(logEvent);
                }
            }
        }
    }
}