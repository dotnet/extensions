// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    internal class ConditionalTheoryDiscoverer : TheoryDiscoverer
    {
        public ConditionalTheoryDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
        {
            var testCases = base.Discover(discoveryOptions, testMethod, theoryAttribute);
            // Xunit evaluates MemeberData before skip conditions. If there is no member data it returns an error test case.
            // However we have some cases where we conditionally expect no MemeberData and we set skip conditions accordingly.
            // E.g. All of the test cases are excluded on a specific OS. In other words, don't fail if it was going to be skipped anyways.
            // We don't always do this because we want to show the individual test cases as skipped where possible.
            if (testCases.Count() == 1 && testCases.First() is ExecutionErrorTestCase)
            {
                var skipReason = testMethod.EvaluateSkipConditions();
                if (skipReason != null)
                {
                    return new[] { new SkippedTestCase(skipReason, DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod) };
                }
            }
            return testCases;
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
        {
            var skipReason = testMethod.EvaluateSkipConditions();
            return skipReason != null
               ? new[] { new SkippedTestCase(skipReason, DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod) }
               : base.CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow)
        {
            var skipReason = testMethod.EvaluateSkipConditions();
            return skipReason != null
                ? base.CreateTestCasesForSkippedDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, skipReason)
                : base.CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow);
        }
    }
}