// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class TaskExtensionsTest
    {
        [Fact]
        public async Task TimeoutAfterTest()
        {
            const double timeoutMilliseconds = 100;
            var sw = new Stopwatch();
            sw.Start();
            await Assert.ThrowsAsync<TimeoutException>(async () =>
                await Task.Run(async () => await Task.Delay(1000)).TimeoutAfter(TimeSpan.FromMilliseconds(timeoutMilliseconds)));
            Assert.True(sw.ElapsedMilliseconds >= timeoutMilliseconds);
        }
    }
}
