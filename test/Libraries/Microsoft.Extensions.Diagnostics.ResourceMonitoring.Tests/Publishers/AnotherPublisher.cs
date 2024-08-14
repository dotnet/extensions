// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Publishers;

/// <summary>
/// Another publisher that do nothing, added to test scenarios where multiple publishers are added to the services collections.
/// </summary>
[Obsolete("Testing the obsolete IResourceUtilizationPublisher interface.")]
internal sealed class AnotherPublisher : IResourceUtilizationPublisher
{
    /// <inheritdoc/>
    public ValueTask PublishAsync(ResourceUtilization utilization, CancellationToken cancellationToken)
    {
        return default;
    }
}
