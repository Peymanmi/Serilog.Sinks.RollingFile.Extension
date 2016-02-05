namespace Serilog.Sinks.RollingFile.Extension.Sinks
{
    using System;

    public class RollingLogFile
    {
        public RollingLogFile(string filename, DateTime date, int sequenceNumber)
        {
            Filename = filename;
            Date = date;
            SequenceNumber = sequenceNumber;
        }

        public string Filename { get; }

        public DateTime Date { get; }

        public int SequenceNumber { get; private set; }


        public DateTime Created { get; }
        internal RollingLogFile Next(TemplatedPathRoller roller)
        {
            var fileName = roller.GetLogFilePath(DateTime.UtcNow, SequenceNumber + 1);
            return new RollingLogFile(fileName, DateTime.UtcNow, SequenceNumber + 1);
        }

        internal void ResetSequance()
        {
            SequenceNumber = 0;
        }
    }
}
