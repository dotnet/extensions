// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.Extensions.AsyncState.Test;
public class AsyncContextTests
{
    [Fact]
    public async Task CreateAsyncContext_BeforeInitialize()
    {
        var state = new AsyncState();
        var initialContextCount = state.ContextCount;
        var context1 = new AsyncContext<IThing>(state);
        var context2 = new AsyncContext<IThing>(state);
        var obj1 = new Thing();
        var obj2 = new Thing();

        Assert.Equal(2, state.ContextCount - initialContextCount);
        state.Initialize();

        await Task.Run(() => context1.Set(obj1));

        await Task.Run(() =>
        {
            Assert.Same(obj1, context1.Get());

            Assert.Null(context2.Get());
        });
    }

    [Fact]
    public async Task CreateAsyncContext_AfterInitialize()
    {
        var state = new AsyncState();
        state.Initialize();
        var initialContextCount = state.ContextCount;

        var context1 = new AsyncContext<IThing>(state);
        var context2 = new AsyncContext<IThing>(state);

        Assert.Equal(2, state.ContextCount - initialContextCount);

        var obj1 = new Thing();

        await Task.Run(() => context1.Set(obj1));

        await Task.Run(() =>
        {
            Assert.Same(obj1, context1.Get());

            Assert.Null(context2.Get());
        });
    }

    [Fact]
    public async Task CreateAsyncContext_BeforeAndAfterInitialize()
    {
        var state = new AsyncState();
        var initialContextCount = state.ContextCount;
        var context1 = new AsyncContext<IThing>(state);
        state.Initialize();
        var context2 = new AsyncContext<IThing>(state);
        var obj1 = new Thing();
        var obj2 = new Thing();

        Assert.Equal(2, state.ContextCount - initialContextCount);

        await Task.Run(() =>
        {
            context1.Set(obj1);
            context2.Set(obj2);
        });

        await Task.Run(() =>
        {
            Assert.Same(obj1, context1.Get());

            Assert.Same(obj2, context2.Get());
        });
    }

    [Fact]
    public async Task Tryget_BeforeAndAfterInitialize()
    {
        var state = new AsyncState();
        var initialContextCount = state.ContextCount;
        var context1 = new AsyncContext<IThing>(state);
        Assert.False(context1.TryGet(out _));
        state.Initialize();
        var obj1 = new Thing();

        Assert.Equal(1, state.ContextCount - initialContextCount);

        await Task.Run(() =>
        {
            Assert.True(context1.TryGet(out var ctx1));
            Assert.Null(ctx1);
            context1.Set(obj1);
        });

        await Task.Run(() =>
        {
            Assert.True(context1.TryGet(out var ctx1));
            Assert.Same(obj1, ctx1);
        });
    }

    [Fact]
    public async Task CreateAsyncContextInAsync_AfterInitialize()
    {
        var state = new AsyncState();
        var initialContextCount = state.ContextCount;
        var context1 = new AsyncContext<IThing>(state);
        var obj1 = new Thing();

        state.Initialize();
        Assert.Equal(1, state.ContextCount - initialContextCount);

        await Task.Run(async () =>
        {
            context1.Set(obj1);

            var context2 = new AsyncContext<IThing>(state);
            var obj2 = new Thing();
            context2.Set(obj2);

            await Task.Run(() =>
            {
                Assert.Same(obj1, context1.Get());

                Assert.Same(obj2, context2.Get());
            });
        });
    }

    [Fact]
    public void Get_WhenNotInitialized()
    {
        var state = new AsyncState();
        var context1 = new AsyncContext<IThing>(state);

        Assert.Throws<InvalidOperationException>(() => _ = context1.Get());
    }

    [Fact]
    public void TryGet_WhenNotInitialized()
    {
        var state = new AsyncState();
        var context1 = new AsyncContext<IThing>(state);

        Assert.False(context1.TryGet(out var context));
        Assert.Null(context);
    }

    [Fact]
    public void Set_WhenNotInitialized()
    {
        var state = new AsyncState();
        var context1 = new AsyncContext<IThing>(state);

        Assert.Throws<InvalidOperationException>(() => context1.Set(new Thing()));
    }

    [Fact]
    public void Reset_DoesNotDisposeObjects()
    {
        var state = new AsyncState();
        var context = new AsyncContext<IDisposable>(state);
        var obj = new Mock<IDisposable>();
        obj.Setup(m => m.Dispose());

        state.Initialize();
        context.Set(obj.Object);
        state.Reset();

        obj.Verify(m => m.Dispose(), Times.Never);
    }

    [Fact]
    public async Task TwoAsyncFlows_WithDiffrentAsyncStates()
    {
        var state = new AsyncState();
        var initialContextCount = state.ContextCount;

        var task1 = Task.Run(async () =>
        {
            var context = new AsyncContext<IThing>(state);
            state.Initialize();
            var obj = new Thing();

            await Task.Run(async () =>
            {
                context.Set(obj);

                await Task.Run(() => Assert.Same(obj, context.Get()));
            });

            state.Reset();
        });

        var task2 = Task.Run(async () =>
        {
            var context = new AsyncContext<string>(state);
            state.Initialize();
            var obj = string.Empty;

            await Task.Run(async () =>
            {
                context.Set(obj);

                await Task.Run(() => Assert.Same(obj, context.Get()));
            });

            state.Reset();
        });

        await Task.WhenAll(task1, task2);
        Assert.Equal(2, state.ContextCount - initialContextCount);
    }

    [Fact]
    public async Task IndependentAsyncFlows_WithSameAsyncState()
    {
        var state = new AsyncState();
        var initialContextCount = state.ContextCount;
        var context = new AsyncContext<IThing>(state);

        Func<Task?> setAsyncState = async () =>
        {
            state.Initialize();

            await Task.Run(async () =>
            {
                var obj2 = new Thing();
                context.Set(obj2);

                await Task.Run(() =>
                {
                    Assert.Same(obj2, context.Get());
                });
            });

            state.Reset();
        };

        var tasks = new Task[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(setAsyncState);
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, state.ContextCount - initialContextCount);
    }
}
