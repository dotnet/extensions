// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;
using Microsoft.Framework.TestAdapter;
using Microsoft.Framework.TestHost.Client;
using Microsoft.AspNet.Testing.xunit;
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

        [Fact]
        public async Task ListTest()
        {
            // Arrange
            var host = new TestHostWrapper();

            // Act
            var result = await host.RunListAsync(_testProject);

            // Assert
            Assert.Equal(0, result);

            Assert.Equal(8, host.Output.Count);
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.True_is_true"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest1(x: 3)"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest2(x: 1, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest2(x: 2, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestFound(m, "SampleTest.TheoryTest2(x: 3, s: \"Hi\")"));
            Assert.Equal("TestDiscovery.Response", host.Output[host.Output.Count - 1].MessageType);
        }

        [Fact]
        public async Task RunTest_All()
        {
            // Arrange
            var host = new TestHostWrapper();

            // Act
            var result = await host.RunTestsAsync(_testProject);

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

        [Fact]
        public async Task RunTest_ByUniqueName()
        {
            // Arrange
            var host = new TestHostWrapper();

            await host.RunListAsync(_testProject);

            var test = host.Output
                .Where(m => m.MessageType == "TestDiscovery.TestFound")
                .First()
                .Payload.ToObject<Test>();

            host.Output.Clear();

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
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        public void RunTest_DoesNotRunOnWin7()
        {
            Version osVersion = Environment.OSVersion.Version;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT
                && osVersion.Major == 6 && osVersion.Minor == 1)
            {
                throw new SystemException("Test should not be running on Win7");
            }
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