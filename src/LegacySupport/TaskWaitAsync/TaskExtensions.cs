// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks;

/// <summary>
/// Provides a set of static methods for <see cref="Task"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class TaskExtensions
{
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
    /// <summary>
    /// Gets a <see cref="Task{TResult}"/> that will complete when the <paramref name="task"/> completes or when the specified <paramref name="cancellationToken"/> has cancellation requested.
    /// </summary>
    /// <typeparam name="TResult">The type of the task result.</typeparam>
    /// <param name="task">The task to wait on for completion.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
    /// <returns>The <see cref="Task{TResult}"/> representing the asynchronous wait.</returns>
    public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
    {
        if (task.IsCompleted || (!cancellationToken.CanBeCanceled))
        {
            return task;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TResult>(cancellationToken);
        }

        return WaitTaskAsync(task, cancellationToken);
    }

    private static async Task<TResult> WaitTaskAsync<TResult>(Task<TResult> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(
                static state => ((TaskCompletionSource<TResult>)state!).SetCanceled(), tcs, false))
        {
            var t = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
#pragma warning disable VSTHRD103 // Call async methods when in an async method
            return t.GetAwaiter().GetResult();
#pragma warning restore VSTHRD103 // Call async methods when in an async method
        }
    }
}
