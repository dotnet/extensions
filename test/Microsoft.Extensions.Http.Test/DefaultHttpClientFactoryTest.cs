// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        [Fact]
        public void Factory_MultipleCalls_DoesNotCacheHttpClient()
        {
            // Arrange
            var count = 0;
            Options.CurrentValue.HttpClientActions.Add(c =>
            {
                count++;
            });

            var factory = new DefaultHttpClientFactory(Services, Options);

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
            Options.CurrentValue.HandlerBuilderActions.Add(b =>
            {
                count++;
            });

            var factory = new DefaultHttpClientFactory(Services, Options);

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
            Options.CurrentValue.HandlerBuilderActions.Add(b =>
            {
                var mockHandler = new Mock<HttpMessageHandler>();
                mockHandler
                    .Protected()
                    .Setup("Dispose", true)
                    .Throws(new Exception("Dispose should not be called"));

                b.PrimaryHandler = mockHandler.Object;
            });

            var factory = new DefaultHttpClientFactory(Services, Options);

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

            var factory = new DefaultHttpClientFactory(Services, Options);

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

            var factory = new DefaultHttpClientFactory(Services, Options);

            // Act
            var client = factory.CreateClient("github");

            // Assert
            Assert.Equal(1, count);
        }
    }
}
