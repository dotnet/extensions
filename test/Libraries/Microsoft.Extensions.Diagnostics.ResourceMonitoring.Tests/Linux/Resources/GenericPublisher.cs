// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

/// <summary>
/// A publisher that accept <see cref="Action{Utilization}"/> in its constructor.
/// </summary>
[Obsolete("Testing the obsolete IResourceUtilizationPublisher interface.")]
internal sealed class GenericPublisher : IResourceUtilizationPublisher
{
    private readonly Action<ResourceUtilization> _publish;
    public GenericPublisher(Action<ResourceUtilization> publish)
    {
        _publish = publish;
    }

    /// <inheritdoc/>
    public ValueTask PublishAsync(ResourceUtilization utilization, CancellationToken cancellationToken)
    {
        _publish(utilization);
        return default;
    }
}
