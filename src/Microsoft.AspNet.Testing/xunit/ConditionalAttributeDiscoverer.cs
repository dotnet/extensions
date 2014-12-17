// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNet.Testing.xunit
{
    internal class ConditionalAttributeDiscoverer : IXunitTestCaseDiscoverer
    {
        public IEnumerable<IXunitTestCase> Discover(ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var skipReason = EvaluateSkipConditions(testMethod);

            IXunitTestCaseDiscoverer innerDiscoverer;
            if (testMethod.Method.GetCustomAttributes(typeof(TheoryAttribute)).Any())
            {
                innerDiscoverer = new TheoryDiscoverer();
            }
            else
            {
                innerDiscoverer = new FactDiscoverer();
            }

            var res = innerDiscoverer.Discover(testMethod, factAttribute).Select(s => new SkipReasonTestCase(skipReason, s));
            return res;
        }

        private string EvaluateSkipConditions(ITestMethod testMethod)
        {
            var conditionAttributes = testMethod.Method
                .GetCustomAttributes(typeof(ITestCondition))
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