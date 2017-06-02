// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggingServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddLogging_WrapsServiceCollection()
        {
            var services = new ServiceCollection();

            var callbackCalled = true;
            var loggerBuilder = services.AddLogging(builder =>
            {
                Assert.Same(services, builder.Services);
            });
            Assert.True(callbackCalled);
        }
    }
}
