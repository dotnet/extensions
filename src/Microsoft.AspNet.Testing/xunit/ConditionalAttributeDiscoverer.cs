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
        public IEnumerable<IXunitTestCase> Discover(ITestCollection testCollection, IAssemblyInfo assembly, ITypeInfo testClass, IMethodInfo testMethod, IAttributeInfo factAttribute)
        {
            var skipReason = EvaluateSkipConditions(testMethod);
            var wrapperAttributeInfo = new SkipReasonAttributeInfo(skipReason, factAttribute);

            IXunitTestCaseDiscoverer innerDiscoverer;
            if (testMethod.GetCustomAttributes(typeof(TheoryAttribute)).Any())
            {
                innerDiscoverer = new TheoryDiscoverer();
            }
            else
            {
                innerDiscoverer = new FactDiscoverer();
            }

            var res = innerDiscoverer.Discover(testCollection, assembly, testClass, testMethod, wrapperAttributeInfo);
            return res;
        }

        private string EvaluateSkipConditions(IMethodInfo testMethod)
        {
            var conditionAttributes = testMethod
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