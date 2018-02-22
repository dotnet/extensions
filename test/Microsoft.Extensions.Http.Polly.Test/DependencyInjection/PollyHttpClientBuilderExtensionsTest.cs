// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;
using Polly;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class PollyHttpClientBuilderExtensionsTest
    {
        [Fact]
        public void AddPolicyHandler_NonGeneric_AddsPolicyHandler()
        {
            var serviceCollection = new ServiceCollection();

            HttpMessageHandlerBuilder builder = null;

            // Act1
            serviceCollection.AddHttpClient("example.com")
                .AddPolicyHandler(Policy.TimeoutAsync(5))
                .ConfigureHttpMessageHandlerBuilder(b =>
                {
                    builder = b;
                });

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            // Act2
            var client = factory.CreateClient("example.com");

            // Assert
            Assert.NotNull(client);

            Assert.Collection(
                builder.AdditionalHandlers,
                h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
                h => Assert.IsType<PolicyHttpMessageHandler>(h),
                h => Assert.IsType<LoggingHttpMessageHandler>(h));
        }

        [Fact]
        public void AddPolicyHandler_Generic_AddsPolicyHandler()
        {
            var serviceCollection = new ServiceCollection();

            HttpMessageHandlerBuilder builder = null;

            // Act1
            serviceCollection.AddHttpClient("example.com")
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(5))
                .ConfigureHttpMessageHandlerBuilder(b =>
                {
                    builder = b;
                });

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            // Act2
            var client = factory.CreateClient("example.com");

            // Assert
            Assert.NotNull(client);

            Assert.Collection(
                builder.AdditionalHandlers,
                h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
                h => Assert.IsType<PolicyHttpMessageHandler>(h),
                h => Assert.IsType<LoggingHttpMessageHandler>(h));
        }

        [Fact]
        public void AddServerErrorPolicyHandler_AddsPolicyHandler()
        {
            var serviceCollection = new ServiceCollection();

            HttpMessageHandlerBuilder builder = null;

            // Act1
            serviceCollection.AddHttpClient("example.com")
                .AddServerErrorPolicyHandler(b => b.RetryAsync(5))
                .ConfigureHttpMessageHandlerBuilder(b =>
                {
                    builder = b;
                });

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            // Act2
            var client = factory.CreateClient("example.com");

            // Assert
            Assert.NotNull(client);

            Assert.Collection(
                builder.AdditionalHandlers,
                h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
                h => Assert.IsType<PolicyHttpMessageHandler>(h),
                h => Assert.IsType<LoggingHttpMessageHandler>(h));
        }
    }
}
