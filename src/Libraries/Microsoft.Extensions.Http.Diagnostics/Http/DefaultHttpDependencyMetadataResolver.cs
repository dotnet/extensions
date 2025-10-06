// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Http.Diagnostics;

/// <summary>
/// Default implementation of <see cref="HttpDependencyMetadataResolver"/> that uses the base
/// trie-based lookup algorithm.
/// </summary>
public sealed class DefaultHttpDependencyMetadataResolver : HttpDependencyMetadataResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHttpDependencyMetadataResolver"/> class.
    /// </summary>
    /// <param name="dependencyMetadata">A collection of HTTP dependency metadata used for request resolution.</param>
    public DefaultHttpDependencyMetadataResolver(IEnumerable<IDownstreamDependencyMetadata> dependencyMetadata)
        : base(dependencyMetadata)
    {
    }
}
