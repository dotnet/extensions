// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing.Tests
{
    public class AssemblyTestLogTests : LoggedTest
    {
        private static readonly Assembly ThisAssembly = typeof(AssemblyTestLog).GetTypeInfo().Assembly;

        public AssemblyTestLogTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ForAssembly_ReturnsSameInstanceForSameAssembly()
        {
            Assert.Same(
                AssemblyTestLog.ForAssembly(ThisAssembly),
                AssemblyTestLog.ForAssembly(ThisAssembly));
        }

        [Fact]
        public void TestLogWritesToITestOutputHelper()
        {
            var output = new TestTestOutputHelper();
            var assemblyLog = AssemblyTestLog.Create("NonExistant.Test.Assembly", baseDirectory: null);

            using (assemblyLog.StartTestLog(output, "NonExistant.Test.Class", out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger("TestLogger");
                logger.LogInformation("Information!");

                // Trace is disabled by default
                logger.LogTrace("Trace!");
            }

            Assert.Equal(@"[TIMESTAMP] TestLifetime Information: Starting test TestLogWritesToITestOutputHelper
[TIMESTAMP] TestLogger Information: Information!
[TIMESTAMP] TestLifetime Information: Finished test TestLogWritesToITestOutputHelper in DURATION
", MakeConsistent(output.Output), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public Task TestLogWritesToGlobalLogFile() =>
            RunTestLogFunctionalTest((tempDir, loggerFactory) =>
        {
            // Because this test writes to a file, it is a functional test and should be logged
            // but it's also testing the test logging facility. So this is pretty meta ;)
            var logger = loggerFactory.CreateLogger("Test");

            using (var testAssemblyLog = AssemblyTestLog.Create("FakeTestAssembly", tempDir))
            {
                logger.LogInformation("Created test log in {baseDirectory}", tempDir);

                using (testAssemblyLog.StartTestLog(output: null, className: "FakeTestAssembly.FakeTestClass", loggerFactory: out var testLoggerFactory, minLogLevel: LogLevel.Trace, testName: "FakeTestName"))
                {
                    var testLogger = testLoggerFactory.CreateLogger("TestLogger");
                    testLogger.LogInformation("Information!");
                    testLogger.LogTrace("Trace!");
                }
            }

            logger.LogInformation("Finished test log in {baseDirectory}", tempDir);

            var globalLogPath = Path.Combine(tempDir, "FakeTestAssembly", RuntimeInformation.FrameworkDescription.TrimStart('.'), "global.log");
            var testLog = Path.Combine(tempDir, "FakeTestAssembly", RuntimeInformation.FrameworkDescription.TrimStart('.'), "FakeTestClass", $"FakeTestName.log");

            Assert.True(File.Exists(globalLogPath), $"Expected global log file {globalLogPath} to exist");
            Assert.True(File.Exists(testLog), $"Expected test log file {testLog} to exist");

            var globalLogContent = MakeConsistent(File.ReadAllText(globalLogPath));
            var testLogContent = MakeConsistent(File.ReadAllText(testLog));

            Assert.Equal(@"[GlobalTestLog] [Information] Global Test Logging initialized. Set the 'ASPNETCORE_TEST_LOG_DIR' Environment Variable in order to create log files on disk.
[GlobalTestLog] [Information] Starting test ""FakeTestName""
[GlobalTestLog] [Information] Finished test ""FakeTestName"" in DURATION
", globalLogContent, ignoreLineEndingDifferences: true);
            Assert.Equal(@"[TestLifetime] [Information] Starting test ""FakeTestName""
[TestLogger] [Information] Information!
[TestLogger] [Verbose] Trace!
[TestLifetime] [Information] Finished test ""FakeTestName"" in DURATION
", testLogContent, ignoreLineEndingDifferences: true);
        });

        [Fact]
        public Task TestLogTruncatesTestNameToAvoidLongPaths() =>
            RunTestLogFunctionalTest((tempDir, loggerFactory) =>
        {
            var longTestName = new string('0', 50) + new string('1', 50) + new string('2', 50) + new string('3', 50) + new string('4', 50);
            var logger = loggerFactory.CreateLogger("Test");
            using (var testAssemblyLog = AssemblyTestLog.Create("FakeTestAssembly", tempDir))
            {
                logger.LogInformation("Created test log in {baseDirectory}", tempDir);

                using (testAssemblyLog.StartTestLog(output: null, className: "FakeTestAssembly.FakeTestClass", loggerFactory: out var testLoggerFactory, minLogLevel: LogLevel.Trace, testName: longTestName))
                {
                    testLoggerFactory.CreateLogger("TestLogger").LogInformation("Information!");
                }
            }
            logger.LogInformation("Finished test log in {baseDirectory}", tempDir);

            var testLogFiles = new DirectoryInfo(Path.Combine(tempDir, "FakeTestAssembly", RuntimeInformation.FrameworkDescription.TrimStart('.'), "FakeTestClass")).EnumerateFiles();
            var testLog = Assert.Single(testLogFiles);
            var testFileName = Path.GetFileNameWithoutExtension(testLog.Name);

            // The first half of the file comes from the beginning of the test name passed to the logger
            Assert.Equal(longTestName.Substring(0, testFileName.Length / 2), testFileName.Substring(0, testFileName.Length / 2));
            // The last half of the file comes from the ending of the test name passed to the logger
            Assert.Equal(longTestName.Substring(longTestName.Length - testFileName.Length / 2, testFileName.Length / 2), testFileName.Substring(testFileName.Length - testFileName.Length / 2, testFileName.Length / 2));

            var testLogContent = MakeConsistent(File.ReadAllText(testLog.FullName));
        });

        [Fact]
        public  Task TestLogEnumerateFilenamesToAvoidCollisions() =>
            RunTestLogFunctionalTest((tempDir, loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Test");
            using (var testAssemblyLog = AssemblyTestLog.Create("FakeTestAssembly", tempDir))
            {
                logger.LogInformation("Created test log in {baseDirectory}", tempDir);

                for (var i = 0; i < 10; i++)
                {
                    using (testAssemblyLog.StartTestLog(output: null, className: "FakeTestAssembly.FakeTestClass", loggerFactory: out var testLoggerFactory, minLogLevel: LogLevel.Trace, testName: "FakeTestName"))
                    {
                        testLoggerFactory.CreateLogger("TestLogger").LogInformation("Information!");
                    }
                }
            }
            logger.LogInformation("Finished test log in {baseDirectory}", tempDir);

            // The first log file exists
            Assert.True(File.Exists(Path.Combine(tempDir, "FakeTestAssembly", RuntimeInformation.FrameworkDescription.TrimStart('.'), "FakeTestClass", $"FakeTestName.log")));

            // Subsequent files exist
            for (var i = 0; i < 9; i++)
            {
                Assert.True(File.Exists(Path.Combine(tempDir, "FakeTestAssembly", RuntimeInformation.FrameworkDescription.TrimStart('.'), "FakeTestClass", $"FakeTestName.{i}.log")));
            }
        });

        private static readonly Regex TimestampRegex = new Regex(@"\d+-\d+-\d+T\d+:\d+:\d+");
        private static readonly Regex DurationRegex = new Regex(@"[^ ]+s$");

        private async Task RunTestLogFunctionalTest(Action<string, ILoggerFactory> action, [CallerMemberName] string testName = null)
        {
            using (StartLog(out var loggerFactory, testName))
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"TestLogging_{Guid.NewGuid().ToString("N")}");
                try
                {
                    action(tempDir, loggerFactory);
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        try
                        {
                            Directory.Delete(tempDir, recursive: true);
                        }
                        catch
                        {
                            await Task.Delay(100);
                            Directory.Delete(tempDir, recursive: true);
                        }
                    }
                }
            }
        }

        private static string MakeConsistent(string input)
        {
            return string.Join(Environment.NewLine, input.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .Select(line =>
                {
                    var strippedPrefix = line.IndexOf("[") >= 0 ? line.Substring(line.IndexOf("[")) : line;

                    var strippedDuration =
                        DurationRegex.Replace(strippedPrefix, "DURATION");
                    var strippedTimestamp = TimestampRegex.Replace(strippedDuration, "TIMESTAMP");
                    return strippedTimestamp;
                }));
        }
    }
}
