// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Shared.Pools;
using Polly;
using Polly.Utilities;

namespace Microsoft.Extensions.Resilience.Hedging;

/// <summary>
/// Approach consistent with the retry policy engine.
/// <see href="https://github.com/App-vNext/Polly/blob/174cc53e17bf02da5e1f2c0d74dffb4f23aa99c0/src/Polly/Retry/AsyncRetryEngine.cs"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result handled by the policy delegate.</typeparam>
internal static partial class HedgingEngine<TResult>
{
    private static readonly ObjectPool<Dictionary<Task<TResult>, CancellationPair>> _dictionaryPool = PoolFactory.CreateDictionaryPool<Task<TResult>, CancellationPair>();
    private static readonly ObjectPool<CancellationTokenSource> _cancellationSources = PoolFactory.CreateCancellationTokenSourcePool();

    public static async Task<TResult> ExecuteAsync(
        Func<Context, CancellationToken, Task<TResult>> primaryHedgedTask,
        Context context,
        HedgedTaskProvider<TResult> hedgedTaskProvider,
        HedgingEngineOptions<TResult> options,
        bool continueOnCapturedContext,
        CancellationToken cancellationToken)
    {
        var loadedHedgedTasks = 0;
        var hedgedTasks = _dictionaryPool.Get();

        DelegateResult<TResult>? previousResult = null;

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool loaded = TryLoadNextHedgedTask(primaryHedgedTask, context, hedgedTaskProvider, options, ref loadedHedgedTasks, hedgedTasks, cancellationToken);

                if (!loaded && hedgedTasks.Count == 0)
                {
                    if (previousResult!.Exception is not null)
                    {
                        ExceptionDispatchInfo.Capture(previousResult.Exception).Throw();
                    }

                    return previousResult.Result;
                }

                var completedHedgedTask =
                    await WaitForTaskAsync(
                        options,
                        context,
                        loadedHedgedTasks,
                        hedgedTasks.Keys,
                        continueOnCapturedContext,
                        cancellationToken).ConfigureAwait(continueOnCapturedContext);

                if (completedHedgedTask == null)
                {
                    // If completedHedgedTask is null it indicates that we still do not have any finished hedged task within the hedging delay.
                    // We will create additional hedged task in the next iteration.
                    continue;
                }

                // Drop the source for completed task and dispose it, so it won't be canceled later
                if (hedgedTasks.Remove(completedHedgedTask, out var source))
                {
                    source.Dispose();
                }

                if (previousResult != null)
                {
                    DisposeResult(previousResult.Result);
                }

                try
                {
                    // Note: Task.WhenAny *does not* throw.
                    // To fetch the possible exception, the actual task must be awaited.
                    var result = await completedHedgedTask.ConfigureAwait(continueOnCapturedContext);
                    if (!options.ShouldHandleResultPredicates.AnyMatch(result))
                    {
                        return result;
                    }

                    previousResult = new DelegateResult<TResult>(result);
                }
                catch (Exception ex)
                {
                    var handledException = options.ShouldHandleExceptionPredicates.FirstMatchOrDefault(ex);
                    if (handledException is null)
                    {
                        throw;
                    }

                    previousResult = new DelegateResult<TResult>(ex);
                }

                // If nothing has been returned or thrown yet, the result is a transient failure,
                // and other hedged request will be awaited.
                // Before it, one needs to perform the task adjacent to each hedged call.
                try
                {
                    await options.OnHedgingAsync(previousResult, context, loadedHedgedTasks, cancellationToken).ConfigureAwait(continueOnCapturedContext);
                }
                catch (Exception)
                {
                    DisposeResult(previousResult.Result);
                    throw;
                }
            }
        }
        finally
        {
            CleanupPendingTasks(hedgedTasks);
        }
    }

    private static bool TryLoadNextHedgedTask(
        Func<Context, CancellationToken, Task<TResult>> primaryHedgedTask,
        Context context,
        HedgedTaskProvider<TResult> hedgedTaskProvider,
        HedgingEngineOptions<TResult> options,
        ref int loadedHedgedTasks,
        Dictionary<Task<TResult>, CancellationPair> hedgedTasks,
        CancellationToken cancellationToken)
    {
        if (loadedHedgedTasks >= options.MaxHedgedTasks)
        {
            return false;
        }

        var pair = CancellationPair.Create(cancellationToken);

        Task<TResult>? task;
        try
        {
            if (loadedHedgedTasks == 0)
            {
                task = primaryHedgedTask(context, pair.CancellationToken);
            }

            // Stryker disable once Logical: https://domoreexp.visualstudio.com/R9/_workitems/edit/2804465
            else if (!hedgedTaskProvider(new HedgingTaskProviderArguments(context, loadedHedgedTasks, pair.CancellationToken), out task) || task is null)
            {
                pair.Dispose();
                return false;
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            // this was handled exception, continue with hedging
            task = Task.FromException<TResult>(ex);
        }

        loadedHedgedTasks++;
        hedgedTasks.Add(task, pair);

        // Stryker disable once boolean : no means to test this
        return true;
    }

    private static async Task<Task<TResult>?> WaitForTaskAsync(
        HedgingEngineOptions<TResult> options,
        Context context,
        int loadedHedgedTasks,
        Dictionary<Task<TResult>, CancellationPair>.KeyCollection hedgedTasks,
        bool continueOnCapturedContext,
        CancellationToken cancellationToken)
    {
        // before doing anything expensive, let's check whether any existing task is already completed
        foreach (var task in hedgedTasks)
        {
            if (task.IsCompleted)
            {
                return task;
            }
        }

        if (loadedHedgedTasks == options.MaxHedgedTasks)
        {
            return await WhenAnyAsync(hedgedTasks).ConfigureAwait(continueOnCapturedContext);
        }

        var hedgingDelay = options.HedgingDelayGenerator(new HedgingDelayArguments(context, loadedHedgedTasks - 1, cancellationToken));

        if (hedgingDelay == TimeSpan.Zero)
        {
            // just load the next task
            return null;
        }

        // Stryker disable once equality : no means to test this, stryker changes '<' to '<=' where 0 is already covered in the branch above
        if (hedgingDelay < TimeSpan.Zero)
        {
            // this disables the hedging, we wait for first finished task
            return await WhenAnyAsync(hedgedTasks).ConfigureAwait(continueOnCapturedContext);
        }

        using var delayTaskCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var delayTask = SystemClock.SleepAsync(hedgingDelay, delayTaskCancellation.Token);
        var whenAnyHedgedTask = WhenAnyAsync(hedgedTasks);
        var completedTask = await Task.WhenAny(whenAnyHedgedTask, delayTask).ConfigureAwait(continueOnCapturedContext);

        if (completedTask == delayTask)
        {
            return null;
        }

        // cancel the ongoing delay task
        // Stryker disable once boolean : no means to test this
#if NET8_0_OR_GREATER
        await delayTaskCancellation.CancelAsync().ConfigureAwait(continueOnCapturedContext);
#else
        delayTaskCancellation.Cancel(throwOnFirstException: false);
#endif

        return await whenAnyHedgedTask.ConfigureAwait(continueOnCapturedContext);
    }

    private static void CleanupPendingTasks(Dictionary<Task<TResult>, CancellationPair> tasks)
    {
        // Stryker disable once equality : there is no way to check that the tasks were returned to pool
        if (tasks.Count == 0)
        {
            _dictionaryPool.Return(tasks);
            return;
        }

        // first, cancel any pending requests
        foreach (var pair in tasks)
        {
            pair.Value.Cancellation.Cancel();
        }

        // We are intentionally doing the cleanup in the background as we do not want to
        // delay the hedging.
        // The background cleanup is safe. All exceptions are handled.
        _ = CleanupInBackgroundAsync(tasks);

        static async Task CleanupInBackgroundAsync(Dictionary<Task<TResult>, CancellationPair> tasks)
        {
            foreach (var task in tasks)
            {
                try
                {
                    var result = await task.Key.ConfigureAwait(false);
                    DisposeResult(result);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    // The tasks are spawned inside of HedgingHandler and possible exceptions are handled on ExecuteAsync method.
                    // During the dispose phase we swallow the exception for every task that is running.
                    // They are almost guaranteed to throw since they get canceled.
                }

                // dispose cancellation token source linked with this task
                task.Value.Dispose();
            }

            _dictionaryPool.Return(tasks);
        }
    }

    private static void DisposeResult(TResult? result)
    {
        if (result is IDisposable disposableResult)
        {
            disposableResult.Dispose();
        }
    }

    internal record struct CancellationPair(CancellationTokenSource Cancellation, CancellationTokenRegistration? Registration) : IDisposable
    {
        public CancellationToken CancellationToken => Cancellation.Token;

        public static CancellationPair Create(CancellationToken token)
        {
            var currentCancellation = _cancellationSources.Get();

            if (token.CanBeCanceled)
            {
                return new CancellationPair(currentCancellation, token.Register(o => ((CancellationTokenSource)o!).Cancel(), currentCancellation));
            }

            return new CancellationPair(currentCancellation, null);
        }

        public void Dispose()
        {
            Registration?.Dispose();
            _cancellationSources.Return(Cancellation);
        }
    }
}
