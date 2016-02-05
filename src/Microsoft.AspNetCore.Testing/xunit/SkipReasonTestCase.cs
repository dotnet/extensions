// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    internal class SkipReasonTestCase : LongLivedMarshalByRefObject, IXunitTestCase
    {
        private readonly bool _isTheory;
        private readonly string _skipReason;
        private readonly IXunitTestCase _wrappedTestCase;

        public SkipReasonTestCase(bool isTheory, string skipReason, IXunitTestCase wrappedTestCase)
        {
            _isTheory = isTheory;
            _skipReason = wrappedTestCase.SkipReason ?? skipReason;
            _wrappedTestCase = wrappedTestCase;
        }

        public string DisplayName
        {
            get
            {
                return _wrappedTestCase.DisplayName;
            }
        }

        public IMethodInfo Method
        {
            get
            {
                return _wrappedTestCase.Method;
            }
        }

        public string SkipReason
        {
            get
            {
                return _skipReason;
            }
        }

        public ISourceInformation SourceInformation
        {
            get
            {
                return _wrappedTestCase.SourceInformation;
            }
            set
            {
                _wrappedTestCase.SourceInformation = value;
            }
        }

        public ITestMethod TestMethod
        {
            get
            {
                return _wrappedTestCase.TestMethod;
            }
        }

        public object[] TestMethodArguments
        {
            get
            {
                return _wrappedTestCase.TestMethodArguments;
            }
        }

        public Dictionary<string, List<string>> Traits
        {
            get
            {
                return _wrappedTestCase.Traits;
            }
        }

        public string UniqueID
        {
            get
            {
                return _wrappedTestCase.UniqueID;
            }
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            _wrappedTestCase.Deserialize(info);
        }

        public Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            TestCaseRunner<IXunitTestCase> runner;
            if (_isTheory)
            {
                runner = new XunitTheoryTestCaseRunner(
                    this,
                    DisplayName,
                    _skipReason,
                    constructorArguments,
                    diagnosticMessageSink,
                    messageBus,
                    aggregator,
                    cancellationTokenSource);
            }
            else
            {
                runner = new XunitTestCaseRunner(
                    this,
                    DisplayName,
                    _skipReason,
                    constructorArguments,
                    TestMethodArguments,
                    messageBus,
                    aggregator,
                    cancellationTokenSource);
            }

            return runner.RunAsync();
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            _wrappedTestCase.Serialize(info);
        }
    }
}