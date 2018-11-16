// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    [CollectionDefinition(nameof(EventSourceTests), DisableParallelization = true)]
    public class EventSourceTests : ICollectionFixture<EventSourceTests>
    {
    }

    [Collection(nameof(EventSourceTests))]
    public class DependencyInjectionEventSourceTests: IDisposable
    {
        private readonly TestEventListener _listener = new TestEventListener();

        public DependencyInjectionEventSourceTests()
        {
            _listener.EnableEvents(DependencyInjectionEventSource.Instance, EventLevel.Verbose);
        }

        [Fact]
        public void ExistsWithCorrectId()
        {
            var esType = typeof(DependencyInjectionEventSource);

            Assert.NotNull(esType);

            Assert.Equal("Microsoft-Extensions-DependencyInjection", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("27837f46-1a43-573d-d30c-276de7d02192"), EventSource.GetGuid(esType));
            Assert.NotEmpty(EventSource.GenerateManifest(esType, "assemblyPathToIncludeInManifest"));
        }

        [Fact]
        public void EmitsCallSiteBuiltEvent()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<FakeDisposeCallback>();
            serviceCollection.AddTransient<IFakeOuterService, FakeDisposableCallbackOuterService>();
            serviceCollection.AddSingleton<IFakeMultipleService, FakeDisposableCallbackInnerService>();
            serviceCollection.AddScoped<IFakeMultipleService, FakeDisposableCallbackInnerService>();
            serviceCollection.AddTransient<IFakeMultipleService, FakeDisposableCallbackInnerService>();
            serviceCollection.AddSingleton<IFakeService, FakeDisposableCallbackInnerService>();

            serviceCollection.BuildServiceProvider().GetService<IEnumerable<IFakeOuterService>>();

            var callsiteBuiltEvent = Assert.Single(_listener.EventData);

            Assert.Equal(
                "IEnumerable<Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeOuterService> (size 1)" + Environment.NewLine +
                "    DisposeCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackOuterService" + Environment.NewLine +
                "        RootCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService" + Environment.NewLine +
                "            RootCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback" + Environment.NewLine +
                "        IEnumerable<Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeMultipleService> (size 3)" + Environment.NewLine +
                "            RootCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService" + Environment.NewLine +
                "                RootCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback" + Environment.NewLine +
                "            ScopeCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService" + Environment.NewLine +
                "                RootCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback" + Environment.NewLine +
                "            DisposeCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposableCallbackInnerService" + Environment.NewLine +
                "                RootCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback" + Environment.NewLine +
                "        RootCache new Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeDisposeCallback" + Environment.NewLine,
                GetProperty(callsiteBuiltEvent, "callSite"));

            Assert.Equal(1, callsiteBuiltEvent.EventId);
        }

        private string GetProperty(EventWrittenEventArgs data, string propName)
            => data.Payload[data.PayloadNames.IndexOf(propName)] as string;

        private class TestEventListener : EventListener
        {
            private volatile bool _disposed;
            private ConcurrentQueue<EventWrittenEventArgs> _events = new ConcurrentQueue<EventWrittenEventArgs>();

            public IEnumerable<EventWrittenEventArgs> EventData => _events;

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (!_disposed)
                {
                    _events.Enqueue(eventData);
                }
            }

            public override void Dispose()
            {
                _disposed = true;
                base.Dispose();
            }
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
