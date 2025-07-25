// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestTextToImageClient : ITextToImageClient
{
    public TestTextToImageClient()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public IServiceProvider? Services { get; set; }

    public Func<string, TextToImageOptions?, CancellationToken, Task<TextToImageResponse>>? GenerateImagesAsyncCallback { get; set; }

    public Func<AIContent, string, TextToImageOptions?, CancellationToken, Task<TextToImageResponse>>? EditImageAsyncCallback { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    public bool DisposeInvoked { get; private set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey)
        => serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<TextToImageResponse> GenerateImagesAsync(string prompt, TextToImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        return GenerateImagesAsyncCallback?.Invoke(prompt, options, cancellationToken) ??
            Task.FromResult(new TextToImageResponse());
    }

    public Task<TextToImageResponse> EditImageAsync(
        AIContent originalImage, string prompt, TextToImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        return EditImageAsyncCallback?.Invoke(originalImage, prompt, options, cancellationToken) ??
            Task.FromResult(new TextToImageResponse());
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
