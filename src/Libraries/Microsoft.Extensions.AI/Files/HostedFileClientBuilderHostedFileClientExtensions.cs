// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="IHostedFileClient"/> in the context of <see cref="HostedFileClientBuilder"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public static class HostedFileClientBuilderHostedFileClientExtensions
{
    /// <summary>Creates a new <see cref="HostedFileClientBuilder"/> using <paramref name="innerClient"/> as its inner client.</summary>
    /// <param name="innerClient">The client to use as the inner client.</param>
    /// <returns>The new <see cref="HostedFileClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="HostedFileClientBuilder"/> constructor directly,
    /// specifying <paramref name="innerClient"/> as the inner client.
    /// </remarks>
    public static HostedFileClientBuilder AsBuilder(this IHostedFileClient innerClient)
    {
        _ = Throw.IfNull(innerClient);

        return new HostedFileClientBuilder(innerClient);
    }
}
