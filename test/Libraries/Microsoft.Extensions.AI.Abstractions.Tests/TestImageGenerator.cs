// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestImageGenerator : IImageGenerator
{
    public TestImageGenerator()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public IServiceProvider? Services { get; set; }

    public Func<ImageGenerationRequest, ImageGenerationOptions?, CancellationToken, Task<ImageGenerationResponse>>? GenerateImagesAsyncCallback { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    public bool DisposeInvoked { get; private set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey)
        => serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<ImageGenerationResponse> GenerateAsync(ImageGenerationRequest request, ImageGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return GenerateImagesAsyncCallback?.Invoke(request, options, cancellationToken) ??
            Task.FromResult(new ImageGenerationResponse());
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
