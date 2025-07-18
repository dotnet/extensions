// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

internal sealed class TestTextToImageClient : ITextToImageClient
{
    public Func<string, TextToImageOptions?, CancellationToken, Task<TextToImageResponse>>? GenerateImagesAsyncCallback { get; set; }

    public Func<Stream, string, string, TextToImageOptions?, CancellationToken, Task<TextToImageResponse>>? GenerateEditImageAsyncCallback { get; set; }

    public Func<Type, object?, object?>? GetServiceCallback { get; set; }

    public bool DisposeInvoked { get; private set; }

    public Task<TextToImageResponse> GenerateImagesAsync(string prompt, TextToImageOptions? options, CancellationToken cancellationToken = default)
    {
        return GenerateImagesAsyncCallback?.Invoke(prompt, options, cancellationToken) ??
            Task.FromResult(new TextToImageResponse());
    }

    public Task<TextToImageResponse> GenerateEditImageAsync(
        Stream originalImage, string originalImageFileName, string prompt, TextToImageOptions? options, CancellationToken cancellationToken = default)
    {
        return GenerateEditImageAsyncCallback?.Invoke(originalImage, originalImageFileName, prompt, options, cancellationToken) ??
            Task.FromResult(new TextToImageResponse());
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return GetServiceCallback?.Invoke(serviceType, serviceKey);
    }

    public void Dispose()
    {
        DisposeInvoked = true;
    }
}
