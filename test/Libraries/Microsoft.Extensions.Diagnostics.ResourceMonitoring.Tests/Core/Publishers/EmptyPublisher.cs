// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Publishers;

/// <summary>
/// A publisher that do nothing.
/// </summary>
internal sealed class EmptyPublisher : IResourceUtilizationPublisher
{
    /// <inheritdoc/>
    public ValueTask PublishAsync(Utilization utilization, CancellationToken cancellationToken)
    {
        return default;
    }
}
