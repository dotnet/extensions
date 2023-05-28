// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace System.Cloud.Messaging.Tests.Data;

/// <summary>
/// Provides test implementation for <see cref="MessageContext"/>.
/// </summary>
internal sealed class TestMessageContext
    : MessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestMessageContext"/> class.
    /// </summary>
    /// <param name="features">The feature collection.</param>
    /// <param name="sourcePayload">The payload obtained from the <see cref="IMessageSource"/>.</param>
    public TestMessageContext(IFeatureCollection features, ReadOnlyMemory<byte> sourcePayload)
        : base(features, sourcePayload)
    {
    }

    /// <summary>
    /// Mock implementation which returns default <see cref="ValueTask"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    public override ValueTask MarkCompleteAsync(CancellationToken cancellationToken) => default;
}
