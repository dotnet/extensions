// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;
using Microsoft.Dnx.Testing.Abstractions;
using Microsoft.Dnx.TestHost.Client;
using Xunit;

namespace Microsoft.Dnx.TestHost
{
    public class TestHostTest
    {
        private readonly string _testProject;

        public TestHostTest()
        {
            var services = CallContextServiceLocator.Locator.ServiceProvider;

            var libraryManager = (ILibraryManager)services.GetService(typeof(ILibraryManager));
            _testProject = Path.GetDirectoryName(libraryManager.GetLibrary("Sample.Tests").Path);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task ListTest()
        {
            // Arrange
            var host = new TestHostWrapper(_testProject);

            await host.StartAsync();

            // Act
            var result = await host.ListTestsAsync();

            // Assert
            Assert.Equal(0, result);

            /* Following message will be sent when test is running in an environment missing DIA.
               Should it exists, it will be extracted from the message list.
               {
                   "Name": "Microsoft.Dnx.TestHost.TestAdapter.SourceInformationProvider",
                   "EventId": 0,
                   "Level": "Warning",
                   "Message": "Failed to create DIA DataSource. No source information will be available.\r\nSystem.Runtime.InteropServices.COMException (0x80040154): Retrieving the COM class factory for component with CLSID {E6756135-1E65-4D17-8576-610761398C3C} failed due to the following error: 80040154 Class not registered (Exception from HRESULT: 0x80040154 (REGDB_E_CLASSNOTREG)).\r\n   at Microsoft.Framework.TestHost.TestAdapter.SourceInformationProvider.EnsureInitialized() in C:\\projects\\testing\\src\\Microsoft.Dnx.TestHost\\TestAdapter\\SourceInformationProvider.cs:line 155"
             */

            var fullMessageDiagnostics = string.Format("Full output: \n{0}", string.Join("\n", host.Output));
            var testOutput = host.Output.Where(message => message.MessageType != "Log");

            Assert.True(10 == testOutput.Count(), "Number of messages is not right. \n" + fullMessageDiagnostics);
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.True_is_true"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest1(x: 3)"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest2(x: 1, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest2(x: 2, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest2(x: 3, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.SampleAsyncTest"));
            Assert.Single(host.Output, m => TestFound(m, "DerivedTest.ThisGetsInherited"));
            Assert.Equal("TestDiscovery.Response", host.Output[host.Output.Count - 1].MessageType);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task ListTest_AsyncMethod_Symbols()
        {
            // Arrange
            var host = new TestHostWrapper(_testProject);

            await host.StartAsync();

            // Act
            var result = await host.ListTestsAsync();

            // Assert
            Assert.Equal(0, result);

            var test = GetTest(host.Output, "SampleTest.SampleAsyncTest");
            Assert.NotNull(test);

            Assert.EndsWith("SampleTest.cs", test.CodeFilePath);
            Assert.Equal(35, test.LineNumber);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task ListTest_InheritedMethod_Symbols()
        {
            // Arrange
            var host = new TestHostWrapper(_testProject);

            await host.StartAsync();

            // Act
            var result = await host.ListTestsAsync();

            // Assert
            Assert.Equal(0, result);

            var test = GetTest(host.Output, "DerivedTest.ThisGetsInherited");
            Assert.NotNull(test);

            Assert.EndsWith("BaseTest.cs", test.CodeFilePath);
            Assert.Equal(12, test.LineNumber);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task RunTest_All()
        {
            // Arrange
            var host = new TestHostWrapper(_testProject);

            await host.StartAsync();

            // Act
            var result = await host.RunTestsAsync();

            // Assert
            Assert.Equal(0, result);

            Assert.Equal(19, host.Output.Count);
            Assert.Single(host.Output, m => TestStarted(m, "SampleTest.True_is_true"));
            Assert.Single(host.Output, m => TestPassed(m, "SampleTest.True_is_true"));
            Assert.Single(host.Output, m => TestStarted(m, "SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestPassed(m, "SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestStarted(m, "SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestPassed(m, "SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestStarted(m, "SampleTest.TheoryTest1(x: 3)"));
            Assert.Single(host.Output, m => TestPassed(m, "SampleTest.TheoryTest1(x: 3)"));
            Assert.Single(host.Output, m => TestStarted(m, "SampleTest.TheoryTest2(x: 1, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestPassed(m, "SampleTest.TheoryTest2(x: 1, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestStarted(m, "SampleTest.TheoryTest2(x: 2, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestPassed(m, "SampleTest.TheoryTest2(x: 2, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestStarted(m, "SampleTest.TheoryTest2(x: 3, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestPassed(m, "SampleTest.TheoryTest2(x: 3, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestStarted(m, "SampleTest.SampleAsyncTest"));
            Assert.Single(host.Output, m => TestPassed(m, "SampleTest.SampleAsyncTest"));
            Assert.Single(host.Output, m => TestStarted(m, "DerivedTest.ThisGetsInherited"));
            Assert.Single(host.Output, m => TestPassed(m, "DerivedTest.ThisGetsInherited"));

            Assert.Equal("TestExecution.Response", host.Output[host.Output.Count - 1].MessageType);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task RunTest_ByUniqueName()
        {
            // Arrange
            var host = new TestHostWrapper(_testProject);

            await host.StartAsync();

            await host.ListTestsAsync();

            var test = host.Output
                .Where(m => m.MessageType == "TestDiscovery.TestFound")
                .First()
                .Payload.ToObject<Test>();

            host.Output.Clear();

            host = new TestHostWrapper(_testProject);
            await host.StartAsync();

            // Act
            var result = await host.RunTestsAsync(_testProject, test.FullyQualifiedName);

            // Assert
            Assert.Equal(0, result);

            Assert.Equal(3, host.Output.Count);
            Assert.Single(host.Output, m => TestStarted(m, test.DisplayName));
            Assert.Single(host.Output, m => TestPassed(m, test.DisplayName));
            Assert.Equal("TestExecution.Response", host.Output[host.Output.Count - 1].MessageType);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task RunTest_ByUniqueName_ProtocolVersion_MatchingVersion()
        {
            // Arrange
            var host = new TestHostWrapper(_testProject);
            host.ProtocolVersion = 1;

            await host.StartAsync();

            await host.ListTestsAsync();

            var test = host.Output
                .Where(m => m.MessageType == "TestDiscovery.TestFound")
                .First()
                .Payload.ToObject<Test>();

            host.Output.Clear();

            host = new TestHostWrapper(_testProject);
            host.ProtocolVersion = 1;
            await host.StartAsync();

            // Act
            var result = await host.RunTestsAsync(_testProject, test.FullyQualifiedName);

            // Assert
            Assert.Equal(0, result);

            Assert.Equal(1, host.ProtocolVersion);

            Assert.Equal(4, host.Output.Count);
            Assert.Single(host.Output, m => m.MessageType == "ProtocolVersion");
            Assert.Single(host.Output, m => TestStarted(m, test.DisplayName));
            Assert.Single(host.Output, m => TestPassed(m, test.DisplayName));
            Assert.Equal("TestExecution.Response", host.Output[host.Output.Count - 1].MessageType);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task RunTest_ByUniqueName_ProtocolVersion_UnknownVersion()
        {
            // Arrange
            var host = new TestHostWrapper(_testProject);
            host.ProtocolVersion = 2;

            await host.StartAsync();

            await host.ListTestsAsync();

            var test = host.Output
                .Where(m => m.MessageType == "TestDiscovery.TestFound")
                .First()
                .Payload.ToObject<Test>();

            host.Output.Clear();

            host = new TestHostWrapper(_testProject);
            host.ProtocolVersion = 2;
            await host.StartAsync();

            // Act
            var result = await host.RunTestsAsync(_testProject, test.FullyQualifiedName);

            // Assert
            Assert.Equal(0, result);

            Assert.Equal(1, host.ProtocolVersion);

            Assert.Equal(4, host.Output.Count);
            Assert.Single(host.Output, m => m.MessageType == "ProtocolVersion");
            Assert.Single(host.Output, m => TestStarted(m, test.DisplayName));
            Assert.Single(host.Output, m => TestPassed(m, test.DisplayName));
            Assert.Equal("TestExecution.Response", host.Output[host.Output.Count - 1].MessageType);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task TestHostExits_WhenParentProcessExits()
        {
            // Arrange
            var parentProcess = new Process();
            parentProcess.StartInfo.FileName = "cmd";
            parentProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            var testHost = new TestHostWrapper(_testProject);
            int testHostProcessId;

            try
            {
                parentProcess.Start();
                testHost.ParentProcessId = parentProcess.Id;

                // Act
                await testHost.StartAsync();
                testHostProcessId = testHost.Process.Id;
            }
            finally
            {
                parentProcess.Kill();
            }

            // Assert
            // By this time the test host process could have been killed and if not wait for 5 seconds
            // before doing a check again.
            var testHostProcess = GetProcessById(testHostProcessId);
            if (testHostProcess != null)
            {
                testHostProcess.WaitForExit(5 * 1000);
                testHostProcess = GetProcessById(testHostProcessId);
            }
            Assert.Null(testHostProcess);
        }
        private static bool TestFound(Message message, string name)
        {
            if (!string.Equals("TestDiscovery.TestFound", message.MessageType))
            {
                return false;
            }

            if (!string.Equals(name, message.Payload.ToObject<Test>().DisplayName))
            {
                return false;
            }

            return true;
        }

        private static bool TestStarted(Message message, string name)
        {
            if (!string.Equals("TestExecution.TestStarted", message.MessageType))
            {
                return false;
            }

            if (!string.Equals(name, message.Payload.ToObject<Test>().DisplayName))
            {
                return false;
            }

            return true;
        }

        private static bool TestPassed(Message message, string name)
        {
            if (!string.Equals("TestExecution.TestResult", message.MessageType))
            {
                return false;
            }

            var result = message.Payload.ToObject<TestResult>();
            if (!string.Equals(name, result.Test.DisplayName))
            {
                return false;
            }

            if (TestOutcome.Passed != result.Outcome)
            {
                return false;
            }

            return true;
        }

        private static Test GetTest(IEnumerable<Message> messages, string name)
        {
            foreach (var message in messages)
            {
                if (string.Equals("TestDiscovery.TestFound", message.MessageType))
                {
                    var test = message.Payload.ToObject<Test>();
                    if (string.Equals(name, test.DisplayName))
                    {
                        return test;
                    }
                }
            }

            return null;
        }

        private Process GetProcessById(int id)
        {
            return Process.GetProcesses().FirstOrDefault(p => p.Id == id);
        }
    }
}