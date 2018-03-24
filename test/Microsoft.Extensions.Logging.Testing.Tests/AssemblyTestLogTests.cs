// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public async Task TestLogWritesToGlobalLogFile()
        {
            // Because this test writes to a file, it is a functional test and should be logged
            // but it's also testing the test logging facility. So this is pretty meta ;)
            var tempDir = Path.Combine(Path.GetTempPath(), $"TestLogging_{Guid.NewGuid().ToString("N")}");
            try
            {
                using (StartLog(out var loggerFactory))
                {
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
                }

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

        private static readonly Regex TimestampRegex = new Regex(@"\d+-\d+-\d+T\d+:\d+:\d+");
        private static readonly Regex DurationRegex = new Regex(@"[^ ]+s$");
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
