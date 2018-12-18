// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Memory.Tests.Infrastructure
{
    public static class TaskExtensions
    {
        public static async Task<bool> WaitAsync(this Task task, TimeSpan timeout)
        {
            if (task.IsCompleted)
            {
                return true;
            }

            var cts = new CancellationTokenSource();
            var completed =  await Task.WhenAny(task, Task.Delay(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : timeout, cts.Token));

            if (completed != task)
            {
                return false;
            }

            cts.Cancel();
            return true;
        }
    }
}
