// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Extensions.Http
{
    public class DefaultHttpClientFactoryTest
    {
        public DefaultHttpClientFactoryTest()
        {
            Services = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            Options = Services.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();
        }

        public IServiceProvider Services { get; }

        public IOptionsMonitor<HttpClientFactoryOptions> Options { get; }

        public IEnumerable<IHttpMessageHandlerBuilderFilter> EmptyFilters = Array.Empty<IHttpMessageHandlerBuilderFilter>();

        [Fact]
        public void Factory_MultipleCalls_DoesNotCacheHttpClient()
        {
            // Arrange
            var count = 0;
            Options.CurrentValue.HttpClientActions.Add(c =>
            {
                count++;
            });

            var factory = new DefaultHttpClientFactory(Services, Options, EmptyFilters);

            // Act 1
            var client1 = factory.CreateClient();

            // Act 2
            var client2 = factory.CreateClient();

            // Assert
            Assert.Equal(2, count);
            Assert.NotSame(client1, client2);
        }

        [Fact] 
        public void Factory_MultipleCalls_CachesHandler()
        {
            // Arrange
            var count = 0;
            Options.CurrentValue.HttpMessageHandlerBuilderActions.Add(b =>
            {
                count++;
            });

            var factory = new DefaultHttpClientFactory(Services, Options, EmptyFilters);

            // Act 1
            var client1 = factory.CreateClient();

            // Act 2
            var client2 = factory.CreateClient();

            // Assert
            Assert.Equal(1, count);
            Assert.NotSame(client1, client2);
        }

        [Fact]
        public void Factory_DisposeClient_DoesNotDisposeHandler()
        {
            // Arrange
            Options.CurrentValue.HttpMessageHandlerBuilderActions.Add(b =>
            {
                var mockHandler = new Mock<HttpMessageHandler>();
                mockHandler
                    .Protected()
                    .Setup("Dispose", true)
                    .Throws(new Exception("Dispose should not be called"));

                b.PrimaryHandler = mockHandler.Object;
            });

            var factory = new DefaultHttpClientFactory(Services, Options, EmptyFilters);

            // Act 
            using (factory.CreateClient())
            {
            }

            // Assert (does not throw)
        }

        [Fact]
        public void Factory_CreateClient_WithoutName_UsesDefaultOptions()
        {
            // Arrange
            var count = 0;
            Options.CurrentValue.HttpClientActions.Add(b =>
            {
                count++;
            });

            var factory = new DefaultHttpClientFactory(Services, Options, EmptyFilters);

            // Act
            var client = factory.CreateClient();

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public void Factory_CreateClient_WithName_UsesNamedOptions()
        {
            // Arrange
            var count = 0;
            Options.Get("github").HttpClientActions.Add(b =>
            {
                count++;
            });

            var factory = new DefaultHttpClientFactory(Services, Options, EmptyFilters);

            // Act
            var client = factory.CreateClient("github");

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public void Factory_CreateClient_FiltersCanDecorateBuilder()
        {
            // Arrange
            var expected = new HttpMessageHandler[]
            {
                Mock.Of<DelegatingHandler>(), // Added by filter1
                Mock.Of<DelegatingHandler>(), // Added by filter2
                Mock.Of<DelegatingHandler>(), // Added by filter3
                Mock.Of<DelegatingHandler>(), // Added in options
                Mock.Of<DelegatingHandler>(), // Added by filter3
                Mock.Of<DelegatingHandler>(), // Added by filter2
                Mock.Of<DelegatingHandler>(), // Added by filter1

                Mock.Of<HttpMessageHandler>(), // Set as primary handler by options
            };

            Options.Get("github").HttpMessageHandlerBuilderActions.Add(b =>
            {
                b.PrimaryHandler = expected[7];

                b.AdditionalHandlers.Add((DelegatingHandler)expected[3]);
            });

            var filter1 = new Mock<IHttpMessageHandlerBuilderFilter>();
            filter1
                .Setup(f => f.Configure(It.IsAny<Action<HttpMessageHandlerBuilder>>()))
                .Returns<Action<HttpMessageHandlerBuilder>>(next => (b) => 
                {
                    next(b); // Calls filter2
                    b.AdditionalHandlers.Insert(0, (DelegatingHandler)expected[0]);
                    b.AdditionalHandlers.Add((DelegatingHandler)expected[6]);
                });

            var filter2 = new Mock<IHttpMessageHandlerBuilderFilter>();
            filter2
                .Setup(f => f.Configure(It.IsAny<Action<HttpMessageHandlerBuilder>>()))
                .Returns<Action<HttpMessageHandlerBuilder>>(next => (b) =>
                {
                    next(b); // Calls filter3
                    b.AdditionalHandlers.Insert(0, (DelegatingHandler)expected[1]);
                    b.AdditionalHandlers.Add((DelegatingHandler)expected[5]);
                });

            var filter3 = new Mock<IHttpMessageHandlerBuilderFilter>();
            filter3
                .Setup(f => f.Configure(It.IsAny<Action<HttpMessageHandlerBuilder>>()))
                .Returns<Action<HttpMessageHandlerBuilder>>(next => (b) =>
                {
                    b.AdditionalHandlers.Add((DelegatingHandler)expected[2]);
                    next(b); // Calls options
                    b.AdditionalHandlers.Add((DelegatingHandler)expected[4]);
                });

            var factory = new DefaultHttpClientFactory(Services, Options, new[] { filter1.Object, filter2.Object, filter3.Object, });

            // Act
            var handler = factory.CreateHandler("github");

            // Assert
            for (var i = 0; i < expected.Length - 1; i++)
            {
                Assert.Same(expected[i], handler);
                handler = Assert.IsAssignableFrom<DelegatingHandler>(handler).InnerHandler;
            }

            Assert.Same(expected[7], handler);
        }
    }
}
