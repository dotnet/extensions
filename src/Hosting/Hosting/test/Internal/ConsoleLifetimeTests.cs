// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Fakes;
using Microsoft.Extensions.Hosting.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Hosting.Internal
{
    public partial class HostTests
    {
        [Fact]
        public async Task ConsoleLifetimeDoesNotThrowExceptionFromProcessExit()
        {
            using (var host = CreateBuilder()
                .UseEnvironment("WithHostingEnvironment")
                .Build())
            {
                await host.StartAsync();
                var env = host.Services.GetService<IHostEnvironment>();
                Assert.Equal("WithHostingEnvironment", env.EnvironmentName);
            }
        }
    }
}
