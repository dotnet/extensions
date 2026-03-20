// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestVideoGenerator : IVideoGenerator
{
    public TestVideoGenerator()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public IServiceProvider? Services { get; set; }

    public Func<VideoGenerationRequest, VideoGenerationOptions?, CancellationToken, Task<VideoGenerationResponse>>? GenerateVideosAsyncCallback { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    public bool DisposeInvoked { get; private set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey)
        => serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<VideoGenerationResponse> GenerateAsync(
        VideoGenerationRequest request,
        VideoGenerationOptions? options = null,
        IProgress<VideoGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return GenerateVideosAsyncCallback?.Invoke(request, options, cancellationToken) ??
            Task.FromResult(new VideoGenerationResponse());
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return GetServiceCallback.Invoke(serviceType, serviceKey);
    }

    public void Dispose()
    {
        DisposeInvoked = true;
    }
}
