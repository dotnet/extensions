﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Publishers;

/// <summary>
/// A publisher that throws an error.
/// </summary>
internal sealed class FaultPublisher : IResourceUtilizationPublisher
{
    /// <inheritdoc/>
    public async ValueTask PublishAsync(ResourceUtilization utilization, CancellationToken cancellationToken)
    {
        await default(ValueTask);
        throw new InvalidOperationException();
    }
}
