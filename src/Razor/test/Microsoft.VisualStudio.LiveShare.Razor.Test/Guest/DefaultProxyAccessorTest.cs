// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public class DefaultProxyAccessorTest
    {
        [Fact]
        public void GetProjectSnapshotManagerProxy_Caches()
        {
            // Arrange
            var proxy = Mock.Of<IProjectSnapshotManagerProxy>();
            var proxyAccessor = new TestProxyAccessor<IProjectSnapshotManagerProxy>(proxy);

            // Act
            var proxy1 = proxyAccessor.GetProjectSnapshotManagerProxy();
            var proxy2 = proxyAccessor.GetProjectSnapshotManagerProxy();

            // Assert
            Assert.Same(proxy1, proxy2);
        }

        [Fact]
        public void GetProjectHierarchyProxy_Caches()
        {
            // Arrange
            var proxy = Mock.Of<IProjectHierarchyProxy>();
            var proxyAccessor = new TestProxyAccessor<IProjectHierarchyProxy>(proxy);

            // Act
            var proxy1 = proxyAccessor.GetProjectHierarchyProxy();
            var proxy2 = proxyAccessor.GetProjectHierarchyProxy();

            // Assert
            Assert.Same(proxy1, proxy2);
        }

        private class TestProxyAccessor<TTestProxy> : DefaultProxyAccessor where TTestProxy : class
        {
            private readonly TTestProxy _proxy;

            public TestProxyAccessor(TTestProxy proxy)
            {
                _proxy = proxy;
            }

            internal override TProxy CreateServiceProxy<TProxy>()
            {
                if (typeof(TProxy) == typeof(TTestProxy))
                {
                    return _proxy as TProxy;
                }

                throw new InvalidOperationException("The proxy accessor was called with unexpected arguments.");
            }
        }
    }
}
