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
    public class LoggedTestRunner : XunitTestRunner
    {
        public LoggedTestRunner(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod, object[]
            testMethodArguments, string skipReason,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected async override Task<Tuple<decimal, string>> InvokeTestAsync(ExceptionAggregator aggregator)
        {
            var testOutputHelper = ConstructorArguments.SingleOrDefault(a => typeof(TestOutputHelper).IsAssignableFrom(a.GetType())) as TestOutputHelper
                ?? new TestOutputHelper();
            testOutputHelper.Initialize(MessageBus, Test);

            var executionTime = await InvokeTestMethodAsync(aggregator, testOutputHelper);

            var output = testOutputHelper.Output;
            testOutputHelper.Uninitialize();

            return Tuple.Create(executionTime, output);
        }

        protected override Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
            => InvokeTestMethodAsync(aggregator, null);

        private Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator, ITestOutputHelper output)
            => new LoggedTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource, output).RunAsync();
    }
}
