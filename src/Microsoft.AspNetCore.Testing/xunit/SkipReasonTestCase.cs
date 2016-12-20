// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    internal class SkipReasonTestCase : LongLivedMarshalByRefObject, IXunitTestCase
    {
        private string _displayName;
        private bool _initialized;
        private IMethodInfo _methodInfo;
        private ITypeInfo[] _methodGenericTypes;
        private Dictionary<string, List<string>> _traits;

        public bool IsTheory { get; set; }

        public string SkipReason { get; set; }

        public string UniqueID { get; set; }

        public ISourceInformation SourceInformation { get; set; }

        public ITestMethod TestMethod { get; set; }

        public object[] TestMethodArguments { get; set; }

        public string DisplayName
        {
            get
            {
                EnsureInitialized();
                return _displayName;
            }
            set
            {
                EnsureInitialized();
                _displayName = value;
            }
        }

        public IMethodInfo Method
        {
            get
            {
                EnsureInitialized();
                return _methodInfo;
            }
            set
            {
                EnsureInitialized();
                _methodInfo = value;
            }
        }

        protected ITypeInfo[] MethodGenericTypes
        {
            get
            {
                EnsureInitialized();
                return _methodGenericTypes;
            }
        }

        public Dictionary<string, List<string>> Traits
        {
            get
            {
                EnsureInitialized();

                return _traits;
            }
            set
            {
                EnsureInitialized();

                _traits = value;
            }
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            UniqueID = info.GetValue<string>(nameof(UniqueID));
            SkipReason = info.GetValue<string>(nameof(SkipReason));
            IsTheory = info.GetValue<bool>(nameof(IsTheory));
            TestMethod = info.GetValue<ITestMethod>(nameof(TestMethod));
            TestMethodArguments = info.GetValue<object[]>(nameof(TestMethodArguments));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(UniqueID), UniqueID);
            info.AddValue(nameof(SkipReason), SkipReason);
            info.AddValue(nameof(IsTheory), IsTheory);
            info.AddValue(nameof(TestMethod), TestMethod);
            info.AddValue(nameof(TestMethodArguments), TestMethodArguments);
        }

        public Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            TestCaseRunner<IXunitTestCase> runner;
            if (IsTheory)
            {
                runner = new XunitTheoryTestCaseRunner(
                    this,
                    DisplayName,
                    SkipReason,
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
                    SkipReason,
                    constructorArguments,
                    TestMethodArguments,
                    messageBus,
                    aggregator,
                    cancellationTokenSource);
            }

            return runner.RunAsync();
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                _initialized = true;
                Initialize();
            }
        }

        private void Initialize()
        {
            Traits = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            Method = TestMethod.Method;

            if (TestMethodArguments != null)
            {
                IReflectionMethodInfo reflectionMethod = Method as IReflectionMethodInfo;
                if (reflectionMethod != null)
                {
                    TestMethodArguments = reflectionMethod.MethodInfo.ResolveMethodArguments(TestMethodArguments);
                }

                if (Method.IsGenericMethodDefinition)
                {
                    _methodGenericTypes = Method.ResolveGenericTypes(TestMethodArguments);
                    Method = Method.MakeGenericMethod(MethodGenericTypes);
                }
            }

            DisplayName = Method.GetDisplayNameWithArguments(TestMethod.Method.Name, TestMethodArguments, MethodGenericTypes);
        }
    }
}