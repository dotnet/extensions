// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AsyncState.Test;

[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Testing")]
public class AsyncStateTests
{
    [Fact]
    public async Task GettingAsyncContextReturnsAsyncContext()
    {
        var state = new AsyncState();
        var context = new Thing();

        var token = state.RegisterAsyncContext();

        static Task SetAsyncContext(AsyncState state, IThing context, AsyncStateToken token)
        {
            state.Initialize();
            state.Set(token, context);

            return Task.CompletedTask;
        }

        await SetAsyncContext(state, context, token).ConfigureAwait(false);

        Assert.Same(context, state.Get(token));
    }

    [Fact]
    public async Task GettingAsyncContextReturnsNullAsyncContextIfSetToNull()
    {
        var state = new AsyncState();
        var context = new Thing();
        var token = state.RegisterAsyncContext();
        state.Initialize();
        state.Set(token, context);

        var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitForNullTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var afterNullCheckTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        var task = Task.Run(async () =>
        {
            // The AsyncContext flows with the execution context
            Assert.Same(context, state.Get(token));

            checkAsyncFlowTcs.SetResult(null!);

            await waitForNullTcs.Task;

            try
            {
                Assert.Null(state.Get(token));

                afterNullCheckTcs.SetResult(null!);
            }
            catch (Exception ex)
            {
                afterNullCheckTcs.SetException(ex);
            }
        });

        await checkAsyncFlowTcs.Task;

        // Null out the context
        state.Set(token, null);

        waitForNullTcs.SetResult(null!);

        Assert.Null(state.Get(token));

        await afterNullCheckTcs.Task;
        await task;
    }

    [Fact]
    public async Task GettingAsyncContextReturnsNullAsyncContextIfChanged()
    {
        var state = new AsyncState();
        var context = new Thing();
        var token = state.RegisterAsyncContext();

        state.Initialize();
        state.Set(token, context);

        var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitForNullTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var afterNullCheckTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        var task = Task.Run(async () =>
        {
            // The AsyncContext flows with the execution context
            Assert.Same(context, state.Get(token));

            checkAsyncFlowTcs.SetResult(null!);

            await waitForNullTcs.Task;

            try
            {
                Assert.Throws<InvalidOperationException>(() => state.Get(token));

                afterNullCheckTcs.SetResult(null!);
            }
            catch (Exception ex)
            {
                afterNullCheckTcs.SetException(ex);
            }
        });

        await checkAsyncFlowTcs.Task;

        // Set a new Async context
        state.Initialize();
        var context2 = new Thing();
        state.Set(token, context2);

        waitForNullTcs.SetResult(null!);

        Assert.Same(context2, state.Get(token));

        await afterNullCheckTcs.Task;
        await task;
    }

    [Fact]
    public async Task GettingAsyncContextDoesNotFlowIfAccessorSetToNull()
    {
        var state = new AsyncState();
        var context = new Thing();
        var token = state.RegisterAsyncContext();
        state.Initialize();
        state.Set(token, context);

        var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        state.Set(token, null);

        var task = Task.Run(() =>
        {
            try
            {
                // The AsyncContext flows with the execution context
                Assert.Null(state.Get(token));

                checkAsyncFlowTcs.SetResult(null!);
            }
            catch (Exception ex)
            {
                checkAsyncFlowTcs.SetException(ex);
            }
        });

        await checkAsyncFlowTcs.Task;
        await task;
    }

    [Fact]
    public async Task GettingAsyncContextDoesNotFlowIfExecutionContextDoesNotFlow()
    {
        var state = new AsyncState();
        var context = new Thing();
        var token = state.RegisterAsyncContext();
        state.Initialize();
        state.Set(token, context);

        var checkAsyncFlowTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            try
            {
                // The AsyncContext flows with the execution context
                Assert.Throws<InvalidOperationException>(() => state.Get(token));
                checkAsyncFlowTcs.SetResult(null!);
            }
            catch (Exception ex)
            {
                checkAsyncFlowTcs.SetException(ex);
            }
        }, null);

        await checkAsyncFlowTcs.Task;
    }

    [Fact]
    public void RegisterContextCorrectly()
    {
        var asyncState = new AsyncState();
        var initialContextCount = asyncState.ContextCount;

        var c1 = asyncState.RegisterAsyncContext();
        Assert.Equal(0, c1.Index - initialContextCount);
        var c2 = asyncState.RegisterAsyncContext();
        Assert.Equal(1, c2.Index - initialContextCount);
        var c3 = asyncState.RegisterAsyncContext();
        Assert.Equal(2, c3.Index - initialContextCount);

        Assert.Equal(3, asyncState.ContextCount - initialContextCount);
    }

    [Fact]
    public void EnsureCount_IncreasesCountCorrectly()
    {
        var l = new List<object?>();
        AsyncState.EnsureCount(l, 5);
        Assert.Equal(5, l.Count);
    }

    [Fact]
    public void EnsureCount_WhenCountLessThanExpected()
    {
        var l = new List<object?>(new object?[5]);
        AsyncState.EnsureCount(l, 2);
        Assert.Equal(5, l.Count);
    }

    [Fact]
    public void EnsureCount_WhenCountEqualWithExpected()
    {
        var l = new List<object?>(new object?[5]);
        AsyncState.EnsureCount(l, 5);
        Assert.Equal(5, l.Count);
    }

    [Fact]
    public async Task AsyncStateCanBeUsedInDifferentServiceProviders()
    {
        await using var spOne = PrepareAsyncState(new Tuple<double>(3.14));
        await using var spTwo = PrepareAsyncState(new Tuple<int>(42));

        _ = spOne.GetRequiredService<IAsyncContext<Tuple<double>>>().Get();
        _ = spTwo.GetRequiredService<IAsyncContext<Tuple<double>>>().Get();

        static ServiceProvider PrepareAsyncState<T>(T value)
            where T : notnull
        {
            var services = new ServiceCollection().AddAsyncState().BuildServiceProvider();
            services.GetRequiredService<IAsyncState>().Initialize();
            services.GetRequiredService<IAsyncContext<T>>().Set(value);
            return services;
        }
    }
}
