namespace Serilog.Sinks.RollingFile.Extension.Sinks
{
    using System;
    using System.IO;
    using System.Text;
    using Core;
    using Events;
    using Formatting;
    using Debugging;

    internal class SizeLimitedFileSink : ILogEventSink, IDisposable
    {
        private static readonly string ThisObjectName = typeof(SizeLimitedFileSink).Name;

        private readonly ITextFormatter formatter;
        private readonly TemplatedPathRoller roller;
        private readonly StreamWriter output;
        private readonly long fileSizeLimitBytes;
        private readonly object syncRoot = new object();
        private RollingLogFile currentLogFile;
        private bool disposed;
        private bool sizeLimitReached;

        public SizeLimitedFileSink(ITextFormatter formatter, TemplatedPathRoller roller, long fileSizeLimitBytes, Encoding encoding = null)
                : this(formatter, roller, fileSizeLimitBytes, roller.GetLatestOrNew(), encoding)
        {
            this.formatter = formatter;
            this.roller = roller;
            this.fileSizeLimitBytes = fileSizeLimitBytes;
            this.output = OpenFileForWriting(roller.LogFileDirectory, roller.GetLatestOrNew(), encoding ?? Encoding.UTF8);
        }

        public SizeLimitedFileSink(ITextFormatter formatter, TemplatedPathRoller roller, long fileSizeLimitBytes,
            RollingLogFile rollingLogFile, Encoding encoding = null)
        {
            this.formatter = formatter;
            this.roller = roller;
            this.fileSizeLimitBytes = fileSizeLimitBytes;
            this.output = OpenFileForWriting(roller.LogFileDirectory, rollingLogFile, encoding ?? Encoding.UTF8);
        }

        private StreamWriter OpenFileForWriting(string folderPath, RollingLogFile rollingLogFile, Encoding encoding)
        {
            EnsureDirectoryCreated(folderPath);
            try
            {
                currentLogFile = rollingLogFile;
                var fullPath = Path.Combine(folderPath, rollingLogFile.Filename);
                var stream = File.Open(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);

                return new StreamWriter(stream, encoding ?? Encoding.UTF8);
            }
            catch (IOException ex)
            {
                SelfLog.WriteLine("Error {0} while opening obsolete file {1}", ex, rollingLogFile.Filename);

                return OpenFileForWriting(folderPath, rollingLogFile.Next(roller), encoding);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Error {0} while opening obsolete file {1}", ex, rollingLogFile.Filename);
                throw;
            }
        }

        private static void EnsureDirectoryCreated(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException("logEvent");

            lock (syncRoot)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(ThisObjectName, "Cannot write to disposed file");
                }

                if (output == null)
                {
                    return;
                }

                formatter.Format(logEvent, output);
                output.Flush();

                if (output.BaseStream.Length > fileSizeLimitBytes)
                {
                    sizeLimitReached = true;
                }
            }
        }

        internal bool SizeLimitReached { get { return sizeLimitReached; } }

        internal RollingLogFile LogFile
        {
            get
            {
                return currentLogFile;
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                output.Flush();
                output.Dispose();
                disposed = true;
            }
        }
    }
}
