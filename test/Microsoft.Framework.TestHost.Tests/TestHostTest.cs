// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;
using Microsoft.Framework.TestAdapter;
using Microsoft.Framework.TestHost.Client;
using Microsoft.Framework.TestHost.TestAdapter;
using Xunit;

namespace Microsoft.Framework.TestHost
{
    public class TestHostTest
    {
        private readonly string _testProject;

        public TestHostTest()
        {
            var services = CallContextServiceLocator.Locator.ServiceProvider;

            var libraryManager = (ILibraryManager)services.GetService(typeof(ILibraryManager));
            _testProject = Path.GetDirectoryName(libraryManager.GetLibraryInformation("Sample.Tests").Path);
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
                   "Name": "Microsoft.Framework.TestHost.TestAdapter.SourceInformationProvider",
                   "EventId": 0,
                   "Level": "Warning",
                   "Message": "Failed to create DIA DataSource. No source information will be available.\r\nSystem.Runtime.InteropServices.COMException (0x80040154): Retrieving the COM class factory for component with CLSID {E6756135-1E65-4D17-8576-610761398C3C} failed due to the following error: 80040154 Class not registered (Exception from HRESULT: 0x80040154 (REGDB_E_CLASSNOTREG)).\r\n   at Microsoft.Framework.TestHost.TestAdapter.SourceInformationProvider.EnsureInitialized() in C:\\projects\\testing\\src\\Microsoft.Framework.TestHost\\TestAdapter\\SourceInformationProvider.cs:line 155"
             */

            var fullMessageDiagnostics = string.Format("Full output: \n{0}", string.Join("\n", host.Output));
            var testOutput = host.Output.Where(message => message.MessageType != "Log");

            Assert.True(8 == testOutput.Count(), "Output count is not 8. \n" + fullMessageDiagnostics);
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.True_is_true"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest1(x: 3)"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest2(x: 1, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest2(x: 2, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest2(x: 3, s: \"Hi\")"));
            Assert.Equal("TestDiscovery.Response", host.Output[host.Output.Count - 1].MessageType);
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

            Assert.Equal(15, host.Output.Count);
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
    }
}