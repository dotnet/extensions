// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    internal class ConditionalAttributeDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public ConditionalAttributeDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo attributeInfo)
        {
            var skipReason = EvaluateSkipConditions(testMethod);

            IXunitTestCaseDiscoverer innerDiscoverer;
            if (testMethod.Method.GetCustomAttributes(typeof(TheoryAttribute)).Any())
            {
                innerDiscoverer = new TheoryDiscoverer(_diagnosticMessageSink);
            }
            else
            {
                innerDiscoverer = new FactDiscoverer(_diagnosticMessageSink);
            }

            var testCases = innerDiscoverer
                .Discover(discoveryOptions, testMethod, attributeInfo)
                .Select(testCase => new SkipReasonTestCase
                {
                    IsTheory = typeof(XunitTheoryTestCase).GetTypeInfo().IsAssignableFrom(testCase.GetType().GetTypeInfo()),
                    SkipReason = testCase.SkipReason ?? skipReason,
                    SourceInformation = testCase.SourceInformation,
                    TestMethod = testCase.TestMethod,
                    TestMethodArguments = testCase.TestMethodArguments,
                    UniqueID = testCase.UniqueID,
                });

            return testCases;
        }

        private string EvaluateSkipConditions(ITestMethod testMethod)
        {
            var testClass = testMethod.TestClass.Class;
            var assembly = testMethod.TestClass.TestCollection.TestAssembly.Assembly;
            var conditionAttributes = testMethod.Method
                .GetCustomAttributes(typeof(ITestCondition))
                .Concat(testClass.GetCustomAttributes(typeof(ITestCondition)))
                .Concat(assembly.GetCustomAttributes(typeof(ITestCondition)))
                .OfType<ReflectionAttributeInfo>()
                .Select(attributeInfo => attributeInfo.Attribute);

            foreach (ITestCondition condition in conditionAttributes)
            {
                if (!condition.IsMet)
                {
                    return condition.SkipReason;
                }
            }

            return null;
        }
    }
}