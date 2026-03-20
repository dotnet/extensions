// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingVideoGeneratorTests
{
    [Fact]
    public void RequiresInnerVideoGenerator()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new TestDelegatingVideoGenerator(null!));
    }

    [Fact]
    public async Task GenerateVideosAsyncDefaultsToInnerGeneratorAsync()
    {
        var expectedResponse = new VideoGenerationResponse();
        using var inner = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, ct) => Task.FromResult(expectedResponse)
        };

        using var delegating = new TestDelegatingVideoGenerator(inner);
        var result = await delegating.GenerateAsync(new VideoGenerationRequest("Test"));
        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestVideoGenerator();
        using var generator = new TestDelegatingVideoGenerator(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => generator.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        using var inner = new TestVideoGenerator();
        using var generator = new TestDelegatingVideoGenerator(inner);
        Assert.Same(generator, generator.GetService(typeof(DelegatingVideoGenerator)));
        Assert.Same(generator, generator.GetService(typeof(IVideoGenerator)));
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        using var inner = new TestVideoGenerator
        {
            GetServiceCallback = (type, key) => key is not null ? "inner-result" : null
        };

        using var generator = new TestDelegatingVideoGenerator(inner);
        Assert.Equal("inner-result", generator.GetService(typeof(string), "someKey"));
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        using var inner = new TestVideoGenerator
        {
            GetServiceCallback = (type, key) => type == typeof(string) ? "inner-result" : null
        };

        using var generator = new TestDelegatingVideoGenerator(inner);
        Assert.Equal("inner-result", generator.GetService(typeof(string)));
    }

    [Fact]
    public void Dispose_SetsFlag()
    {
        using var inner = new TestVideoGenerator();
        var generator = new TestDelegatingVideoGenerator(inner);
        Assert.False(inner.DisposeInvoked);
        generator.Dispose();
        Assert.True(inner.DisposeInvoked);
    }

    [Fact]
    public void Dispose_MultipleCallsSafe()
    {
        using var inner = new TestVideoGenerator();
        var generator = new TestDelegatingVideoGenerator(inner);
        generator.Dispose();
        generator.Dispose();
        Assert.True(inner.DisposeInvoked);
    }

    private sealed class TestDelegatingVideoGenerator : DelegatingVideoGenerator
    {
        public TestDelegatingVideoGenerator(IVideoGenerator innerGenerator)
            : base(innerGenerator)
        {
        }
    }
}
