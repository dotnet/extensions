// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedTestInvoker : XunitTestInvoker
    {
        private TestOutputHelper _output;

        public LoggedTestInvoker(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected override async Task AfterTestMethodInvokedAsync()
        {
            await base.AfterTestMethodInvokedAsync();

            if (_output != null)
            {
                _output.Uninitialize();
            }
        }

        protected override object CreateTestClass()
        {
            var testClass = base.CreateTestClass();

            if (testClass is ILoggedTest loggedTest)
            {
                // Try resolving ITestOutputHelper from constructor arguments
                var testOutputHelper = ConstructorArguments?.SingleOrDefault(a => typeof(ITestOutputHelper).IsAssignableFrom(a.GetType())) as ITestOutputHelper;

                // None resolved so create a new one and retain a reference to it for initialization/uninitialization
                if (testOutputHelper == null)
                {
                    testOutputHelper = _output = new TestOutputHelper();
                    _output.Initialize(MessageBus, Test);
                }

                loggedTest.Initialize(TestMethod, TestMethodArguments, testOutputHelper);
            }

            return testClass;
        }
    }
}
