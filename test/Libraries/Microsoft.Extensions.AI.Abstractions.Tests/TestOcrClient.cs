// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public sealed class TestOcrClient : IOcrClient
{
    public TestOcrClient()
    {
        GetServiceCallback = DefaultGetServiceCallback;
    }

    public IServiceProvider? Services { get; set; }

    public Func<Stream,
        string,
        OcrOptions?,
        IProgress<OcrProgress>?,
        CancellationToken,
        Task<OcrResult>>?
        GetTextAsyncCallback
    { get; set; }

    public Func<Type, object?, object?> GetServiceCallback { get; set; }

    private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey)
        => serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public Task<OcrResult> GetTextAsync(
        Stream document,
        string mediaType,
        OcrOptions? options = null,
        IProgress<OcrProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => GetTextAsyncCallback!.Invoke(document, mediaType, options, progress, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceCallback!.Invoke(serviceType, serviceKey);

    public void Dispose()
    {
        // Dispose of resources if any.
    }
}
