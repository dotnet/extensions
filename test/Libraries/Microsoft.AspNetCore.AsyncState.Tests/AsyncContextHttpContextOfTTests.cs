// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AsyncState;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.AsyncState.Test;

public class AsyncContextHttpContextOfTTests
{
    private readonly IHttpContextAccessor _accessorMock;
    private readonly IAsyncState _asyncState;
    private readonly IAsyncContext<Thing> _context;

    public AsyncContextHttpContextOfTTests()
    {
        var serviceCollection = new ServiceCollection()
            .AddHttpContextAccessor()
            .AddAsyncStateHttpContext();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _accessorMock = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        _accessorMock.HttpContext = new DefaultHttpContext();

        _context = serviceProvider.GetRequiredService<IAsyncContext<Thing>>();
        _asyncState = serviceProvider.GetRequiredService<IAsyncState>();
        _asyncState.Reset();
    }

    [Fact]
    public void TryGetReturnsTrueWhenHttpContextPresent()
    {
        var value = new Thing();
        _context.Set(value);

        Assert.True(_context.TryGet(out Thing? stored));
        Assert.Same(value, stored);
    }

    [Fact]
    public void TryGetReturnsTrueWhenHttpContextPresentAndValueNotSet()
    {
        Assert.True(_context.TryGet(out Thing? stored));
        Assert.Null(stored);
    }

    [Fact]
    public void GetReturnsNullWhenHttpContextPresentAndValueNotSet()
    {
        Assert.Null(_context.Get());
    }

    [Fact]
    public void TryGetReturnsFalseWhenHttpContextNotPresent()
    {
        _accessorMock.HttpContext = null;

        Assert.False(_context.TryGet(out Thing? stored));
        Assert.Null(stored);
    }

    [Fact]
    public void SetThrowsWhenHttpContextNotPresent()
    {
        _accessorMock.HttpContext = null;
        var value = new Thing();

        Assert.Throws<InvalidOperationException>(() => _context.Set(value));
    }

    [Fact]
    public void GetThrowsWhenHttpContextNotPresent()
    {
        _accessorMock.HttpContext = null;
        Assert.Throws<InvalidOperationException>(() => _context.Get());
    }

    [Fact]
    public void TryGet_WhenAsyncStateIsUsed_ReturnsTrue()
    {
        _accessorMock.HttpContext = null;
        _asyncState.Initialize();

        var value = new Thing();
        _context.Set(value);

        Assert.True(_context.TryGet(out Thing? stored));
        Assert.Same(value, stored);
    }

    [Fact]
    public void TryGet_WhenAsyncStateIsUsedAndValueNotSet_ReturnsNull()
    {
        _accessorMock.HttpContext = null;
        _asyncState.Initialize();

        Assert.Null(_context.Get());
    }
}
