namespace Serilog.Sinks.RollingFile.Extension.Sinks
{
    using Events;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class TemplatedPathRoller
    {
        const string OldStyleDateSpecifier = "{0}";
        const string DateSpecifier = "{Date}";
        const string LeveSpecifier = "{Level}";
        const string DateFormat = "yyyyMMdd";
        const string DefaultSeparator = "-";

        readonly string directory;
        readonly string _pathTemplate;
        readonly Regex _filenameMatcher;

        public TemplatedPathRoller(string pathTemplate)
        {
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (pathTemplate.Contains(OldStyleDateSpecifier))
                throw new ArgumentException("The old-style date specifier " + OldStyleDateSpecifier +
                    " is no longer supported, instead please use " + DateSpecifier);

            directory = Path.GetDirectoryName(pathTemplate);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }

            directory = Path.GetFullPath(directory);

            if (directory.Contains(DateSpecifier))
                throw new ArgumentException("The date cannot form part of the directory name");

            var filenameTemplate = Path.GetFileName(pathTemplate);
            if (!filenameTemplate.Contains(DateSpecifier))
            {
                filenameTemplate = Path.GetFileNameWithoutExtension(filenameTemplate) + DefaultSeparator +
                    DateSpecifier + Path.GetExtension(filenameTemplate);
            }

            var indexOfDateSpecifier = filenameTemplate.IndexOf(DateSpecifier, StringComparison.Ordinal);
            var indexOfLevelSpecifier = filenameTemplate.IndexOf(LeveSpecifier, StringComparison.Ordinal);
            if(indexOfLevelSpecifier == -1)
            {
                var prefix = filenameTemplate.Substring(0, indexOfDateSpecifier);                
                var suffix = filenameTemplate.Substring(indexOfDateSpecifier + DateSpecifier.Length);
                _filenameMatcher = new Regex(
                            "^" +
                            Regex.Escape(prefix) +
                            "(?<date>\\d{" + DateFormat.Length + "})" +                            
                            "(?<inc>_[0-9]{3,}){0,1}" +
                            Regex.Escape(suffix) +
                            "$");
            }
            else if (indexOfDateSpecifier < indexOfLevelSpecifier)
            {
                var prefix = filenameTemplate.Substring(0, indexOfDateSpecifier);
                var middle = filenameTemplate.Substring(indexOfDateSpecifier + DateSpecifier.Length, indexOfLevelSpecifier - (indexOfDateSpecifier + DateSpecifier.Length));
                var suffix = filenameTemplate.Substring(indexOfLevelSpecifier + LeveSpecifier.Length);
                _filenameMatcher = new Regex(
                            "^" +
                            Regex.Escape(prefix) +
                            "(?<date>\\d{" + DateFormat.Length + "})" +
                            Regex.Escape(middle) +
                            "(?<level>[aA-zZ]+?(?=_|\\.))" +
                            "(?<inc>_[0-9]{3,}){0,1}" +
                            Regex.Escape(suffix) +
                            "$");
            }
            else
            {
                var prefix = filenameTemplate.Substring(0, indexOfLevelSpecifier);
                var middle = filenameTemplate.Substring(indexOfLevelSpecifier + LeveSpecifier.Length, indexOfDateSpecifier - (indexOfLevelSpecifier + LeveSpecifier.Length));
                var suffix = filenameTemplate.Substring(indexOfDateSpecifier + DateSpecifier.Length);
                _filenameMatcher = new Regex(
                                "^" +
                                Regex.Escape(prefix) +
                                "(?<level>[aA-zZ]+?(?=\\" + middle + " )" +
                                Regex.Escape(middle) +
                                "(?<date>\\d{" + DateFormat.Length + "})" +
                                "(?<inc>_[0-9]{3,}){0,1}" +
                                Regex.Escape(suffix) +
                                "$");
            }



            DirectorySearchPattern = filenameTemplate.Replace(DateSpecifier, "*");
            DirectorySearchPattern = filenameTemplate.Replace(LeveSpecifier, "*");
            LogFileDirectory = directory;
            _pathTemplate = Path.Combine(LogFileDirectory, filenameTemplate);

            this.PathIncludesLevel = filenameTemplate.IndexOf(LeveSpecifier, StringComparison.Ordinal) > -1;
        }

        public string LogFileDirectory { get; }

        public string DirectorySearchPattern { get; }

        public bool PathIncludesLevel { get; private set; }

        public string GetLogFilePath(DateTime date, LogEventLevel? level, int sequenceNumber)
        {
            var tok = date.ToString(DateFormat, CultureInfo.InvariantCulture);

            if (sequenceNumber != 0)
                tok += "_" + sequenceNumber.ToString("0000", CultureInfo.InvariantCulture);

            return _pathTemplate.Replace(DateSpecifier, tok).Replace(LeveSpecifier, level?.ToString().ToLower());
        }

        internal RollingLogFile GetLatestOrNew(LogEventLevel level = LogEventLevel.Information)
        {
            var fileInfos = Directory.GetFiles(directory)
                .Select(f => new FileInfo(f));

            var matchedFiles = SelectMatches(fileInfos, DateTime.UtcNow);
            if (matchedFiles.Any())
                return matchedFiles.OrderBy(x => x.SequenceNumber).Last();
            else
                return new RollingLogFile(GetLogFilePath(DateTime.UtcNow, level, 1), DateTime.UtcNow, 1);
        }

        public IEnumerable<RollingLogFile> GetAllFiles()
        {
            var fileInfos = Directory.GetFiles(directory)
                .Select(f => new FileInfo(f));
            foreach (var fInfo in fileInfos)
            {
                var filename = fInfo.Name;
                var match = _filenameMatcher.Match(filename);
                if (match.Success)
                {
                    var inc = 0;
                    var incGroup = match.Groups["inc"];
                    if (incGroup.Captures.Count != 0)
                    {
                        var incPart = incGroup.Captures[0].Value.Substring(1);
                        inc = int.Parse(incPart, CultureInfo.InvariantCulture);
                    }

                    yield return new RollingLogFile(filename, fInfo.CreationTimeUtc, inc);
                }
            }
        }

        public IEnumerable<RollingLogFile> SelectMatches(IEnumerable<FileInfo> fileInfos, DateTime? date = null)
        {
            foreach (var fInfo in fileInfos)
            {
                var filename = fInfo.Name;
                if (date.HasValue)
                {
                    var tok = date.Value.ToString(DateFormat, CultureInfo.InvariantCulture);
                    if (filename.IndexOf(tok) < 0)
                        continue;
                }


                var match = _filenameMatcher.Match(filename);
                if (match.Success)
                {
                    var inc = 0;
                    var incGroup = match.Groups["inc"];
                    if (incGroup.Captures.Count != 0)
                    {
                        var incPart = incGroup.Captures[0].Value.Substring(1);
                        inc = int.Parse(incPart, CultureInfo.InvariantCulture);
                    }

                    yield return new RollingLogFile(filename, fInfo.CreationTimeUtc, inc);
                }

            }
        }

        public IEnumerable<RollingLogFile> SelectMatches(IEnumerable<string> filenames)
        {
            foreach (var filename in filenames)
            {
                var match = _filenameMatcher.Match(filename);
                if (match.Success)
                {
                    var inc = 0;
                    var incGroup = match.Groups["inc"];
                    var level = match.Groups["level"];
                    if (incGroup.Captures.Count != 0)
                    {
                        var incPart = incGroup.Captures[0].Value.Substring(1);
                        inc = int.Parse(incPart, CultureInfo.InvariantCulture);
                    }

                    yield return new RollingLogFile(filename, DateTime.UtcNow, inc , level.Value);
                }
            }
        }
    }
}
