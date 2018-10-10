namespace Serilog.Sinks.RollingFile.Extension.Sinks
{
    using System;
    using System.Collections.Generic;

    using Serilog.Events;

    public class RollingLogFile
    {
        public IDictionary<LogEventLevel, int> SequenceCollection = new Dictionary<LogEventLevel, int>();

        public RollingLogFile(string filename, DateTime date, int sequenceNumber)
        {
            Filename = filename;
            Date = date;
            SequenceNumber = sequenceNumber;
        }

        public RollingLogFile(string filename, DateTime date, int sequenceNumber, string level)
            : this(filename, date, sequenceNumber)
        {
            LogEventLevel logLevel;
            if (Enum.TryParse(level, true, out logLevel))
            {
                Level = logLevel;

                if (!SequenceCollection.ContainsKey(logLevel))
                    SequenceCollection.Add(logLevel, sequenceNumber);
                else
                    SequenceCollection[logLevel] = sequenceNumber;
            }
        }

        public DateTime Created { get; }

        public DateTime Date { get; }

        public string Filename { get; }

        public LogEventLevel? Level { get; private set; }

        public int SequenceNumber
        {
            get
            {
                var l = Level.HasValue ? Level.Value : LogEventLevel.Information;
                if (!SequenceCollection.ContainsKey(l))
                    SequenceCollection.Add(l, 0);

                return SequenceCollection[l];
            }
            private set
            {
                var l = Level.HasValue ? Level.Value : LogEventLevel.Information;
                if (!SequenceCollection.ContainsKey(l))
                    SequenceCollection.Add(l, value);

                SequenceCollection[l] = value;
            }
        }

        internal RollingLogFile Next(TemplatedPathRoller roller, LogEventLevel? level = null)
        {
            Level = level;
            var fileName = roller.GetLogFilePath(DateTime.UtcNow, level, SequenceNumber + 1);
            return new RollingLogFile(
                fileName,
                DateTime.UtcNow,
                SequenceNumber + 1,
                level.HasValue ? level.ToString() : null);
        }

        internal void ResetSequance()
        {
            SequenceNumber = 0;
        }
    }
}