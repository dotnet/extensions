// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.Extensions.Resilience.Polly.Test.Hedging;

public static class TaskHelper
{
    public static async Task AdvanceTimeUntilFinished(this Task task, FakeTimeProvider timeProvider, TimeSpan? delta = null, TimeSpan? maxAdvance = null)
    {
        var advanceTask = CreateAdvanceTask(task, timeProvider, delta, maxAdvance);

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        await task;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
        await advanceTask;
    }

    public static async Task<T> AdvanceTimeUntilFinished<T>(this Task<T> task, FakeTimeProvider timeProvider, TimeSpan? delta = null, TimeSpan? maxAdvance = null)
    {
        delta ??= TimeSpan.FromDays(1);
        var advanceTask = CreateAdvanceTask(task, timeProvider, delta, maxAdvance);

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        var result = await task;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
        await advanceTask;

        return result;
    }

    private static Task CreateAdvanceTask(Task task, FakeTimeProvider timeProvider, TimeSpan? delta, TimeSpan? maxAdvance)
    {
        var totalAdvanced = 0d;
        delta ??= TimeSpan.FromDays(1);

        return Task.Run(async () =>
        {
            while (!task.IsCompleted)
            {
                timeProvider.Advance(delta.Value);
                totalAdvanced += delta.Value.TotalMilliseconds;

                if (maxAdvance != null && totalAdvanced > maxAdvance.Value.TotalMilliseconds)
                {
                    break;
                }

                await Task.Delay(1);
            }
        });
    }
}
