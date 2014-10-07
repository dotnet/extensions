// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;
using Microsoft.Framework.TestAdapter;
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
            Assert.Single(host.Output, m => TestFound(m, "Sample.Tests.SampleTest.True_is_true"));
            Assert.Single(host.Output, m => TestFound(m, "Sample.Tests.SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestFound(m, "Sample.Tests.SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestFound(m, "Sample.Tests.SampleTest.TheoryTest1(x: 3)"));
            Assert.Single(host.Output, m => TestFound(m, "Sample.Tests.SampleTest.TheoryTest2(x: 1, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestFound(m, "Sample.Tests.SampleTest.TheoryTest2(x: 2, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestFound(m, "Sample.Tests.SampleTest.TheoryTest2(x: 3, s: \"Hi\")"));
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
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.True_is_true"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.True_is_true"));
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest1(x: 3)"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest1(x: 3)"));
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest2(x: 1, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest2(x: 1, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest2(x: 2, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest2(x: 2, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest2(x: 3, s: \"Hi\")"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest2(x: 3, s: \"Hi\")"));
            Assert.Equal("TestExecution.Response", host.Output[host.Output.Count - 1].MessageType);
        }

        [Fact]
        public async Task RunTest_ByDisplayName()
        {
            // Arrange
            var host = new TestHostWrapper();

            // Act
            var result = await host.RunTestsAsync(
                _testProject,
                "Sample.Tests.SampleTest.TheoryTest1(x: 1)",
                "Sample.Tests.SampleTest.TheoryTest1(x: 2)");

            // Assert
            Assert.Equal(0, result);

            Assert.Equal(5, host.Output.Count);
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest1(x: 2)"));
            Assert.Equal("TestExecution.Response", host.Output[host.Output.Count - 1].MessageType);
        }

        [Fact]
        public async Task RunTest_ByDisplayName_Short()
        {
            // Arrange
            var host = new TestHostWrapper();

            // Act
            var result = await host.RunTestsAsync(_testProject, "Sample.Tests.SampleTest.TheoryTest1");

            // Assert
            Assert.Equal(0, result);

            Assert.Equal(7, host.Output.Count);
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest1(x: 1)"));
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest1(x: 2)"));
            Assert.Single(host.Output, m => TestStarted(m, "Sample.Tests.SampleTest.TheoryTest1(x: 3)"));
            Assert.Single(host.Output, m => TestPassed(m, "Sample.Tests.SampleTest.TheoryTest1(x: 3)"));
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