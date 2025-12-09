// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="IImageGenerator"/> that passes through calls to another instance.
/// </summary>
/// <remarks>
/// This is recommended as a base type when building generators that can be chained in any order around an underlying <see cref="IImageGenerator"/>.
/// The default implementation simply passes each call to the inner generator instance.
/// </remarks>
[Experimental(diagnosticId: DiagnosticIds.Experiments.ImageGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public class DelegatingImageGenerator : IImageGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingImageGenerator"/> class.
    /// </summary>
    /// <param name="innerGenerator">The wrapped generator instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    protected DelegatingImageGenerator(IImageGenerator innerGenerator)
    {
        InnerGenerator = Throw.IfNull(innerGenerator);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the inner <see cref="IImageGenerator" />.</summary>
    protected IImageGenerator InnerGenerator { get; }

    /// <inheritdoc />
    public virtual Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request, ImageGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return InnerGenerator.GenerateAsync(request, options, cancellationToken);
    }

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        // If the key is non-null, we don't know what it means so pass through to the inner service.
        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            InnerGenerator.GetService(serviceType, serviceKey);
    }

    /// <summary>Provides a mechanism for releasing unmanaged resources.</summary>
    /// <param name="disposing"><see langword="true"/> if being called from <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            InnerGenerator.Dispose();
        }
    }
}
