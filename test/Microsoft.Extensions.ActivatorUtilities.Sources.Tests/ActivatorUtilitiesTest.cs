// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class ActivatorUtilitiesTests
    {
        [Fact]
        public void NullValuesAllowedInConstructorMatching()
        {
            var testClass = (TestClass)ActivatorUtilities.CreateInstance(new TestServiceProvider(), typeof(TestClass), 5, null);
            Assert.Equal(5, testClass._i);
            Assert.Equal(null, testClass._s);
        }

        private class TestClass
        {
            public int _i;
            public string _s;

            public TestClass(int i, string s)
            {
                _i = i;
                _s = s;
            }
        }

        private class TestServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return null;
            }
        }
    }
}