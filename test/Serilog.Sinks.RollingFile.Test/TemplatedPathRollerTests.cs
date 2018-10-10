namespace Serilog.Sinks.RollingFile.Test
{
    using System;
    using System.IO;
    using System.Linq;

    using NUnit.Framework;

    using Serilog.Events;
    using Serilog.Sinks.RollingFile.Extension.Sinks;

    [Category("Unit")]
    public class TemplatedPathRollerTests
    {
        [Test]
        public void ANonZeroIncrementIsIncludedAndPadded()
        {
            var roller = new TemplatedPathRoller("Logs\\log.{Date}.txt");
            var now = new DateTime(2013, 7, 14, 3, 24, 9, 980);
            var path = roller.GetLogFilePath(now, LogEventLevel.Information, 12);
            AssertEqualAbsolute("Logs\\log.20130714_0012.txt", path);
        }

        [Test]
        public void IfNoTokenIsSpecifiedDashFollowedByTheDateIsImplied()
        {
            var roller = new TemplatedPathRoller("Logs\\log.txt");
            var now = new DateTime(2013, 7, 14, 3, 24, 9, 980);
            var path = roller.GetLogFilePath(now, LogEventLevel.Information, 0);
            AssertEqualAbsolute("Logs\\log-20130714.txt", path);
        }

        [Test]
        public void MatchingExcludesSimilarButNonmatchingFiles()
        {
            var roller = new TemplatedPathRoller("log-{Date}.txt");
            const string similar1 = "log-0.txt";
            const string similar2 = "log-helloyou.txt";
            var matched = roller.SelectMatches(new[] { similar1, similar2 });
            Assert.AreEqual(0, matched.Count());
        }

        [Test]
        public void MatchingParsesDates()
        {
            var roller = new TemplatedPathRoller("log-{Date}.txt");
            const string newer = "log-20150101.txt";
            const string older = "log-20141231.txt";
            var matched = roller.SelectMatches(new[] { newer, older }).OrderBy(m => m.Date).Select(m => m.Filename)
                .ToArray();
            Assert.AreEqual(new[] { newer, older }, matched);
        }

        [Test]
        public void MatchingSelectsFiles()
        {
            var roller = new TemplatedPathRoller("log-{Date}.txt");
            const string example1 = "log-20131210.txt";
            const string example2 = "log-20131210-debug_031.txt";
            const string example3 = "log-20131210_031.txt";
            var matched = roller.SelectMatches(new[] { example1, example2, example3 }).ToArray();
            Assert.AreEqual(2, matched.Count());
            Assert.AreEqual(0, matched[0].SequenceNumber);
            Assert.AreEqual(31, matched[1].SequenceNumber);
        }

        [Test]
        public void MatchingSelectsFilesIncludeLevel()
        {
            var roller = new TemplatedPathRoller("log-{Date}-{Level}.txt");
            const string example1 = "log-20131210-information.txt";
            const string example2 = "log-20131210-debug_031.txt";
            const string example3 = "log-20131210-021.txt";
            var matched = roller.SelectMatches(new[] { example1, example2, example3 }).ToArray();
            Assert.AreEqual(2, matched.Count());
            Assert.AreEqual(0, matched[0].SequenceNumber);
            Assert.AreEqual(31, matched[1].SequenceNumber);

            Assert.AreEqual(LogEventLevel.Information, matched[0].Level);
            Assert.AreEqual(LogEventLevel.Debug, matched[1].Level);
        }

        [Test]
        public void NewStyleSpecifierCannotBeProvidedInDirectory()
        {
            var ex = Assert.Throws<ArgumentException>(() => new TemplatedPathRoller("{Date}\\log.txt"));
            Assert.True(ex.Message.Contains("directory"));
        }

        [Test]
        public void TheDirectorSearchPatternUsesWildcardInPlaceOfDate()
        {
            var roller = new TemplatedPathRoller("Logs\\log-{Date}.txt");
            Assert.AreEqual("log-*.txt", roller.DirectorySearchPattern);
        }

        [TestCase("Logs\\log.{Date}.txt", "Logs\\log.20130714.txt")]
        [TestCase("Logs\\log.{Date}.{Level}.txt", "Logs\\log.20130714.information.txt")]
        public void TheLogFileIncludesDateToken(string pattern, string expectedResult)
        {
            var roller = new TemplatedPathRoller(pattern);
            var now = new DateTime(2013, 7, 14, 3, 24, 9, 980);
            var path = roller.GetLogFilePath(now, LogEventLevel.Information, 0);
            AssertEqualAbsolute(expectedResult, path);
        }

        [Test]
        public void TheLogFileIsNotRequiredToIncludeADirectory()
        {
            var roller = new TemplatedPathRoller("log-{Date}");
            var now = new DateTime(2013, 7, 14, 3, 24, 9, 980);
            var path = roller.GetLogFilePath(now, LogEventLevel.Information, 0);
            AssertEqualAbsolute("log-20130714", path);
        }

        [Test]
        public void TheLogFileIsNotRequiredToIncludeAnExtension()
        {
            var roller = new TemplatedPathRoller("Logs\\log-{Date}");
            var now = new DateTime(2013, 7, 14, 3, 24, 9, 980);
            var path = roller.GetLogFilePath(now, LogEventLevel.Information, 0);
            AssertEqualAbsolute("Logs\\log-20130714", path);
        }

        [Test]
        public void TheRollerReturnsTheLogFileDirectory()
        {
            var roller = new TemplatedPathRoller("Logs\\log.{Date}.txt");
            AssertEqualAbsolute("Logs", roller.LogFileDirectory);
        }

        [Test]
        public void WhenOldStyleSpecifierIsSuppliedTheExceptionIsInformative()
        {
            var ex = Assert.Throws<ArgumentException>(() => new TemplatedPathRoller("log-{0}.txt"));
            Assert.True(ex.Message.Contains("{Date}"));
        }

        private static void AssertEqualAbsolute(string path1, string path2)
        {
            var abs1 = Path.GetFullPath(path1);
            var abs2 = Path.GetFullPath(path2);
            Assert.AreEqual(abs1, abs2);
        }
    }
}